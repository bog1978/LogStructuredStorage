using Storage.Node;

namespace Storage.Api.Dto;

internal static class MappingExt
{
    extension(Storage.Db.Cluster.Node node)
    {
        public NodeDto ToDto() => new(node.NodeId, node.HostName);
    }

    extension(Storage.Db.Cluster.Bucket bucket)
    {
        public BucketDto ToDto() => new(bucket.BucketId, bucket.NodeId, bucket.Ttl);
    }

    extension(Storage.Db.Cluster.File file)
    {
        public DataLocation Location => new(file.BucketId, file.PartId, file.PartOffset);

        public FileDto ToDto() => new(file.FileName, file.FileSize, file.CreatedAt);
    }
}