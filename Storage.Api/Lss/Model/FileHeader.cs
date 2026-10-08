namespace Storage.Api.Lss.Model;

internal sealed record FileHeader(
    string FileName,
    string ContentType,
    int Length,
    DateTimeOffset CreatedAt);
