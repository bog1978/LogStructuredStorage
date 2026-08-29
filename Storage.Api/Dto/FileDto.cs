namespace Storage.Api.Dto;

internal sealed record FileDto(
    string Key,
    string FileName,
    long FileSize,
    DateTimeOffset CreatedAt);