using System.Text;
using DotNext.Threading;

namespace Storage.Api.Lss;

internal sealed class PartStorage : IDisposable
{
    private readonly AsyncReaderWriterLock _lock = new();
    private BinaryWriter? _writer;
    private PartHeader _partHeader;
    private string _partPath;
    private bool _isHot;

    public PartStorage(string partPath, bool isHot)
    {
        _isHot = isHot;
        _partPath = partPath;
        (_partHeader, _writer) = LoadPart(partPath);
    }

    public PartStorage(string rootPath, int partNumber, int partSizeMb)
    {
        _isHot = true;
        if (!Directory.Exists(rootPath))
            Directory.CreateDirectory(rootPath);
        _partPath = Path.Combine(rootPath, $"{partNumber:0000000000}.lss");
        (_partHeader, _writer) = CreatePart(_partPath, partNumber, partSizeMb);
    }

    public bool CanWrite => _writer != null;

    public bool IsHot => _isHot;

    public int PartNumber
    {
        get
        {
            // TODO: переделать на Interlocked. 
            if (!_lock.TryEnterReadLock())
                throw new InvalidOperationException("Cannot read part number");
            var partNumber = _partHeader.PartNumber;
            _lock.Release();
            return partNumber;
        }
    }

    public DateTimeOffset MaxTime
    {
        get
        {
            // TODO: переделать на Interlocked. 
            if (!_lock.TryEnterReadLock())
                throw new InvalidOperationException("Cannot read part number");
            var maxTime = _partHeader.MaxTime;
            _lock.Release();
            return maxTime;
        }
    }

    internal string PartPath => _partPath;

    public async Task<long> TryWrite(FileHeader fileHeader, Stream inStream, CancellationToken token)
    {
        var isLocked = false;
        try
        {
            await _lock.EnterWriteLockAsync(token);
            isLocked = true;

            if (_writer == null)
                return -1;

            if (fileHeader.Length != inStream.Length)
                throw new InvalidOperationException("File length mismatch");

            if (_writer.BaseStream.Length < _writer.BaseStream.Position + sizeof(int) + fileHeader.Length)
            {
                _partHeader = _writer.ClosePart(_partHeader);
                Close();
                return -1;
            }

            var offset = _writer.BaseStream.Position;
            _writer.WriteFileHeader(fileHeader);
            await inStream.CopyToAsync(_writer.BaseStream, token);
            _writer.Flush();
            _partHeader = _writer.UpdateWriteOffset(_partHeader);
            return offset;
        }
        finally
        {
            if (isLocked)
                _lock.Release();
        }
    }

    public async Task Read(
        long offset,
        Stream outStream,
        Action<FileHeader> headersCallback,
        CancellationToken token)
    {
        var isLocked = false;
        try
        {
            await _lock.EnterReadLockAsync(token);
            isLocked = true;
            await using var stream = new FileStream(PartPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using var reader = new BinaryReader(stream, Encoding.UTF8, true);
            stream.Seek(offset, SeekOrigin.Begin);
            var fileHeader = reader.ReadFileHeader();
            headersCallback(fileHeader);
            // TODO: Переделать на асинхронное копирование диапазона stream в outStream.
            var data = reader.ReadBytes(fileHeader.Length);
            await outStream.WriteAsync(data, token);
        }
        finally
        {
            if (isLocked)
                _lock.Release();
        }
    }

    public async Task MakeCold(string bucketColdDir, CancellationToken token)
    {
        var isLocked = false;
        try
        {
            await _lock.EnterWriteLockAsync(token);
            isLocked = true;

            if (!IsHot)
                throw new InvalidOperationException("Part is already cold");


            if (!Directory.Exists(bucketColdDir))
                Directory.CreateDirectory(bucketColdDir);

            var newPath = Path.Combine(bucketColdDir, Path.GetFileName(PartPath));

            await using (var srcStream =
                         new FileStream(PartPath, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite))
            await using (var dstStream =
                         new FileStream(newPath, FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite))
                await srcStream.CopyToAsync(dstStream, token);
            Close();
            File.Delete(_partPath);
            _partPath = newPath;
            (_partHeader, _writer) = LoadPart(_partPath);
            _isHot = false;
        }
        finally
        {
            if (isLocked)
                _lock.Release();
        }
    }

    public async Task Delete(CancellationToken token)
    {
        var isLocked = false;
        try
        {
            await _lock.EnterWriteLockAsync(token);
            isLocked = true;
            Close();
            File.Delete(PartPath);
        }
        finally
        {
            if (isLocked)
                _lock.Release();
        }
    }

    public void Close()
    {
        if (_writer == null)
            return;
        _writer.Flush();
        _writer.Dispose();
        _writer = null;
    }

    public void Dispose()
    {
        Close();
        _writer?.Dispose();
        _lock.Dispose();
    }

    private static (PartHeader header, BinaryWriter? writer) LoadPart(string partPath)
    {
        var stream = new FileStream(partPath, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite);
        var partHeader = stream.ReadPartHeader();
        if (partHeader.WritePosition <= 0)
        {
            stream.Dispose();
            return (partHeader, null);
        }

        var writer = new BinaryWriter(stream);
        stream.Seek(partHeader.WritePosition, SeekOrigin.Begin);
        return (partHeader, writer);
    }

    private static (PartHeader header, BinaryWriter? writer) CreatePart(string partPath, int partNumber, int partSizeMb)
    {
        var stream = new FileStream(partPath, FileMode.CreateNew, FileAccess.ReadWrite, FileShare.ReadWrite);
        stream.SetLength(partSizeMb * 1024 * 1024);
        var writer = new BinaryWriter(stream);
        var partHeader = writer.CreatePartHeader(partNumber);
        return (partHeader, writer);
    }
}