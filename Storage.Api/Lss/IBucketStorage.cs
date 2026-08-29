namespace Storage.Api.Lss;

internal interface IBucketStorage : IDisposable
{
    DataLocation Write(string fileName, byte[] data);
    (string fileName, byte[] data, DateTimeOffset createdAt) Read(DataLocation location);
}