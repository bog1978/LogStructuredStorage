using System.Text;

namespace Storage.Api.Lss;

internal static class HeaderExt
{
    private static long Size => sizeof(int) + sizeof(long) * 3 + sizeof(byte);

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
        public PartHeader CreatePartHeader(PartHeader header)
        {
            writer.BaseStream.Position = 0;
            return writer.WritePartHeader(header with { WritePosition = Size });
        }

        public PartHeader MakeWarmPart(PartHeader header) => writer.UpdatePartHeader(
            header with
            {
                PartType = PartTypeEnum.Warm,
                WritePosition = -1
            });

        public PartHeader MakeColdPart(PartHeader header) => writer.UpdatePartHeader(
            header with
            {
                PartType = PartTypeEnum.Cold,
                WritePosition = -1
            });
        
        public PartHeader UpdateWriteOffset(PartHeader header) => writer.UpdatePartHeader(
            header with
            {
                WritePosition = writer.BaseStream.Position
            });

        private PartHeader UpdatePartHeader(PartHeader header)
        {
            var position = writer.BaseStream.Position;
            writer.BaseStream.Position = 0;
            writer.WritePartHeader(header);
            writer.BaseStream.Position = position;
            return header;
        }

        private PartHeader WritePartHeader(PartHeader header)
        {
            var pt = (byte)header.PartType;
            writer.Write(header.PartNumber);
            writer.Write(pt);
            writer.Write(header.MinTime.ToUnixTimeSeconds());
            writer.Write(header.MaxTime.ToUnixTimeSeconds());
            writer.Write(header.WritePosition);
            writer.Flush();
            return header;
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
            if (reader.BaseStream.Position > 0)
                throw new InvalidOperationException("Invalid position");

            var partNumber = reader.ReadInt32();
            var partType = reader.ReadByte();
            var minTime = reader.ReadInt64();
            var maxTime = reader.ReadInt64();
            var writePosition = reader.ReadInt64();
            return new PartHeader(
                partNumber,
                writePosition,
                (PartTypeEnum)partType,
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