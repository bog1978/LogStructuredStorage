using System.ComponentModel.DataAnnotations;

namespace Storage.Api.Options;

internal class StorageOptions : IOptionsBase
{
    public static string SectionName => nameof(StorageOptions);
    
    [Required(AllowEmptyStrings = false)]
    public string HotPath { get; set; } = string.Empty;

    [Required(AllowEmptyStrings = false)]
    public string ColdPath { get; set; } = string.Empty;

    [Required, Range(100, 1024)]
    public int PartSizeMb { get; set; } = 100;
    
    [Required(AllowEmptyStrings = false)] 
    public string ConnectionString { get; set; } = string.Empty;
    
    [Required(AllowEmptyStrings = false), MinLength(5)]
    public string NodeId { get; set; } = string.Empty;
    
    [Required]
    public TimeSpan PolicyInterval { get; set; } = TimeSpan.FromSeconds(60);
    
    [Range(1, 256)]
    public int BodySizeLimitMb { get; set; }
}