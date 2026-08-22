using System.ComponentModel.DataAnnotations;
using MinimalApi.Hosting.Options;

namespace Storage.Api;

internal sealed class ApiOptions : IOptionsBase
{
    public static string SectionName => nameof(ApiOptions);

    [Required(AllowEmptyStrings = false)] 
    public string ClusterConnectionString { get; set; } = string.Empty;
    
    [Range(1, 256)]
    public int BodySizeLimitMb { get; set; }

    [Required(AllowEmptyStrings = false), MinLength(5)]
    public string NodeId { get; set; } = string.Empty;
}