namespace Storage.Cluster;

public interface IBucketStorage : IDisposable
{
    DataLocation Write(byte[] data);
    byte[] Read(DataLocation location);
}