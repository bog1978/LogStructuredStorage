namespace Storage.Api.Lss;

public interface IBucketStorage : IDisposable
{
    DataLocation Write(byte[] data);
    byte[] Read(DataLocation location);
}