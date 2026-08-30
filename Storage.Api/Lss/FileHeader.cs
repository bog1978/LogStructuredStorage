namespace Storage.Api.Lss;

internal record FileHeader(
    string FileName,
    string ContentType,
    int Length,
    DateTimeOffset CreatedAt);