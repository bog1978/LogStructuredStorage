using Microsoft.Extensions.Logging;
using Refit;
using Storage.Client;

internal class Worker(ILogger<Worker> logger, IStorageApi client)
{
    public async Task DoWork()
    {
        try
        {
            var nodes = await client.GetNodesAsync();
            if (nodes.Count == 0)
            {
                Console.WriteLine("No nodes found");
                return;
            }

            var bucketCreateDto = new BucketCreateDto
            {
                NodeId = nodes.First().NodeId,
                BucketId = "test-bucket",
                TtlHot = "00:01:00",
                TtlCold = "01:00:00"
            };

            var bucket = await client.CreateBucketAsync(bucketCreateDto);

            for (var i = 0; i < 100; i++)
            {
                var fileName = $"file_{i}.dat";
                var filePath = Path.Combine("test_files", fileName);
                var size = Random.Shared.Next(1024 * 1024 / 4, 1024 * 1024 * 4);
                var data = new byte[size];
                Random.Shared.NextBytes(data);
                using var stream = new MemoryStream(data);
                var file = await client.UploadFileAsync("test-bucket", filePath, new StreamPart(stream, fileName));
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error uploading file: {Message}", ex.Message);
        }
    }
}