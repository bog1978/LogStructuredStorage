namespace Storage.Node;

public interface IBucketStorage : IDisposable
{
    DataLocation Write(byte[] data);
    byte[] Read(DataLocation location);
}