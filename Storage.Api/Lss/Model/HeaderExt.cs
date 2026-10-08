using System.Text;

namespace Storage.Api.Lss.Model;

internal static class HeaderExt
{
    internal const uint FormatMagic = 0x3253534C; // "LSS2" in little-endian byte order.
    internal const byte FormatVersion = 2;
    private const int MaxTextFieldCharacters = 255;

    internal const int HeaderZoneSize = 100 * 1024;
    internal const int HeaderSize
        = sizeof(uint)
        + sizeof(byte) * 2
        + sizeof(int) * 3
        + sizeof(long) * 3;
    internal const int IndexEntrySize
        = sizeof(int) * 2 
        + sizeof(long);
    internal const int MaxIndexEntries = (HeaderZoneSize - HeaderSize) / IndexEntrySize;

    extension(Stream stream)
    {
        public PartHeader ReadPartHeader()
        {
            using var reader = new BinaryReader(stream, Encoding.UTF8, leaveOpen: true);
            return reader.ReadPartHeader();
        }
    }

    extension(BinaryWriter writer)
    {
        public void WriteDataEntry(PartDataEntry entry)
        {
            ValidateTextLength(entry.FileName);
            ValidateTextLength(entry.ContentType);
            writer.Write(entry.FileName);
            writer.Write(entry.ContentType);
        }

        public PartHeader CreatePartHeader(PartHeader header) => writer.UpdatePartHeader(
            header with
            {
                Magic = FormatMagic,
                Version = FormatVersion,
                WritePosition = HeaderZoneSize
            });

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

        public PartHeader UpdateWritePosition(PartHeader header, int writePosition, DateTimeOffset fileCreatedAt)
        {
            var minTime = header.CommittedFileCount == 0 || fileCreatedAt < header.MinTime
                ? fileCreatedAt
                : header.MinTime;
            var maxTime = header.CommittedFileCount == 0 || fileCreatedAt > header.MaxTime
                ? fileCreatedAt
                : header.MaxTime;

            return writer.UpdatePartHeader(header with
            {
                WritePosition = writePosition,
                MinTime = minTime,
                MaxTime = maxTime,
                CommittedFileCount = checked(header.CommittedFileCount + 1)
            });
        }

        public void WriteIndexEntry(PartIndexEntry entry)
        {
            writer.Write(entry.RecordOffset);
            writer.Write(entry.FileLength);
            writer.Write(entry.CreatedAt.ToUnixTimeMilliseconds());
        }

        private PartHeader UpdatePartHeader(PartHeader header)
        {
            var stream = writer.BaseStream;
            var position = stream.Position;
            stream.Position = 0;
            writer.WritePartHeader(header);
            stream.Position = position;
            return header;
        }

        public PartHeader WritePartHeader(PartHeader header)
        {
            writer.Write(header.Magic);
            writer.Write(header.Version);
            writer.Write(header.PartNumber);
            writer.Write((byte)header.PartType);
            writer.Write(header.CreatedAt.ToUnixTimeMilliseconds());
            writer.Write(header.MinTime.ToUnixTimeMilliseconds());
            writer.Write(header.MaxTime.ToUnixTimeMilliseconds());
            writer.Write(header.WritePosition);
            writer.Write(header.CommittedFileCount);
            writer.Flush();
            return header;
        }
    }

    extension(BinaryReader reader)
    {
        public PartDataEntry ReadDataEntry() => new(reader.ReadString(), reader.ReadString());

        public PartHeader ReadPartHeader()
        {
            if (reader.BaseStream.Position != 0)
                throw new InvalidOperationException("Invalid position");

            var magic = reader.ReadUInt32();
            if (magic != FormatMagic)
                throw new InvalidDataException("Неверная сигнатура файла раздела.");

            var version = reader.ReadByte();
            if (version != FormatVersion)
                throw new InvalidDataException($"Неподдерживаемая версия файла раздела: {version}.");

            var partNumber = reader.ReadInt32();
            var partType = (PartTypeEnum)reader.ReadByte();
            if (!Enum.IsDefined(partType))
                throw new InvalidDataException($"Неизвестный тип раздела: {(byte)partType}.");

            var createdAt = DateTimeOffset.FromUnixTimeMilliseconds(reader.ReadInt64());
            var minTime = DateTimeOffset.FromUnixTimeMilliseconds(reader.ReadInt64());
            var maxTime = DateTimeOffset.FromUnixTimeMilliseconds(reader.ReadInt64());
            var writePosition = reader.ReadInt32();
            var committedFileCount = reader.ReadInt32();
            if (committedFileCount < 0 || committedFileCount > MaxIndexEntries)
                throw new InvalidDataException($"Некорректное число индексных записей: {committedFileCount}.");

            return new PartHeader(
                magic,
                version,
                partNumber,
                partType,
                createdAt,
                minTime,
                maxTime,
                writePosition,
                committedFileCount);
        }

        public PartIndexEntry ReadIndexEntry()
        {
            var recordOffset = reader.ReadInt32();
            var fileLength = reader.ReadInt32();
            var createdAt = DateTimeOffset.FromUnixTimeMilliseconds(reader.ReadInt64());
            return new PartIndexEntry(recordOffset, fileLength, createdAt);
        }
    }

    private static void ValidateTextLength(string value)
    {
        if (value.Length > MaxTextFieldCharacters)
            throw new InvalidDataException($"Текстовое поле превышает допустимый размер {MaxTextFieldCharacters} символов.");
    }
}
