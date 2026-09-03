using System.Security.Cryptography;
using Storage.Api.Lss;

namespace Storage.Tests;

public class PartStorageTests
{
    private const string RootPath = @"D:\\Share\lss\part";

    [OneTimeSetUp]
    public void Setup()
    {
        if (Directory.Exists(RootPath))
            Directory.Delete(RootPath, true);
        Directory.CreateDirectory(RootPath);
    }

    [OneTimeTearDown]
    public void Cleanup()
    {
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        if (Directory.Exists(RootPath))
            Directory.Delete(RootPath, true);
    }

    [Test]
    public async Task GenericTest()
    {
        var rootPath = $"{RootPath}\\test1";
        var offsetList = new List<(long Len, string Hash)>();

        string partPath;
        using (var ps0 = new PartStorage(rootPath, 0, 100_000_000))
        {
            partPath = ps0.PartPath;
            for (var i = 0; i < 10; i++)
            {
                var size = Random.Shared.Next(500_000, 5_000_000);
                var wData = new byte[size];
                Random.Shared.NextBytes(wData);
                var wHash = Convert.ToBase64String(SHA256.HashData(wData));
                var fileHeader = new FileHeader("test_file.tmp", "", size, DateTimeOffset.UtcNow);
                using var ms = new MemoryStream(wData);
                var offset = await ps0.TryWrite(fileHeader, ms, CancellationToken.None);
                if (offset < 0)
                    break;
                offsetList.Add((offset, wHash));
            }
        }

        using (var ps1 = new PartStorage(partPath, true))
        {
            while (true)
            {
                var size = Random.Shared.Next(500_000, 5_000_000);
                var wData = new byte[size];
                Random.Shared.NextBytes(wData);
                var wHash = Convert.ToBase64String(SHA256.HashData(wData));
                var fileHeader = new FileHeader("test_file.tmp", "", size, DateTimeOffset.UtcNow);
                using var ms = new MemoryStream(wData);
                var offset = await ps1.TryWrite(fileHeader, ms, CancellationToken.None);
                if (offset < 0)
                    break;
                offsetList.Add((offset, wHash));
            }
        }

        using (var ps2 = new PartStorage(partPath, true))
        {
            foreach (var (offset, wHash) in offsetList)
            {
                using var ms = new MemoryStream();
                await ps2.Read(offset, ms, _ => { }, CancellationToken.None);
                var rData = ms.ToArray();
                var rHash = Convert.ToBase64String(SHA256.HashData(rData));
                Assert.That(rHash, Is.EqualTo(wHash));
            }
        }
    }

    [Test]
    public async Task ReadOnlyTest()
    {
        var rootPath = $"{RootPath}\\test2";

        string partPath;
        using (var ps0 = new PartStorage(rootPath, 1, 100_000_000))
        {
            partPath = ps0.PartPath;
            while (true)
            {
                var size = Random.Shared.Next(500_000, 5_000_000);
                var wData = new byte[size];
                Random.Shared.NextBytes(wData);
                var fileHeader = new FileHeader("test_file.tmp", "", size, DateTimeOffset.UtcNow);
                using var ms = new MemoryStream(wData);
                var offset = await ps0.TryWrite(fileHeader, ms, CancellationToken.None);
                if (offset < 0)
                    break;
            }
        }

        using (var ps1 = new PartStorage(partPath, true))
        {
            var size = Random.Shared.Next(500_000, 5_000_000);
            var wData = new byte[size];
            Random.Shared.NextBytes(wData);
            using (Assert.EnterMultipleScope())
            {
                var fileHeader = new FileHeader("test_file.tmp", "", size, DateTimeOffset.UtcNow);
                using var ms = new MemoryStream(wData);
                var offset = await ps1.TryWrite(fileHeader, ms, CancellationToken.None);
                Assert.That(offset, Is.EqualTo(-1));
            }
        }
    }
}