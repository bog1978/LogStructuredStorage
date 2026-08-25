using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Storage.Client;

internal class Program
{
    public static async Task Main(string[] args)
    {
        var builder = Host.CreateApplicationBuilder();

        builder.Services
                .AddStorageClient(builder.Configuration)
                .AddSingleton<Worker>();

        var host = builder.Build();

        try
        {
            var worker = host.Services.GetRequiredService<Worker>();
            await worker.DoWork();
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
        }
    }
}