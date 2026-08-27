using Microsoft.Extensions.Options;
using Storage.Api.DataAccess;
using Storage.Api.Lss;
using Storage.Api.Options;

namespace Storage.Api.Services;

internal class PolicyService(
    ILogger<PolicyService> logger,
    IOptions<StorageOptions> options,
    IServiceScopeFactory scopeFactory,
    INodeStorage nodeStorage) : BackgroundService
{
    private readonly StorageOptions _options = options.Value;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ApplyPolicyAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error applying policy: {Message}", ex.Message);
            }
            finally
            {
                await Task.Delay(_options.PolicyInterval, stoppingToken);
            }
        }
    }

    private async Task ApplyPolicyAsync(CancellationToken stoppingToken)
    {
        // Физически удаляется после истечения срока хранения в холодном хранилище.
        // TODO: Реализовать перенос из горячего в холодное хранилище.
        using var scope = scopeFactory.CreateScope();
        var clusterDataAccess = scope.ServiceProvider.GetRequiredService<IClusterDataAccess>();
        var buckets = await clusterDataAccess.GetBucketsAsync(stoppingToken);
        var policyMap = buckets.ToDictionary(x => x.BucketId, x => new RetentionPolicy(x.TtlCold));
        var map = nodeStorage.ApplyRetentionPolicy(x => policyMap[x]);
        foreach (var (bucket, parts) in map)
        foreach (var part in parts)
            await clusterDataAccess.DeleteFilesAsync(_options.NodeId, bucket, part);
    }
}