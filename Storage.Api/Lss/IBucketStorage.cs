namespace Storage.Api.Lss;

internal interface IBucketStorage : IDisposable
{
    DataLocation Write(FileHeader fileHeader, byte[] data);
    (FileHeader fileHeader, byte[] data) Read(DataLocation location);
}