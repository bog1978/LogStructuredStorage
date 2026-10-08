using Model = Storage.Cluster.DataAccess.Model;

namespace Storage.Api.Dto;

internal static class MappingExt
{
    extension(Model.Node node)
    {
        public NodeDto ToDto() => new(
            node.NodeName,
            node.HostName);
    }

    extension(Model.Bucket bucket)
    {
        public BucketDto ToDto() => new(
            bucket.BucketName,
            bucket.NodeId,
            bucket.TtlHot,
            bucket.TtlCold);
    }

    public static string GetFileKey(string nodeName, string bucketName, int partNumber, int fileIndex) =>
        $"{nodeName}:{bucketName}:{partNumber}:{fileIndex}";
}
