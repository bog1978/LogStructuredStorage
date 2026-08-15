using System.Text;

namespace Storage.Node;

internal sealed class PartStorage : IDisposable
{
    private BinaryWriter? _writer;
    private readonly ReaderWriterLockSlim _lock = new();
    private PartHeader _partHeader;

    public PartStorage(string partPath)
    {
        PartPath = partPath;
        var stream = new FileStream(partPath, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite);
        _partHeader = stream.ReadHeader();
        if (_partHeader.WritePosition > 0)
        {
            _writer = new BinaryWriter(stream);
            stream.Seek(_partHeader.WritePosition, SeekOrigin.Begin);
        }

        PartNumber = _partHeader.PartNumber;
    }

    public PartStorage(string rootPath, int partNumber, int partSize)
    {
        PartNumber = partNumber;
        if(!Directory.Exists(rootPath))
            Directory.CreateDirectory(rootPath);
        PartPath = Path.Combine(rootPath, $"{partNumber:00000000}.lss");
        var stream = new FileStream(PartPath, FileMode.CreateNew, FileAccess.ReadWrite, FileShare.ReadWrite);
        stream.SetLength(partSize);
        _writer = new BinaryWriter(stream);
        _partHeader = _writer.CreateHeader(partNumber);
    }

    public int PartNumber { get; }

    public string PartPath { get; }
    
    public bool CanWrite => _writer != null;

    public bool TryWrite(byte[] data, out long offset)
    {
        if (_writer == null)
            throw new InvalidOperationException("Part is read only.");
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

    public void Close()
    {
        _writer?.Dispose();
        _writer = null;
    }

    public void Dispose()
    {
        Close();
        _writer?.Dispose();
        _lock.Dispose();
    }
}