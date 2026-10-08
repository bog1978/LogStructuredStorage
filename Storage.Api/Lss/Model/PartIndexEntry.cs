namespace Storage.Api.Lss.Model;

internal readonly record struct PartIndexEntry(
    int RecordOffset,
    int FileLength,
    DateTimeOffset CreatedAt);
