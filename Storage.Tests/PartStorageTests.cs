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
    public void GenericTest()
    {
        var rootPath = $"{RootPath}\\test1";
        var offsetList = new List<(long Len, string Hash)>();

        string partPath;
        using (var ps0 = new PartStorage(rootPath, 0, 100_000_000))
        {
            partPath = ps0.PartPath;
            for (var i = 0; i < 10; i++)
            {
                var size = Random.Shared.NextInt64(500_000, 5_000_000);
                var wData = new byte[size];
                Random.Shared.NextBytes(wData);
                var wHash = Convert.ToBase64String(SHA256.HashData(wData));
                if (!ps0.TryWrite("test_file.tmp", wData, out var offset))
                    break;
                offsetList.Add((offset, wHash));
            }
        }

        using (var ps1 = new PartStorage(partPath, true))
        {
            while (true)
            {
                var size = Random.Shared.NextInt64(500_000, 5_000_000);
                var wData = new byte[size];
                Random.Shared.NextBytes(wData);
                var wHash = Convert.ToBase64String(SHA256.HashData(wData));
                if (!ps1.TryWrite("test_file.tmp", wData, out var offset))
                    break;
                offsetList.Add((offset, wHash));
            }
        }

        using (var ps2 = new PartStorage(partPath, true))
        {
            foreach (var (offset, wHash) in offsetList)
            {
                var (_, rData, _) = ps2.Read(offset);
                var rHash = Convert.ToBase64String(SHA256.HashData(rData));
                Assert.That(rHash, Is.EqualTo(wHash));
            }
        }
    }

    [Test]
    public void ReadOnlyTest()
    {
        var rootPath = $"{RootPath}\\test2";

        string partPath;
        using (var ps0 = new PartStorage(rootPath, 1, 100_000_000))
        {
            partPath = ps0.PartPath;
            while (true)
            {
                var size = Random.Shared.NextInt64(500_000, 5_000_000);
                var wData = new byte[size];
                Random.Shared.NextBytes(wData);
                if (!ps0.TryWrite("test_file.tmp", wData, out _))
                    break;
            }
        }

        using (var ps1 = new PartStorage(partPath, true))
        {
            var size = Random.Shared.NextInt64(500_000, 5_000_000);
            var wData = new byte[size];
            Random.Shared.NextBytes(wData);
            using (Assert.EnterMultipleScope())
            {
                Assert.That(ps1.TryWrite("test_file.tmp", wData, out var offset), Is.False);
                Assert.That(offset, Is.EqualTo(-1));
            }
        }
    }
}