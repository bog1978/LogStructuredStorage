using System.Text;

namespace Storage.Api.Lss;

internal static class HeaderExt
{
    private static long Size => sizeof(int) + sizeof(long) * 3;

    public static PartHeader ReadHeader(this Stream stream)
    {
        using var reader = new BinaryReader(stream, Encoding.UTF8, true);
        return reader.ReadHeader();
    }

    public static PartHeader CreateHeader(this BinaryWriter writer, int partNumber)
    {
        var now = DateTimeOffset.UtcNow;
        var header = new PartHeader(partNumber, Size, now, now);        
        writer.WriteHeader(header);
        return header;
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
        WriteHeader(writer, header);
        writer.BaseStream.Position = position;
    }

    private static void WriteHeader(this BinaryWriter writer, PartHeader header)
    {
        writer.Write(header.PartNumber);
        writer.Write(header.MinTime.ToUnixTimeSeconds());
        writer.Write(header.MaxTime.ToUnixTimeSeconds());
        writer.Write(header.WritePosition);
        writer.Flush();
    }
    
    private static PartHeader ReadHeader(this BinaryReader reader)
    {
        var partNumber = reader.ReadInt32();
        var minTime = reader.ReadInt64();
        var maxTime = reader.ReadInt64();
        var writePosition = reader.ReadInt64();
        return new PartHeader(
            partNumber,
            writePosition,
            DateTimeOffset.FromUnixTimeSeconds(minTime),
            DateTimeOffset.FromUnixTimeSeconds(maxTime));
    }
}