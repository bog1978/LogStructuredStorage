using System.Security.Cryptography;
using Storage.Node;

namespace Storage.Tests;

public class PartStorageTests
{
    private const string RootPath = @"E:\\Share\lss\part";

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
                if (!ps0.TryWrite(wData, out var offset))
                    break;
                offsetList.Add((offset, wHash));
            }
        }

        using (var ps1 = new PartStorage(partPath))
        {
            while (true)
            {
                var size = Random.Shared.NextInt64(500_000, 5_000_000);
                var wData = new byte[size];
                Random.Shared.NextBytes(wData);
                var wHash = Convert.ToBase64String(SHA256.HashData(wData));
                if (!ps1.TryWrite(wData, out var offset))
                    break;
                offsetList.Add((offset, wHash));
            }
        }

        using (var ps2 = new PartStorage(partPath))
        {
            foreach (var (offset, wHash) in offsetList)
            {
                var rData = ps2.Read(offset);
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
                if (!ps0.TryWrite(wData, out _))
                    break;
            }
        }

        using (var ps1 = new PartStorage(partPath))
        {
            var size = Random.Shared.NextInt64(500_000, 5_000_000);
            var wData = new byte[size];
            Random.Shared.NextBytes(wData);
            Assert.Throws<InvalidOperationException>(() => ps1.TryWrite(wData, out _));
        }
    }
}