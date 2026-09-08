using System.Text;
using DotNext.Threading;
using Storage.Api.Lss.Model;

namespace Storage.Api.Lss;

internal sealed class PartStorage : IDisposable
{
    private readonly AsyncReaderWriterLock _lock = new();
    private BinaryWriter? _writer;
    private PartHeader _partHeader;
    private string _partPath;

    private PartStorage(string partPath, PartHeader partHeader, BinaryWriter? writer)
    {
        _partPath = partPath;
        _partHeader = partHeader;
        _writer = writer;
        PartNumber = _partHeader.PartNumber;
    }

    public bool IsHot => _partHeader.PartType == PartTypeEnum.Hot;

    public int PartNumber { get; }

    internal string PartPath => _partPath;

    public async Task<long> TryWrite(
        FileHeader fileHeader,
        Stream inStream,
        CancellationToken token)
    {
        var isLocked = false;
        try
        {
            await _lock.EnterWriteLockAsync(token);
            isLocked = true;

            if (_partHeader.PartType != PartTypeEnum.Hot)
                return -1;

            if (_writer == null)
                throw new InvalidOperationException("Writer is null");

            if (fileHeader.Length != inStream.Length)
                throw new InvalidOperationException("File length mismatch");

            var headerBytes = FileHeader.ToBytes(fileHeader);
            if (_writer.BaseStream.Length < _writer.BaseStream.Position + headerBytes.Length + fileHeader.Length)
            {
                _partHeader = _writer.MakeWarmPart(_partHeader);
                Close();
                return -1;
            }
            else
            {
                var offset = _writer.BaseStream.Position;
                _writer.Write(headerBytes);
                await inStream.CopyToAsync(_writer.BaseStream, token);
                _writer.Flush();
                _partHeader = _writer.UpdateWriteOffset(_partHeader);
                return offset;
            }
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
            await using var stream = new FileStream(_partPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using var reader = new BinaryReader(stream, Encoding.UTF8, true);
            stream.Seek(offset, SeekOrigin.Begin);
            var fileHeader = FileHeader.ToHeader(reader);
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

    public async Task Delete(CancellationToken token)
    {
        var isLocked = false;
        try
        {
            await _lock.EnterWriteLockAsync(token);
            isLocked = true;
            Close();
            File.Delete(_partPath);
            _partHeader = _partHeader with { PartType = PartTypeEnum.Deleted };
        }
        finally
        {
            if (isLocked)
                _lock.Release();
        }
    }

    public async Task<PartTypeEnum> ApplyRetentionPolicy(RetentionPolicy policy, string bucketColdDir, CancellationToken token)
    {
        var isLockedRead = false;

        try
        {
            await _lock.EnterReadLockAsync(token);
            isLockedRead = true;

            if (_partHeader.PartType == PartTypeEnum.Hot)
                return _partHeader.PartType;

            // Полное время жизни складывается из горячего и холодного.
            if (_partHeader.MaxTime + policy.TtlHot + policy.TtlCold < DateTimeOffset.UtcNow)
                await DeleteInternal(token);
            else if (_partHeader.PartType == PartTypeEnum.Warm && _partHeader.MaxTime + policy.TtlHot < DateTimeOffset.UtcNow)
                await MakeColdInternal(bucketColdDir, token);

            return _partHeader.PartType;
        }
        finally
        {
            if (isLockedRead)
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

    private async Task MakeColdInternal(string bucketColdDir, CancellationToken token)
    {
        var isLocked = false;
        try
        {
            if (!Directory.Exists(bucketColdDir))
                Directory.CreateDirectory(bucketColdDir);

            await _lock.UpgradeToWriteLockAsync(token);
            isLocked = true;

            if (_partHeader.PartType == PartTypeEnum.Cold)
                throw new InvalidOperationException("Part is already cold");

            // TODO: Копировать сразу с новым заголовком. Тогда можно будет использовать EnterReadLockAsync,
            // TODO: а перед удалением - UpgradeToWriteLockAsync.
            _writer?.MakeColdPart(_partHeader);

            var newPath = Path.Combine(bucketColdDir, Path.GetFileName(_partPath));

            await using (var srcStream = new FileStream(_partPath, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite))
            await using (var dstStream = new FileStream(newPath, FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite))
                await srcStream.CopyToAsync(dstStream, token);
            Close();
            File.Delete(_partPath);
            _partPath = newPath;
            (_partHeader, _writer) = LoadPart(_partPath);
        }
        finally
        {
            if (isLocked)
                _lock.DowngradeFromWriteLock();
        }
    }

    private async Task DeleteInternal(CancellationToken token)
    {
        var isLocked = false;
        try
        {
            await _lock.UpgradeToWriteLockAsync(token);
            isLocked = true;
            Close();
            File.Delete(_partPath);
            _partHeader = _partHeader with { PartType = PartTypeEnum.Deleted };
        }
        finally
        {
            if (isLocked)
                _lock.DowngradeFromWriteLock();
        }
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

    public static PartStorage Create(string partPath)
    {
        var (partHeader, writer) = LoadPart(partPath);
        return new PartStorage(partPath, partHeader, writer);
    }

    public static PartStorage Create(string rootPath, int partNumber, int partSizeMb)
    {
        if (!Directory.Exists(rootPath))
            Directory.CreateDirectory(rootPath);
        var partPath = Path.Combine(rootPath, $"{partNumber:0000000000}.lss");
        var stream = new FileStream(partPath, FileMode.CreateNew, FileAccess.ReadWrite, FileShare.ReadWrite);
        stream.SetLength(partSizeMb * 1024 * 1024);
        var writer = new BinaryWriter(stream);
        var now = DateTimeOffset.UtcNow;
        var partHeader = writer.CreatePartHeader(new PartHeader(partNumber, 0, PartTypeEnum.Hot, now, now));
        return new PartStorage(partPath, partHeader, writer);
    }
}