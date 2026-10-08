using System.Text;
using Storage.Api.Lss.Model;

namespace Storage.Api.Lss;

/// <summary>
/// Opens a partition and validates its header and committed index.
/// </summary>
internal static class PartFileLoader
{
    internal static (PartHeader Header, List<PartIndexEntry> IndexEntries, BinaryWriter? Writer) Load(string partPath)
    {
        var stream = new FileStream(partPath, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite);
        try
        {
            if (stream.Length < HeaderExt.HeaderZoneSize)
                throw new InvalidDataException($"Файл раздела короче служебной зоны: {partPath}");

            var partHeader = stream.ReadPartHeader();
            var indexEntries = ReadIndexEntries(stream, partHeader.CommittedFileCount);
            var expectedWritePosition = ValidateIndex(stream, indexEntries, partPath);

            if (partHeader.PartType == PartTypeEnum.Hot)
                return OpenWriter(stream, partHeader, indexEntries, expectedWritePosition, partPath);

            if (partHeader.WritePosition != -1)
                throw new InvalidDataException($"У неактивного раздела указан WritePosition: {partHeader.WritePosition}");

            stream.Dispose();
            return (partHeader, indexEntries, null);
        }
        catch
        {
            stream.Dispose();
            throw;
        }
    }

    private static List<PartIndexEntry> ReadIndexEntries(Stream stream, int count)
    {
        var entries = new List<PartIndexEntry>(count);
        stream.Position = HeaderExt.HeaderSize;
        using var reader = new BinaryReader(stream, Encoding.UTF8, leaveOpen: true);
        for (var i = 0; i < count; i++)
            entries.Add(reader.ReadIndexEntry());

        return entries;
    }

    private static long ValidateIndex(Stream stream, IReadOnlyList<PartIndexEntry> entries, string partPath)
    {
        var expectedOffset = (long)HeaderExt.HeaderZoneSize;
        foreach (var entry in entries)
        {
            if (entry.RecordOffset != expectedOffset ||
                entry.RecordLength < sizeof(int) * 2L + entry.FileLength ||
                entry.FileLength < 0 ||
                entry.RecordLength > stream.Length - entry.RecordOffset)
            {
                throw new InvalidDataException($"Некорректная индексная запись в разделе {partPath}.");
            }

            expectedOffset = checked(entry.RecordOffset + entry.RecordLength);
        }

        return expectedOffset;
    }

    private static (PartHeader Header, List<PartIndexEntry> IndexEntries, BinaryWriter? Writer) OpenWriter(
        FileStream stream,
        PartHeader header,
        List<PartIndexEntry> entries,
        long expectedWritePosition,
        string partPath)
    {
        if (header.WritePosition != expectedWritePosition ||
            header.WritePosition < HeaderExt.HeaderZoneSize ||
            header.WritePosition > stream.Length)
        {
            throw new InvalidDataException($"Некорректная позиция записи в разделе {partPath}: {header.WritePosition}");
        }

        var writer = new BinaryWriter(stream);
        stream.Position = header.WritePosition;
        return (header, entries, writer);
    }
}
