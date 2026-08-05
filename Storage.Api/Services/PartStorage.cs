using System.Text;

namespace Storage.Api.Services;

internal sealed class PartStorage : IDisposable
{
    private readonly string _partPath;
    private readonly FileStream _stream;
    private readonly BinaryWriter _writer;
    private readonly ReaderWriterLockSlim _lock = new();
    private PartHeader _partHeader;

    public PartStorage(string partPath)
    {
        _partPath = partPath;
        _stream = new FileStream(partPath, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite);
        _writer = new BinaryWriter(_stream, Encoding.UTF8, true);
        _partHeader = PartHeader.Read(partPath);
        _stream.Seek(_partHeader.WritePosition, SeekOrigin.Begin);
        PartNumber =  _partHeader.PartNumber;
    }

    public PartStorage(string rootPath, int partNumber, int partSize)
    {
        PartNumber = partNumber;
        _partPath = Path.Combine(rootPath, $"{partNumber:00000000}.lss");
        _stream = new FileStream(_partPath, FileMode.CreateNew, FileAccess.ReadWrite, FileShare.ReadWrite);
        _stream.SetLength(partSize);
        _writer = new BinaryWriter(_stream, Encoding.UTF8, true);
        var now = DateTimeOffset.UtcNow;
        _partHeader = new PartHeader(partNumber, PartHeader.Size, now, now);
        PartHeader.Write(_partPath, _partHeader);
        _writer.Flush();
    }

    public int PartNumber { get; }

    public bool TryWrite(byte[] data, out long offset)
    {
        try
        {
            _lock.EnterWriteLock();
            if (_stream.Length < _stream.Position + sizeof(int) + data.Length)
            {
                _partHeader = PartHeader.Close(_partPath, _partHeader);
                offset = 0;
                return false;
            }

            offset = _stream.Position;
            _writer.Write(data.Length);
            _writer.Write(data);
            _writer.Flush();
            _partHeader =  PartHeader.Append(_partPath, _partHeader, _stream.Position);
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
            using var stream = new FileStream(_partPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using var reader = new BinaryReader(stream, Encoding.UTF8, true);
            _stream.Seek(offset, SeekOrigin.Begin);
            var size = reader.ReadInt32();
            return reader.ReadBytes(size);
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    public void Dispose()
    {
        _stream.Dispose();
        _lock.Dispose();
    }
}

internal record PartHeader(
    int PartNumber,
    long WritePosition,
    DateTimeOffset MinTime,
    DateTimeOffset MaxTime)
{
    public static long Size =>  sizeof(int) + sizeof(long) * 3;
    
    public static PartHeader Read(string partPath)
    {
        using var stream = new FileStream(partPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        using var reader = new BinaryReader(stream);
        return new PartHeader(
            reader.ReadInt32(),
            reader.ReadInt64(),
            DateTimeOffset.FromUnixTimeSeconds(reader.ReadInt64()),
            DateTimeOffset.FromUnixTimeSeconds(reader.ReadInt64()));
    }

    public static void Write(string partPath, PartHeader header)
    {
        using var stream = new FileStream(partPath, FileMode.Open, FileAccess.Write, FileShare.ReadWrite);
        using var writer = new BinaryWriter(stream);
        writer.Write(header.PartNumber);
        writer.Write(header.WritePosition);
        writer.Write(header.MinTime.ToUnixTimeSeconds());
        writer.Write(header.MaxTime.ToUnixTimeSeconds());
    }

    public static PartHeader Close(string partPath, PartHeader header)
    {
        var partHeader = header with { WritePosition = -1 };
        Write(partPath, partHeader);
        return partHeader;
    }
    
    public static PartHeader Append(string partPath, PartHeader header, long offset)
    {
        var partHeader = header with { WritePosition = offset };
        Write(partPath, partHeader);
        return partHeader;
    }
}