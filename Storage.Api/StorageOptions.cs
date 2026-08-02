using System.ComponentModel.DataAnnotations;
using MinimalApi.Hosting.Options;

namespace Storage.Api;

internal sealed class StorageOptions : IOptionsBase
{
    public static string SectionName => "StorageOptions";

    [Required(AllowEmptyStrings = false)] 
    public string ClusterConnectionString { get; set; } = string.Empty;
    
    [Required(AllowEmptyStrings = false)]
    public string NodeConnectionString { get; set; } = string.Empty;

    [Range(1, 256)]
    public int BodySizeLimitMb { get; set; }

    [Required(AllowEmptyStrings = false), MinLength(5)]
    public string NodeName { get; set; } = string.Empty;
    
    [Required(AllowEmptyStrings = false)]
    public string BucketRootPath { get;set; } = string.Empty;

    [Range(128, 1024)]
    public int BucketPartSizeMb { get; set; }
}