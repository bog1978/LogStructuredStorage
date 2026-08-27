using Storage.Cluster;
using Model = Storage.Cluster.DataAccess.Model;

namespace Storage.Api.Dto;

internal static class MappingExt
{
    extension(Model.Node node)
    {
        public NodeDto ToDto() => new(node.NodeId, node.HostName);
    }

    extension(Model.Bucket bucket)
    {
        public BucketDto ToDto() => new(bucket.BucketId, bucket.NodeId, bucket.Ttl);
    }

    extension(Model.File file)
    {
        public DataLocation Location => new(file.BucketId, file.PartId, file.PartOffset);

        public FileDto ToDto() => new(file.FileName, file.FileSize, file.CreatedAt);
    }
}