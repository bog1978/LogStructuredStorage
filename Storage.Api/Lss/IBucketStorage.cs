namespace Storage.Api.Lss;

internal interface IBucketStorage : IDisposable
{
    DataLocation Write(FileHeader fileHeader, Stream data);
    (FileHeader fileHeader, byte[] data) Read(DataLocation location);
}