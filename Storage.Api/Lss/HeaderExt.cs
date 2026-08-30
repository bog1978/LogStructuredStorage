using System.Text;

namespace Storage.Api.Lss;

internal static class HeaderExt
{
    private static long Size => sizeof(int) + sizeof(long) * 3;

    extension(Stream stream)
    {
        public PartHeader ReadPartHeader()
        {
            using var reader = new BinaryReader(stream, Encoding.UTF8, true);
            return reader.ReadPartHeader();
        }
    }

    extension(BinaryWriter writer)
    {
        public PartHeader CreatePartHeader(int partNumber)
        {
            var now = DateTimeOffset.UtcNow;
            var header = new PartHeader(partNumber, Size, now, now);        
            writer.WritePartHeader(header);
            return header;
        }

        public PartHeader ClosePart(PartHeader header)
        {
            var partHeader = header with { WritePosition = -1 };
            UpdatePartHeader(writer, partHeader);
            return partHeader;
        }

        public PartHeader UpdateWriteOffset(PartHeader header)
        {
            var partHeader = header with { WritePosition = writer.BaseStream.Position };
            UpdatePartHeader(writer, partHeader);
            return partHeader;
        }

        private void UpdatePartHeader(PartHeader header)
        {
            var position = writer.BaseStream.Position;
            writer.BaseStream.Position = 0;
            WritePartHeader(writer, header);
            writer.BaseStream.Position = position;
        }

        private void WritePartHeader(PartHeader header)
        {
            writer.Write(header.PartNumber);
            writer.Write(header.MinTime.ToUnixTimeSeconds());
            writer.Write(header.MaxTime.ToUnixTimeSeconds());
            writer.Write(header.WritePosition);
            writer.Flush();
        }

        public void WriteFileHeader(FileHeader header)
        {
            writer.Write(header.FileName);
            writer.Write(header.ContentType);
            writer.Write(header.CreatedAt.ToUnixTimeMilliseconds());
            writer.Write(header.Length);
        }
    }

    extension(BinaryReader reader)
    {
        private PartHeader ReadPartHeader()
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

        public FileHeader ReadFileHeader()
        {
            var fileName = reader.ReadString();
            var contentType = reader.ReadString();
            var createdAt = DateTimeOffset.FromUnixTimeMilliseconds(reader.ReadInt64());
            var length = reader.ReadInt32();
            return new FileHeader(fileName, contentType, length, createdAt);
        }
    }
}