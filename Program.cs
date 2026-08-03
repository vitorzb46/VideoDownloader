using Microsoft.Extensions.DependencyInjection;
using VideoDownloader;
using VideoDownloader.Youtube.Implementation;
using YoutubeExplode;
using YoutubeExplode.Converter;

class Program
{
    static async Task Main()
    {
        var serviceProvider = ConfigureServices();

        var app = serviceProvider.GetRequiredService<DownloadApplication>();

        await app.Executar();
    }

    private static ServiceProvider ConfigureServices()
    {
        var services = new ServiceCollection();

        services.AddTransient<DownloadApplication>();        
        services.AddSingleton<YoutubeClient>();
        services.AddSingleton<ConversionRequestBuilder>();
        services.AddScoped<YoutubeService>();
        // Configure your services here
        return services.BuildServiceProvider();
    }
}
