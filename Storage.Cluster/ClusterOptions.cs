using System.ComponentModel.DataAnnotations;
using MinimalApi.Hosting.Options;

namespace Storage.Cluster;

public sealed class ClusterOptions : IOptionsBase
{
    public static string SectionName => nameof(ClusterOptions);

    [Required(AllowEmptyStrings = false)] 
    public string ConnectionString { get; set; } = string.Empty;
    
    [Required(AllowEmptyStrings = false), MinLength(5)]
    public string NodeId { get; set; } = string.Empty;
    
    [Required]
    public TimeSpan PolicyInterval { get; set; } = TimeSpan.FromSeconds(60);
}