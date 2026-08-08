using Microsoft.Extensions.DependencyInjection;
using MonoTorrent.Client;
using System.Net;
using System.Text;
using VideoDownloader;
using VideoDownloader.Constantes;
using YoutubeDLSharp;
using YoutubeExplode;

[assembly: System.Resources.NeutralResourcesLanguage("pt-BR")]
internal sealed class Program
{
    private static async Task Main(string[] args)
    {
        Console.OutputEncoding = Encoding.UTF8;

        // System.Globalization.CultureInfo.CurrentUICulture = new System.Globalization.CultureInfo("en-US");
        
        var serviceProvider = ConfigureServices();

        var app = serviceProvider.GetRequiredService<DownloadApplication>();

        await app.Executar(args).ConfigureAwait(false);
    }

    private static ServiceProvider ConfigureServices()
    {
        var services = new ServiceCollection();

        services.AddLogging();
        services.AddLocalization(options => options.ResourcesPath = "Resources");

        services.AddTransient<DownloadApplication>();
        services.AddSingleton<YoutubeClient>(provider => new YoutubeClient(ObterCookiesAutenticados()));
        services.AddSingleton<YoutubeDL>();
        services.AddSingleton<TorrentManager>();
        services.AddSingleton<ClientEngine>();
        services.AddScoped<VideoDownloader.Youtube.Implementation.YoutubeExplodeService>();
        services.AddScoped<VideoDownloader.Services.Implementation.YoutubeDLService>();
        services.AddScoped<VideoDownloader.Services.Implementation.TorrentDownloadService>();

        services.AddSingleton<AppSettings>();

        return services.BuildServiceProvider();
    }
    private static List<Cookie> ObterCookiesAutenticados()
    {
        var listaCookies = new List<Cookie>();
        var app = Directory.GetCurrentDirectory();
        Directory.CreateDirectory(Path.Combine(app, "Resources"));

        string path = Path.Combine(app, "Resources", "www.youtube.com_cookies.txt");
        if (!File.Exists(path))
        {
            File.Create(path).Dispose();
        }
        foreach (var linha in File.ReadAllLines(path))
        {
            if (string.IsNullOrWhiteSpace(linha) || linha.StartsWith('#'))
                continue;

            var partes = linha.Split('\t');
            if (partes.Length >= 7)
            {
                var dominio = partes[0];
                var nome = partes[5];
                var valor = partes[6];

                listaCookies.Add(new Cookie(nome, valor, "/", dominio));
            }
        }

        return listaCookies;
    }
}
