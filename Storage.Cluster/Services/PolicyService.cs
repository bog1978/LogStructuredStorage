using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Storage.Cluster.DataAccess;

namespace Storage.Cluster.Services;

internal class PolicyService(
    ILogger<PolicyService> logger,
    IOptions<ClusterOptions> options,
    IServiceScopeFactory scopeFactory,
    INodeStorage nodeStorage) : BackgroundService
{
    private readonly ClusterOptions _options = options.Value;
    
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
        using var scope = scopeFactory.CreateScope();
        var clusterDataAccess = scope.ServiceProvider.GetRequiredService<IClusterDataAccess>();
        var buckets = await clusterDataAccess.GetBucketsAsync(stoppingToken);
        var policyMap = buckets.ToDictionary(x => x.BucketId, x => new RetentionPolicy(x.Ttl));
        nodeStorage.ApplyRetentionPolicy(x => policyMap[x]);
    }
}