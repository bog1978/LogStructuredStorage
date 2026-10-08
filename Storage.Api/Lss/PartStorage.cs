using System.Text;
using DotNext.Threading;
using Storage.Api.Exceptions;
using Storage.Api.Internal;
using Storage.Api.Lss.Model;

namespace Storage.Api.Lss;

internal sealed class PartStorage : IDisposable
{
    private const int MaxTextFieldBytes = 64 * 1024;
    private static readonly Encoding StrictUtf8 = new UTF8Encoding(false, true);

    private readonly AsyncReaderWriterLock _lock = new();
    private readonly List<PartIndexEntry> _indexEntries;
    private BinaryWriter? _writer;
    private PartHeader _partHeader;
    private string _partPath;
    private bool _disposed;

    private PartStorage(string partPath, PartHeader partHeader, List<PartIndexEntry> indexEntries, BinaryWriter? writer)
    {
        _partPath = partPath;
        _partHeader = partHeader;
        _indexEntries = indexEntries;
        _writer = writer;
        PartNumber = _partHeader.PartNumber;
    }

    public bool IsHot => _partHeader.PartType == PartTypeEnum.Hot;

    public int PartNumber { get; }

    internal string PartPath => _partPath;

    public async Task<long> TryWrite(FileHeader fileHeader, Stream inStream, CancellationToken token)
    {
        using var activity = StorageTelemetry.Activity.StartActivity()
            ?.WithDisplayName($"Запись файла {fileHeader.FileName} в раздел {_partPath}");
        var isLocked = false;
        try
        {
            await _lock.EnterWriteLockAsync(token);
            isLocked = true;

            if (_partHeader.PartType != PartTypeEnum.Hot)
            {
                activity?.AddEvent($"Раздел {_partPath} в статусе {_partHeader.PartType} — запись невозможна");
                return -1;
            }

            if (_writer == null)
                throw new InvalidOperationException("Writer равен null");
            if (fileHeader.Length < 0)
                throw new InvalidDataException("Размер файла не может быть отрицательным.");
            if (inStream.CanSeek && inStream.Length - inStream.Position != fileHeader.Length)
                throw new InvalidOperationException("Несоответствие длины файла");

            var fileNameBytes = StrictUtf8.GetBytes(fileHeader.FileName);
            var contentTypeBytes = StrictUtf8.GetBytes(fileHeader.ContentType);
            if (fileNameBytes.Length > MaxTextFieldBytes || contentTypeBytes.Length > MaxTextFieldBytes)
                throw new InvalidDataException($"Метаданные файла превышают {MaxTextFieldBytes} байт.");

            var recordLength = checked(
                sizeof(int) + (long)fileNameBytes.Length +
                sizeof(int) + (long)contentTypeBytes.Length +
                fileHeader.Length);
            var recordOffset = _partHeader.WritePosition;
            var stream = _writer.BaseStream;

            if (_indexEntries.Count >= HeaderExt.MaxIndexEntries ||
                recordOffset < HeaderExt.HeaderZoneSize ||
                recordLength > stream.Length - recordOffset)
            {
                _partHeader = _writer.MakeWarmPart(_partHeader);
                Close();
                activity?.AddEvent($"Запись файла {fileHeader.FileName} в раздел {_partPath} не удалась: закончилась зона данных или индекс. Раздел переведен в статус 'теплый'.");
                return -1;
            }

            var fileIndex = _indexEntries.Count;
            var recordEnd = checked(recordOffset + recordLength);
            try
            {
                stream.Position = recordOffset;
                _writer.Write(fileNameBytes.Length);
                _writer.Write(fileNameBytes);
                _writer.Write(contentTypeBytes.Length);
                _writer.Write(contentTypeBytes);
                await CopyExactlyAsync(inStream, stream, fileHeader.Length, token);
                _writer.Flush();

                var indexEntry = new PartIndexEntry(recordOffset, recordLength, fileHeader.Length, fileHeader.CreatedAt);
                stream.Position = HeaderExt.HeaderSize + (long)fileIndex * HeaderExt.IndexEntrySize;
                _writer.WriteIndexEntry(indexEntry);
                _writer.Flush();

                _partHeader = _writer.UpdateWritePosition(_partHeader, recordEnd, fileHeader.CreatedAt);
                _indexEntries.Add(indexEntry);
                stream.Position = recordEnd;

                activity?.AddEvent($"Файл {fileHeader.FileName} записан в раздел {_partPath}. Индекс: {fileIndex}");
                return fileIndex;
            }
            catch
            {
                if (_writer != null && _partHeader.PartType == PartTypeEnum.Hot)
                    stream.Position = _partHeader.WritePosition;
                throw;
            }
        }
        catch (Exception ex)
        {
            activity?.SetError(ex);
            throw;
        }
        finally
        {
            if (isLocked)
                _lock.Release();
        }
    }

