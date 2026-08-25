using System.ComponentModel.DataAnnotations;
using MinimalApi.Hosting.Options;

namespace Storage.Client;

public class StorageClientOptions : IOptionsBase
{
    public static string SectionName => "ClientOptions";
    
    [Required]
    public string BaseUri { get; set; } = "http://localhost:8096";
}