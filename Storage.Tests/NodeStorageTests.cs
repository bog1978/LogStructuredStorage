using System.Security.Cryptography;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Storage.Node;

namespace Storage.Tests;

public sealed class NodeStorageTests : IDisposable
{
    private readonly INodeStorage _bucketStorage;

    private readonly string[] _bucketNames =
    [
        "test_bucket_1",
        "test_bucket_2",
        "test_bucket_3",
        "test_bucket_4"
    ];
    
    public NodeStorageTests()
    {
        var builder = Host.CreateApplicationBuilder();
        var services = builder.Services;
        services.AddNodeStorage(builder.Configuration);
        var host = builder.Build();
        _bucketStorage = host.Services.GetRequiredService<INodeStorage>();
    }

    [OneTimeSetUp]
    public void Setup() => 
        _bucketStorage.DeleteAll();

    [OneTimeTearDown]
    public void Cleanup() => 
        _bucketStorage.DeleteAll();

    [Test, Order(0)]
    public void CreateBucketsTest()
    {
        foreach (var bucketName in _bucketNames)
            _bucketStorage.GetOrCreateBucket(bucketName);
    }

    [Test, Order(1)]
    public void GenericTest()
    {
        var locationList = new List<(DataLocation Location, string Hash)>();

        for (var i = 0; i < 500; i++)
        {
            var bucketName = _bucketNames[Random.Shared.Next(_bucketNames.Length)];
            var size = Random.Shared.NextInt64(500_000, 5_000_000);
            var wData = new byte[size];
            Random.Shared.NextBytes(wData);
            var wHash = Convert.ToBase64String(SHA256.HashData(wData));
            var location = _bucketStorage.Write(bucketName, wData);
            locationList.Add((location, wHash));
        }

        foreach (var (location, wHash) in locationList)
        {
            var rData = _bucketStorage.Read(location);
            var rHash = Convert.ToBase64String(SHA256.HashData(rData));
            Assert.That(rHash, Is.EqualTo(wHash));
        }
    }

    public void Dispose() => 
        _bucketStorage.Dispose();
}