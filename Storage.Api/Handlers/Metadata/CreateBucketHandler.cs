using JetBrains.Annotations;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Storage.Api.DataAccess;
using Storage.Api.Dto;
using Storage.Api.Internal;
using Storage.Api.Validation;

namespace Storage.Api.Handlers.Metadata;

[UsedImplicitly]
internal sealed class CreateBucketHandler : IEndpointHandler
{
    public static IEndpointConventionBuilder[] ConfigureEndpoint(IEndpointRouteBuilder builder) =>
    [
        builder
            .MapPost("/bucket/", GetBucketsAsync)
            .WithName("CreateBucket")
            .WithTags("Metadata")
    ];

    /// <summary>Создание новой корзины.</summary>
    private static async Task<Created<BucketDto>> GetBucketsAsync(
        [FromBody] BucketCreateDto createDto,
        [FromServices] ILogger<GetBucketsHandler> logger,
        [FromServices] IClusterDataAccess clusterDataAccess,
        CancellationToken token)
    {
        var normalizedBucketName = BucketNameValidator.NormalizeAndValidate(createDto.BucketId);

        var newBucket = await clusterDataAccess.CreateBucketAsync(
            normalizedBucketName,
            createDto.NodeId,
            createDto.TtlHot,
            createDto.TtlCold,
            token);
        return TypedResults.Created($"/bucket/{newBucket.BucketId}", newBucket.ToDto());
    }
}
