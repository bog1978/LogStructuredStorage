namespace Storage.Api.Lss;

internal static class NodStorageExt
{
    public static Task Read(
        this INodeStorage nodeStorage,
        DataLocation location,
        Action<FileHeader> headerCallback,
        Stream outStream,
        CancellationToken token) => 
        nodeStorage
            .GetBucket(location.BucketName)
            .Read(location, headerCallback, outStream, token);
    
    public static Task<DataLocation> Write(
        this INodeStorage nodeStorage,
        string bucketName,
        FileHeader fileHeader,
        Stream data,
        CancellationToken token) => 
        nodeStorage
            .GetOrCreateBucket(bucketName)
            .Write(fileHeader, data, token);
}