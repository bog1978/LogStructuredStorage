using Microsoft.Extensions.Options;
using Storage.Api.DataAccess;
using Storage.Api.Options;

namespace Storage.Api.Services;

internal sealed class NodeInitializer(
    IClusterDataAccess clusterDataAccess,
    IOptions<StorageOptions> options)
{
    private readonly StorageOptions _options = options.Value;

    public async Task InitializeAsync(CancellationToken token)
    {
        await clusterDataAccess.RegisterNodeAsync(_options.NodeName, "localhost", token);
    }
}