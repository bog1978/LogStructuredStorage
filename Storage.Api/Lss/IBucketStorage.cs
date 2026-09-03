namespace Storage.Api.Lss;

internal interface IBucketStorage : IDisposable
{
    Task<DataLocation> Write(FileHeader fileHeader, Stream data, CancellationToken token);
    Task Read(DataLocation location, Action<FileHeader> headerCallback, Stream outStream, CancellationToken token);
}