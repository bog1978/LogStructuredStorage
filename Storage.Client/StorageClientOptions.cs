using System.ComponentModel.DataAnnotations;

namespace Storage.Client;

public class StorageClientOptions
{
    public static string SectionName => "ClientOptions";
    
    [Required]
    public string BaseUri { get; set; } = "http://localhost:8096";
}