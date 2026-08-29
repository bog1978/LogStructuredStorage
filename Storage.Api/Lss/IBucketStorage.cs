namespace Storage.Api.Lss;

internal interface IBucketStorage : IDisposable
{
    DataLocation Write(byte[] data);
    byte[] Read(DataLocation location);
}