using System.ComponentModel.DataAnnotations;
using Storage.Cluster.Options;

namespace Storage.Api;

internal sealed class ApiOptions : IOptionsBase
{
    public static string SectionName => nameof(ApiOptions);

    [Range(1, 256)]
    public int BodySizeLimitMb { get; set; }
}