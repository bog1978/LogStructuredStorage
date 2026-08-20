namespace Storage.Api.Dto;

public record FileDto(
    string FileName,
    string BucketId,
    string NodeId);