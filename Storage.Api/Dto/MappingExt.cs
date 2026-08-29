using Storage.Api.Lss;
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

    extension(Model.File file)
    {
        public DataLocation Location => new(
            file.BucketId,
            file.PartId,
            file.PartOffset);

        public FileDto ToDto() => new(
            GetFileKey(file.NodeId, file.BucketId, file.PartId, file.PartOffset),
            file.FileName,
            file.FileSize,
            file.CreatedAt);
    }

    public static string GetFileKey(string nodeName, string bucketName, int partId, long offset) => 
        $"{nodeName}:{bucketName}:{partId}:{offset}";
}