namespace Storage.Api.Lss.Model;

internal record FileHeader(
    string FileName,
    string ContentType,
    int Length,
    DateTimeOffset CreatedAt)
{
    public static byte[] ToBytes(FileHeader header)
    {
        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream);
        writer.Write(header.FileName);
        writer.Write(header.ContentType);
        writer.Write(header.CreatedAt.ToUnixTimeMilliseconds());
        writer.Write(header.Length);
        stream.Flush();
        return stream.ToArray();
    }

    public static FileHeader ToHeader(BinaryReader reader)
    {
        var fileName = reader.ReadString();
        var contentType = reader.ReadString();
        var createdAt = DateTimeOffset.FromUnixTimeMilliseconds(reader.ReadInt64());
        var length = reader.ReadInt32();
        return new FileHeader(fileName, contentType, length, createdAt);
    }
}