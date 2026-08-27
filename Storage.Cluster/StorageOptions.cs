using System.ComponentModel.DataAnnotations;
using Storage.Cluster.Options;

namespace Storage.Cluster;

public class StorageOptions : IOptionsBase
{
    public static string SectionName => nameof(StorageOptions);
    
    [Required(AllowEmptyStrings = false)]
    public string RootPath { get; set; } = string.Empty;

    [Required, Range(1024 * 1024 * 100, 1024 * 1024 * 1024)]
    public int PartSize { get; set; } = 1024 * 1024 * 100;
}