namespace Storage.Api.Dto;

internal sealed record FileDto(
    string FileName,
    long FileSize,
    DateTimeOffset CreatedAt);