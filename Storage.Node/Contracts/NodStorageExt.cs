namespace Storage.Node;

public static class NodStorageExt
{
    public static byte[] Read(this INodeStorage nodeStorage, DataLocation location) => 
        nodeStorage.GetBucket(location.BucketName).Read(location);
    
    public static DataLocation Write(this INodeStorage nodeStorage, string bucketName, byte[] data) => 
        nodeStorage.GetOrCreateBucket(bucketName).Write(data);
}