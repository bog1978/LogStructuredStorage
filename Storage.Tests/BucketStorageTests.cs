using System.Security.Cryptography;
using Storage.Node;

namespace Storage.Tests;

public class BucketStorageTests
{
    private const string RootPath = @"D:\\Share\lss\bucket";
    private const string BucketName = "test_bucket";

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
        var locationList = new List<(DataLocation Location, string Hash)>();

        using (var ps0 = new BucketStorage(RootPath, BucketName, 100 * 1024 * 1024))
        {
            for (var i = 0; i < 100; i++)
            {
                var size = Random.Shared.NextInt64(500_000, 5_000_000);
                var wData = new byte[size];
                Random.Shared.NextBytes(wData);
                var wHash = Convert.ToBase64String(SHA256.HashData(wData));
                var location = ps0.Write(wData);
                locationList.Add((location, wHash));
            }
        }

        using (var ps2 = new BucketStorage(RootPath, BucketName, 100_000_000))
        {
            foreach (var (location, wHash) in locationList)
            {
                var rData = ps2.Read(location);
                var rHash = Convert.ToBase64String(SHA256.HashData(rData));
                Assert.That(rHash, Is.EqualTo(wHash));
            }
        }
    }
}