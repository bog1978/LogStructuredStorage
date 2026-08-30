namespace Storage.Api.Lss;

internal static class NodStorageExt
{
    public static (FileHeader fileHeader, byte[] data) Read(this INodeStorage nodeStorage, DataLocation location) => 
        nodeStorage.GetBucket(location.BucketName).Read(location);
    
    public static DataLocation Write(this INodeStorage nodeStorage, string bucketName, FileHeader fileHeader, byte[] data) => 
        nodeStorage.GetOrCreateBucket(bucketName).Write(fileHeader, data);
}