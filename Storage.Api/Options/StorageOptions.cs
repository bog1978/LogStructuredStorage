using System.ComponentModel.DataAnnotations;

namespace Storage.Api.Options;

public class StorageOptions : IOptionsBase
{
    public static string SectionName => nameof(StorageOptions);
    
    [Required(AllowEmptyStrings = false)]
    public string RootPath { get; set; } = string.Empty;

    [Required, Range(1024 * 1024 * 100, 1024 * 1024 * 1024)]
    public int PartSize { get; set; } = 1024 * 1024 * 100;
    
    [Required(AllowEmptyStrings = false)] 
    public string ConnectionString { get; set; } = string.Empty;
    
    [Required(AllowEmptyStrings = false), MinLength(5)]
    public string NodeId { get; set; } = string.Empty;
    
    [Required]
    public TimeSpan PolicyInterval { get; set; } = TimeSpan.FromSeconds(60);
    
    [Range(1, 256)]
    public int BodySizeLimitMb { get; set; }
}