    public Task Read(long fileIndex, Stream outStream, Action<FileHeader> headersCallback, CancellationToken token)
    {
        var bucketName = Path.GetFileName(Path.GetDirectoryName(_partPath)) ?? string.Empty;
        return Read(fileIndex, bucketName, outStream, headersCallback, token);
    }

    public async Task Read(
        long fileIndex,
        string bucketName,
        Stream outStream,
        Action<FileHeader> headersCallback,
        CancellationToken token)
    {
        using var activity = StorageTelemetry.Activity.StartActivity()
            ?.WithDisplayName($"Чтение файла с индексом {fileIndex} из раздела {_partPath}");
        var isLocked = false;
        try
        {
            await _lock.EnterReadLockAsync(token);
            isLocked = true;

            if (fileIndex < 0 || fileIndex >= _indexEntries.Count)
                throw new BucketFileNotFoundException(bucketName, $"индекс {fileIndex} в разделе {PartNumber}");

            var entry = _indexEntries[(int)fileIndex];
            var recordEnd = checked(entry.RecordOffset + entry.RecordLength);
            await using var stream = new FileStream(_partPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            if (entry.RecordOffset < HeaderExt.HeaderZoneSize ||
                entry.RecordLength < sizeof(int) * 2L + entry.FileLength ||
                entry.FileLength < 0 || recordEnd > stream.Length)
            {
                throw new InvalidDataException($"Некорректная индексная запись {fileIndex} в разделе {_partPath}.");
            }

            stream.Position = entry.RecordOffset;
            using var reader = new BinaryReader(stream, StrictUtf8, leaveOpen: true);
            var fileName = ReadTextField(reader, recordEnd);
            var contentType = ReadTextField(reader, recordEnd);
            if (stream.Position + entry.FileLength != recordEnd)
                throw new InvalidDataException($"Размер записи {fileIndex} не соответствует индексной записи.");

            var fileHeader = new FileHeader(fileName, contentType, entry.FileLength, entry.CreatedAt);
            headersCallback(fileHeader);
            await CopyExactlyAsync(stream, outStream, entry.FileLength, token);
            activity?.AddEvent($"Файл {fileHeader.FileName} прочитан из раздела {_partPath}. Размер: {fileHeader.Length} байт");
        }
        catch (Exception ex)
        {
            activity?.SetError(ex);
            throw;
        }
        finally
        {
            if (isLocked)
                _lock.Release();
        }
    }

    public async Task Delete(CancellationToken token)
    {
        using var activity = StorageTelemetry.Activity.StartActivity()
            ?.WithDisplayName($"Удаление раздела {_partPath}");

        var isLocked = false;
        try
        {
            await _lock.EnterWriteLockAsync(token);
            isLocked = true;
            Close();
            File.Delete(_partPath);
            activity?.AddEvent($"Раздел {_partPath} удален. Статус: Deleted");
            _partHeader = _partHeader with { PartType = PartTypeEnum.Deleted };
        }
        catch (Exception ex)
        {
            activity?.SetError(ex);
            throw;
        }
        finally
        {
            if (isLocked)
                _lock.Release();
        }
    }

    public async Task<PartTypeEnum> ApplyRetentionPolicy(RetentionPolicy policy, string bucketColdDir, CancellationToken token)
    {
        var isLockedRead = false;

        using var activity = StorageTelemetry.Activity.StartActivity()
            ?.WithDisplayName($"Применение политики хранения для раздела {_partPath}");

        try
        {
            await _lock.EnterReadLockAsync(token);
            isLockedRead = true;

            if (_partHeader.PartType == PartTypeEnum.Hot)
            {
                activity?.AddEvent($"Раздел {_partPath} в статусе Hot — политика не применяется");
                return _partHeader.PartType;
            }

            if (_partHeader.MaxTime + policy.TtlHot + policy.TtlCold < DateTimeOffset.UtcNow)
            {
                activity?.AddEvent($"Раздел {_partPath} удален (MaxTime={_partHeader.MaxTime}, TtlHot={policy.TtlHot}, TtlCold={policy.TtlCold})");
                await DeleteInternal(token);
            }
            else if (_partHeader.PartType == PartTypeEnum.Warm &&
                     _partHeader.MaxTime + policy.TtlHot < DateTimeOffset.UtcNow)
            {
                activity?.AddEvent($"Раздел {_partPath} перенесен в холодное хранилище (MaxTime={_partHeader.MaxTime}, TtlHot={policy.TtlHot})");
                await MakeColdInternal(bucketColdDir, token);
            }

            return _partHeader.PartType;
        }
        catch (Exception ex)
        {
            activity?.SetError(ex);
            throw;
        }
        finally
        {
            if (isLockedRead)
                _lock.Release();
        }
    }

    private void Close()
    {
        if (_writer == null)
            return;
        _writer.Flush();
        _writer.Dispose();
        _writer = null;
    }

    public void Dispose()
    {
        if (_disposed)
            return;
        _disposed = true;
        Close();
        _lock.Dispose();
    }

    private async Task MakeColdInternal(string bucketColdDir, CancellationToken token)
    {
        using var activity = StorageTelemetry.Activity.StartActivity()
            ?.WithDisplayName($"Перенос раздела {_partPath} в холодное хранилище");
        var isLocked = false;
        try
        {
            if (!Directory.Exists(bucketColdDir))
            {
                Directory.CreateDirectory(bucketColdDir);
                activity?.AddEvent($"Создана директория для холодных разделов: {bucketColdDir}");
            }

            var newPath = Path.Combine(bucketColdDir, Path.GetFileName(_partPath));
            var sourcePath = _partPath;
            var coldHeader = _partHeader with
            {
                PartType = PartTypeEnum.Cold,
                WritePosition = -1
            };
            var tempPath = Path.Combine(bucketColdDir, $"{Path.GetFileName(newPath)}.{Guid.NewGuid():N}.tmp");
            var movedToFinalPath = false;

            try
            {
                await using (var srcStream = new FileStream(sourcePath, FileMode.Open, FileAccess.Read, FileShare.Read))
                await using (var dstStream = new FileStream(tempPath, FileMode.CreateNew, FileAccess.ReadWrite, FileShare.None))
                {
                    using (var writer = new BinaryWriter(dstStream, Encoding.UTF8, leaveOpen: true))
                        writer.WritePartHeader(coldHeader);

                    srcStream.Position = HeaderExt.HeaderSize;
                    await CopyExactlyAsync(srcStream, dstStream, srcStream.Length - HeaderExt.HeaderSize, token);
                    await dstStream.FlushAsync(token);
                    dstStream.Flush(flushToDisk: true);
                }

                await _lock.UpgradeToWriteLockAsync(token);
                isLocked = true;

                if (_partHeader.PartType != PartTypeEnum.Warm || _partPath != sourcePath)
                {
                    File.Delete(tempPath);
                    activity?.AddEvent($"Перенос раздела отменен: состояние или путь раздела изменились");
                    return;
                }

                Close();
                File.Move(tempPath, newPath);
                movedToFinalPath = true;
                File.Delete(sourcePath);

                _partPath = newPath;
                _partHeader = coldHeader;
                _writer = null;
            }
            catch
            {
                if (File.Exists(tempPath))
                    File.Delete(tempPath);
                if (movedToFinalPath && File.Exists(sourcePath))
                {
                    try
                    {
                        File.Delete(newPath);
                    }
                    catch
                    {
                        // Preserve the original exception; startup can report any leftover duplicate.
                    }
                }
                throw;
            }

            activity?.AddEvent($"Раздел {_partPath} перенесен в холодное хранилище: {newPath}");
        }
        catch (Exception ex)
        {
            activity?.SetError(ex);
            throw;
        }
        finally
        {
            if (isLocked)
                _lock.DowngradeFromWriteLock();
        }
    }

    private async Task DeleteInternal(CancellationToken token)
    {
        using var activity = StorageTelemetry.Activity.StartActivity()
            ?.WithDisplayName($"Удаление раздела {_partPath}");
        var isLocked = false;

        try
        {
            await _lock.UpgradeToWriteLockAsync(token);
            isLocked = true;
            Close();
            File.Delete(_partPath);
            _partHeader = _partHeader with { PartType = PartTypeEnum.Deleted };
        }
        catch (Exception ex)
        {
            activity?.SetError(ex);
            throw;
        }
        finally
        {
            if (isLocked)
                _lock.DowngradeFromWriteLock();
        }
    }

    private static string ReadTextField(BinaryReader reader, long recordEnd)
    {
        if (recordEnd - reader.BaseStream.Position < sizeof(int))
            throw new InvalidDataException("Запись обрывается внутри длины текстового поля.");

        var byteLength = reader.ReadInt32();
        if (byteLength < 0 || byteLength > MaxTextFieldBytes || byteLength > recordEnd - reader.BaseStream.Position)
            throw new InvalidDataException($"Некорректная длина текстового поля: {byteLength}.");

        var bytes = reader.ReadBytes(byteLength);
        if (bytes.Length != byteLength)
            throw new EndOfStreamException("Запись обрывается внутри текстового поля.");
        return StrictUtf8.GetString(bytes);
    }

    private static async Task CopyExactlyAsync(Stream source, Stream destination, long bytesToCopy, CancellationToken token)
    {
        var buffer = new byte[81920];
        while (bytesToCopy > 0)
        {
            var requested = (int)Math.Min(buffer.Length, bytesToCopy);
            var read = await source.ReadAsync(buffer.AsMemory(0, requested), token);
            if (read == 0)
                throw new EndOfStreamException("Поток закончился раньше ожидаемой длины записи.");

            await destination.WriteAsync(buffer.AsMemory(0, read), token);
            bytesToCopy -= read;
        }
    }

    private static (PartHeader Header, List<PartIndexEntry> IndexEntries, BinaryWriter? Writer) LoadPart(string partPath)
    {
        var stream = new FileStream(partPath, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite);
        try
        {
            if (stream.Length < HeaderExt.HeaderZoneSize)
                throw new InvalidDataException($"Файл раздела короче служебной зоны: {partPath}");

            var partHeader = stream.ReadPartHeader();
            var indexEntries = new List<PartIndexEntry>(partHeader.CommittedFileCount);
            stream.Position = HeaderExt.HeaderSize;
            using (var reader = new BinaryReader(stream, Encoding.UTF8, leaveOpen: true))
            {
                for (var i = 0; i < partHeader.CommittedFileCount; i++)
                    indexEntries.Add(reader.ReadIndexEntry());
            }

            var expectedOffset = (long)HeaderExt.HeaderZoneSize;
            foreach (var entry in indexEntries)
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

            if (partHeader.PartType == PartTypeEnum.Hot)
            {
                if (partHeader.WritePosition != expectedOffset ||
                    partHeader.WritePosition < HeaderExt.HeaderZoneSize ||
                    partHeader.WritePosition > stream.Length)
                {
                    throw new InvalidDataException($"Некорректная позиция записи в разделе {partPath}: {partHeader.WritePosition}");
                }

                var writer = new BinaryWriter(stream, Encoding.UTF8, leaveOpen: false);
                stream.Position = partHeader.WritePosition;
                return (partHeader, indexEntries, writer);
            }

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

    public static PartStorage Create(string partPath)
    {
        var (partHeader, indexEntries, writer) = LoadPart(partPath);
        return new PartStorage(partPath, partHeader, indexEntries, writer);
    }

    public static PartStorage Create(string rootPath, int partNumber, int partSizeMb)
    {
        if (!Directory.Exists(rootPath))
            Directory.CreateDirectory(rootPath);
        var partPath = Path.Combine(rootPath, $"{partNumber:0000000000}.lss");
        var stream = new FileStream(partPath, FileMode.CreateNew, FileAccess.ReadWrite, FileShare.ReadWrite);
        stream.SetLength(partSizeMb * 1024L * 1024L);
        var writer = new BinaryWriter(stream, Encoding.UTF8, leaveOpen: false);
        var now = DateTimeOffset.UtcNow;
        var partHeader = writer.CreatePartHeader(new PartHeader(
            partNumber,
            HeaderExt.HeaderZoneSize,
            PartTypeEnum.Hot,
            now,
            now,
            now,
            0));
        stream.Position = HeaderExt.HeaderZoneSize;
        return new PartStorage(partPath, partHeader, [], writer);
    }
}
