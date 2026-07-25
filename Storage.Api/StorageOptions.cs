using MinimalApi.Hosting.Options;

namespace Storage.Api;

internal sealed class StorageOptions : IOptionsBase
{
    public static string SectionName => "StorageOptions";
}