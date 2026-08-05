using Microsoft.Extensions.DependencyInjection;
using System.Net;
using VideoDownloader;
using VideoDownloader.Youtube.Implementation;
using YoutubeExplode;

[assembly: System.Resources.NeutralResourcesLanguage("pt-BR")]
internal sealed class Program
{
    private static async Task Main(string[] args)
    {
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
        services.AddScoped<YoutubeExplodeService>();
        services.AddScoped<YoutubeDLService>();

        return services.BuildServiceProvider();
    }
    private static List<Cookie> ObterCookiesAutenticados()
    {
        var listaCookies = new List<Cookie>();

        foreach (var linha in File.ReadAllLines(Path.Combine("Resources", "www.youtube.com_cookies.txt")))
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
