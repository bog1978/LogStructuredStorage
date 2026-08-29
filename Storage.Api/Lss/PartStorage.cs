using System.Text;

namespace Storage.Api.Lss;

internal sealed class PartStorage : IDisposable
{
    private readonly ReaderWriterLockSlim _lock = new();
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
            try
            {
                _lock.EnterReadLock();
                return _partHeader.PartNumber;
            }
            finally
            {
                _lock.ExitReadLock();
            }
        }
    }

    public DateTimeOffset MinTime
    {
        get
        {
            try
            {
                _lock.EnterReadLock();
                return _partHeader.MinTime;
            }
            finally
            {
                _lock.ExitReadLock();
            }
        }
    }

    public DateTimeOffset MaxTime
    {
        get
        {
            try
            {
                _lock.EnterReadLock();
                return _partHeader.MaxTime;
            }
            finally
            {
                _lock.ExitReadLock();
            }
        }
    }

    internal string PartPath => _partPath;

    public bool TryWrite(byte[] data, out long offset)
    {
        if (_writer == null)
        {
            offset = -1;
            return false;
        }

        try
        {
            _lock.EnterWriteLock();
            if (_writer.BaseStream.Length < _writer.BaseStream.Position + sizeof(int) + data.Length)
            {
                _partHeader = _writer.ClosePart(_partHeader);
                offset = -1;
                Close();
                return false;
            }

            offset = _writer.BaseStream.Position;
            _writer.Write(data.Length);
            _writer.Write(data);
            _writer.Flush();
            _partHeader = _writer.UpdateWriteOffset(_partHeader);
            return true;
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    public byte[] Read(long offset)
    {
        try
        {
            _lock.EnterReadLock();
            using var stream = new FileStream(PartPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using var reader = new BinaryReader(stream, Encoding.UTF8, true);
            stream.Seek(offset, SeekOrigin.Begin);
            var size = reader.ReadInt32();
            return reader.ReadBytes(size);
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    public void MakeCold(string bucketColdDir)
    {
        if (!IsHot)
            throw new InvalidOperationException("Part is already cold");

        try
        {
            _lock.EnterWriteLock();

            if (!Directory.Exists(bucketColdDir))
                Directory.CreateDirectory(bucketColdDir);

            var newPath = Path.Combine(bucketColdDir, Path.GetFileName(PartPath));

            using (var srcStream = new FileStream(PartPath, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite))
            using (var dstStream = new FileStream(newPath, FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite))
                srcStream.CopyTo(dstStream);
            Close();
            File.Delete(_partPath);
            _partPath = newPath;
            (_partHeader, _writer) = LoadPart(_partPath);
            _isHot = false;
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    public void Delete()
    {
        try
        {
            _lock.EnterWriteLock();
            Close();
            File.Delete(PartPath);
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    public void Close()
    {
        _writer?.Flush();
        _writer?.Dispose();
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
        var partHeader = stream.ReadHeader();
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
        var partHeader = writer.CreateHeader(partNumber);
        return (partHeader, writer);
    }
}