using Storage.Node;

namespace Storage.Api.Dto;

internal static class MappingExt
{
    extension(Storage.Cluster.Model.Node node)
    {
        public NodeDto ToDto() => new(node.NodeId, node.HostName);
    }

    extension(Storage.Cluster.Model.Bucket bucket)
    {
        public BucketDto ToDto() => new(bucket.BucketId, bucket.NodeId, bucket.Ttl);
    }

    extension(Storage.Cluster.Model.File file)
    {
        public DataLocation Location => new(file.BucketId, file.PartId, file.PartOffset);

        public FileDto ToDto() => new(file.FileName, file.FileSize, file.CreatedAt);
    }
}