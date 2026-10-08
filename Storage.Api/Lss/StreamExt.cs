namespace Storage.Api.Lss;

internal static class StreamExt
{
    extension(Stream source)
    {
        public async Task CopyExactlyAsync(Stream destination, long bytesToCopy, CancellationToken token)
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
    }
}
