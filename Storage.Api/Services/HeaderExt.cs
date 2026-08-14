namespace Storage.Api.Services;

internal static class HeaderExt
{
    public static long Size => sizeof(int) + sizeof(long) * 3;

    public static PartHeader ReadHeader(this Stream stream)
    {
        using var reader = new BinaryReader(stream);
        return new PartHeader(
            reader.ReadInt32(),
            reader.ReadInt64(),
            DateTimeOffset.FromUnixTimeSeconds(reader.ReadInt64()),
            DateTimeOffset.FromUnixTimeSeconds(reader.ReadInt64()));
    }

    public static void CreateHeader(this BinaryWriter writer, PartHeader header)
    {
        Write(writer, header);
    }

    public static PartHeader ClosePart(this BinaryWriter writer, PartHeader header)
    {
        var partHeader = header with { WritePosition = -1 };
        Update(writer, partHeader);
        return partHeader;
    }

    public static PartHeader UpdateWriteOffset(this BinaryWriter writer, PartHeader header)
    {
        var partHeader = header with { WritePosition = writer.BaseStream.Position };
        Update(writer, partHeader);
        return partHeader;
    }

    private static void Update(this BinaryWriter writer, PartHeader header)
    {
        var position = writer.BaseStream.Position;
        writer.BaseStream.Position = 0;
        Write(writer, header);
        writer.BaseStream.Position = position;
    }

    private static void Write(this BinaryWriter writer, PartHeader header)
    {
        writer.Write(header.PartNumber);
        writer.Write(header.WritePosition);
        writer.Write(header.MinTime.ToUnixTimeSeconds());
        writer.Write(header.MaxTime.ToUnixTimeSeconds());
        writer.Flush();
    }
}