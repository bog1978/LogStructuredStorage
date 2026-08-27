using System.Security.Cryptography;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Storage.Cluster;

namespace Storage.Tests;

public sealed class NodeStorageTests : IDisposable
{
    private readonly IHost _host;

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
        _host = builder.Build();
    }

    [OneTimeSetUp]
    public void Setup()
    {
        var bucketStorage = _host.Services.GetRequiredService<INodeStorage>();
        bucketStorage.DeleteAll();
    }

    [Test, Order(0)]
    public void CreateBucketsTest()
    {
        var bucketStorage = _host.Services.GetRequiredService<INodeStorage>();
        foreach (var bucketName in _bucketNames)
            bucketStorage.GetOrCreateBucket(bucketName);
    }

    [Test, Order(1)]
    public void GenericTest()
    {
        var bucketStorage = _host.Services.GetRequiredService<INodeStorage>();
        var locationList = new List<(DataLocation Location, string Hash)>();

        for (var i = 0; i < 500; i++)
        {
            var bucketName = _bucketNames[Random.Shared.Next(_bucketNames.Length)];
            var size = Random.Shared.NextInt64(500_000, 5_000_000);
            var wData = new byte[size];
            Random.Shared.NextBytes(wData);
            var wHash = Convert.ToBase64String(SHA256.HashData(wData));
            var location = bucketStorage.Write(bucketName, wData);
            locationList.Add((location, wHash));
        }

        foreach (var (location, wHash) in locationList)
        {
            var rData = bucketStorage.Read(location);
            var rHash = Convert.ToBase64String(SHA256.HashData(rData));
            Assert.That(rHash, Is.EqualTo(wHash));
        }
    }

    [Test, Order(2)]
    public void PolicyTest()
    {
        var policy = new RetentionPolicy(TimeSpan.Zero);
        var bucketStorage = _host.Services.GetRequiredService<INodeStorage>();
        bucketStorage.ApplyRetentionPolicy(_ => policy);
    }

    public void Dispose() =>
        _host.Dispose();
}