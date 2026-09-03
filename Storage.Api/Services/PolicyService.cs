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

    protected override async Task ExecuteAsync(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            try
            {
                await ApplyPolicyAsync(token);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error applying policy: {Message}", ex.Message);
            }
            finally
            {
                await Task.Delay(_options.PolicyInterval, token);
            }
        }
    }

    private async Task ApplyPolicyAsync(CancellationToken token)
    {
        // Физически удаляется после истечения срока хранения в холодном хранилище.
        // TODO: Реализовать перенос из горячего в холодное хранилище.
        using var scope = scopeFactory.CreateScope();
        var clusterDataAccess = scope.ServiceProvider.GetRequiredService<IClusterDataAccess>();
        var buckets = await clusterDataAccess.GetBucketsAsync(token);
        var policyMap = buckets.ToDictionary(x => x.BucketName, x => new RetentionPolicy(x.TtlHot, x.TtlCold));
        await nodeStorage.ApplyRetentionPolicy(x => policyMap[x], token);
    }
}