using System.Security.Cryptography;
using Storage.Api.Lss;
using Storage.Cluster;

namespace Storage.Tests;

public class BucketStorageTests
{
    private const string HotPath = @"D:\\Share\lss\hot\bucket";
    private const string ColdPath = @"D:\\Share\lss\cold\bucket";
    private const string BucketName = "test_bucket";

    [OneTimeSetUp]
    public void Setup()
    {
        if (Directory.Exists(HotPath))
            Directory.Delete(HotPath, true);
        Directory.CreateDirectory(HotPath);
        if (Directory.Exists(ColdPath))
            Directory.Delete(ColdPath, true);
        Directory.CreateDirectory(ColdPath);
    }

    [OneTimeTearDown]
    public void Cleanup()
    {
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        if (Directory.Exists(HotPath))
            Directory.Delete(HotPath, true);
        if (Directory.Exists(ColdPath))
            Directory.Delete(ColdPath, true);
    }

    [Test]
    public void GenericTest()
    {
        var locationList = new List<(DataLocation Location, string Hash)>();

        using (var ps0 = new BucketStorage(HotPath, ColdPath, BucketName, 100))
        {
            for (var i = 0; i < 100; i++)
            {
                var size = Random.Shared.Next(500_000, 5_000_000);
                var wData = new byte[size];
                Random.Shared.NextBytes(wData);
                var wHash = Convert.ToBase64String(SHA256.HashData(wData));
                var fileHeader = new FileHeader("test_file.tmp", "", size, DateTimeOffset.UtcNow);
                var location = ps0.Write(fileHeader, wData);
                locationList.Add((location, wHash));
            }
        }

        using (var ps2 = new BucketStorage(HotPath, ColdPath, BucketName, 100))
        {
            foreach (var (location, wHash) in locationList)
            {
                var (_, rData) = ps2.Read(location);
                var rHash = Convert.ToBase64String(SHA256.HashData(rData));
                Assert.That(rHash, Is.EqualTo(wHash));
            }
        }
    }
}