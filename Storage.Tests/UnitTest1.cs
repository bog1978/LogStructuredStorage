using System.Security.Cryptography;
using Storage.Api.Services;

namespace Storage.Tests;

public class PartStorageTests
{
    private const string RootPath = @"E:\\Share\lss";
    
    [SetUp]
    public void Setup()
    {
        if (Directory.Exists(RootPath))
            Directory.Delete(RootPath, true);
        Directory.CreateDirectory(RootPath);
    }

    [Test]
    public void GenericTest()
    {
        var offsetList = new List<(long Len, string Hash)>();

        var ps = new PartStorage(RootPath, 0, 100_000_000);
        while (true)
        {
            var size = Random.Shared.NextInt64(500_000, 5_000_000);
            var wData = new byte[size];
            Random.Shared.NextBytes(wData);
            var wHash = Convert.ToBase64String(SHA256.HashData(wData));
            if (!ps.TryWrite(wData, out var offset))
                break;
            offsetList.Add((offset, wHash));
        }

        var ps2 = new PartStorage(ps.PartPath);
        foreach (var (offset, wHash) in offsetList)
        {
            var rData = ps2.Read(offset);
            var rHash = Convert.ToBase64String(SHA256.HashData(rData));
            Assert.That(rHash, Is.EqualTo(wHash));
        }
    }
}