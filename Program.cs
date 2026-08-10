using Microsoft.Extensions.DependencyInjection;
using MonoTorrent.Client;
using System.Net;
using System.Net.Sockets;
using System.Text;
using VideoDownloader;
using VideoDownloader.Constantes;
using YoutubeDLSharp;
using YoutubeExplode;

[assembly: System.Resources.NeutralResourcesLanguage("pt-BR")]
internal sealed partial class Program
{
    private static async Task Main(string[] args)
    {
        Console.OutputEncoding = Encoding.UTF8;

        // System.Globalization.CultureInfo.CurrentUICulture = new System.Globalization.CultureInfo("en-US");

        var serviceProvider = ConfigureServices();

        var app = serviceProvider.GetRequiredService<DownloadApplication>();

        await app.Executar(args).ConfigureAwait(false);
        Console.ReadKey(true);
    }

    private static ServiceProvider ConfigureServices()
    {
        var services = new ServiceCollection();
        int portaLivre = ObterPortaLivre();

        services.AddLogging();
        services.AddLocalization(options => options.ResourcesPath = "Resources");

        services.AddTransient<DownloadApplication>();
        services.AddSingleton<YoutubeClient>(provider => new YoutubeClient(ObterCookiesAutenticados()));
        services.AddSingleton<YoutubeDL>();
        services.AddSingleton<ClientEngine>(sp =>
        {
            var settingBuilder = new EngineSettingsBuilder
            {
                // --- REDE E CONEXÕES ---
                AllowPortForwarding = true,
                AllowLocalPeerDiscovery = true,
                DhtEndPoint = new IPEndPoint(IPAddress.Any, 0), // Porta UDP dinâmica para DHT
                ListenEndPoints = new Dictionary<string, IPEndPoint>
                {
                    { "ipv4", new IPEndPoint(IPAddress.Any, 0) }, // Porta TCP/UDP dinâmica para Peers
                    { "ipv6", new IPEndPoint(IPAddress.IPv6Any, 0) }
                },
                MaximumConnections = 200,

                // --- CACHE E ARQUIVOS ---
                AutoSaveLoadFastResume = true,
                AutoSaveLoadMagnetLinkMetadata = true,
                AutoSaveLoadDhtCache = true,
                UsePartialFiles = false, // Desativado para ajudar o VLC a ler o arquivo direto
                DiskCacheBytes = 50 * 1024 * 1024, // 50MB de RAM dedicada a cache

                // --- STREAMING E WEBSEEDS ---
                HttpStreamingPrefix = $"http://127.0.0.1:{portaLivre}/torrent-stream/",
                WebSeedConnectionTimeout = TimeSpan.FromSeconds(15),
                WebSeedSpeedTrigger = 0 // Baixa de fontes HTTP e P2P simultaneamente se disponível

            };

            EngineSettings settings = settingBuilder.ToSettings();
            return new ClientEngine(settings);
        });
        services.AddScoped<VideoDownloader.Youtube.Implementation.YoutubeExplodeService>();
        services.AddScoped<VideoDownloader.Services.Implementation.YoutubeDLService>();
        services.AddScoped<VideoDownloader.Services.Implementation.TorrentDownloadService>();

        services.AddSingleton<AppSettings>();

        return services.BuildServiceProvider();
    }
    private static int ObterPortaLivre()
    {
        using var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        socket.Bind(new IPEndPoint(IPAddress.Loopback, 0)); // '0' força o Windows a dar uma porta vazia
        return ((IPEndPoint)socket.LocalEndPoint!).Port;
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
