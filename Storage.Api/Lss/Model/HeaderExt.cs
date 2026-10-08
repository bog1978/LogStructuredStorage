using System.Text;

namespace Storage.Api.Lss.Model;

internal static class HeaderExt
{
    private static readonly byte[] FormatMagic = "LSS2"u8.ToArray();
    private const byte FormatVersion = 2;

    internal const int HeaderZoneSize = 100 * 1024;
    internal const int HeaderSize = sizeof(int) + sizeof(byte) + sizeof(int) + sizeof(byte) +
                                    sizeof(long) * 4 + sizeof(int);
    internal const int IndexEntrySize = sizeof(long) * 2 + sizeof(int) + sizeof(long);
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
        public PartHeader CreatePartHeader(PartHeader header) => writer.UpdatePartHeader(
            header with { WritePosition = HeaderZoneSize });

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

        public PartHeader UpdateWritePosition(PartHeader header, long writePosition, DateTimeOffset fileCreatedAt)
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
            writer.Write(entry.RecordLength);
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
            writer.Write(FormatMagic);
            writer.Write(FormatVersion);
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
        public PartHeader ReadPartHeader()
        {
            if (reader.BaseStream.Position != 0)
                throw new InvalidOperationException("Invalid position");

            var magic = reader.ReadBytes(FormatMagic.Length);
            if (!magic.AsSpan().SequenceEqual(FormatMagic))
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
            var writePosition = reader.ReadInt64();
            var committedFileCount = reader.ReadInt32();
            if (committedFileCount < 0 || committedFileCount > MaxIndexEntries)
                throw new InvalidDataException($"Некорректное число индексных записей: {committedFileCount}.");

            return new PartHeader(
                partNumber,
                writePosition,
                partType,
                minTime,
                maxTime,
                createdAt,
                committedFileCount);
        }

        public PartIndexEntry ReadIndexEntry()
        {
            var recordOffset = reader.ReadInt64();
            var recordLength = reader.ReadInt64();
            var fileLength = reader.ReadInt32();
            var createdAt = DateTimeOffset.FromUnixTimeMilliseconds(reader.ReadInt64());
            return new PartIndexEntry(recordOffset, recordLength, fileLength, createdAt);
        }
    }
}
