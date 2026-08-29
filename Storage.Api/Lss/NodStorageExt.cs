namespace Storage.Api.Lss;

internal static class NodStorageExt
{
    public static (string fileName, byte[] data, DateTimeOffset createdAt) Read(this INodeStorage nodeStorage, DataLocation location) => 
        nodeStorage.GetBucket(location.BucketName).Read(location);
    
    public static DataLocation Write(this INodeStorage nodeStorage, string bucketName, string fileName, byte[] data) => 
        nodeStorage.GetOrCreateBucket(bucketName).Write(fileName, data);
}