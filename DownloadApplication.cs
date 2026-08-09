using Microsoft.Extensions.Localization;
using MonoTorrent;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using VideoDownloader.Services.Implementation;
using VideoDownloader.Youtube.Implementation;

namespace VideoDownloader;

[SuppressMessage("Performance", "CA1812:Avoid uninstantiated internal classes")]
internal sealed class DownloadApplication(YoutubeExplodeService ys, YoutubeDLService ydl, TorrentDownloadService torrent, IStringLocalizer<DownloadApplication> localizer)
{
    public async Task Executar(string[] args)
    {
        
        
        args = ["torrent", "magnet:?xt=urn:btih:3EAE86E3ECFE8BC7E3151F56285D47FA9E60514F&dn=Silo+S03E06+1080p+HEVC+x265-MeGusta&tr=udp%3A%2F%2Fopen.stealth.si%3A80%2Fannounce&tr=udp%3A%2F%2Ftracker.opentrackr.org%3A1337%2Fannounce&tr=udp%3A%2F%2Fexodus.desync.com%3A6969%2Fannounce&tr=udp%3A%2F%2Ftracker.torrent.eu.org%3A451%2Fannounce&tr=udp%3A%2F%2Fopen.demonii.com%3A1337%2Fannounce&tr=udp%3A%2F%2Ftracker.dler.org%3A6969%2Fannounce&tr=udp%3A%2F%2Fexplodie.org%3A6969%2Fannounce&tr=udp%3A%2F%2Ftracker.ololosh.space%3A6969%2Fannounce&tr=udp%3A%2F%2Ftracker.dump.cl%3A6969%2Fannounce&tr=udp%3A%2F%2Ftracker.bittor.pw%3A1337%2Fannounce&tr=udp%3A%2F%2Ftracker-udp.gbitt.info%3A80%2Fannounce&tr=udp%3A%2F%2Fretracker01-msk-virt.corbina.net%3A80%2Fannounce&tr=udp%3A%2F%2Fopen.free-tracker.ga%3A6969%2Fannounce&tr=udp%3A%2F%2Fns-1.x-fins.com%3A6969%2Fannounce&tr=udp%3A%2F%2Fleet-tracker.moe%3A1337%2Fannounce&tr=udp%3A%2F%2Fp4p.arenabg.com%3A1337%2Fannounce&tr=udp%3A%2F%2Ftracker.leechers-paradise.org%3A6969%2Fannounce&tr=udp%3A%2F%2Ftracker.open-internet.nl%3A6969%2Fannounce&tr=udp%3A%2F%2Ftracker.pirateparty.gr%3A6969%2Fannounce&tr=udp%3A%2F%2Fdenis.stalker.upeer.me%3A6969%2Fannounce"];
        

        if (args.Length == 0)
        {
            Console.WriteLine(localizer["Console_Uso"]);
            return;
        }

        try
        {
            switch (args[0].ToUpperInvariant())
            {
                case "VIDEO":
                    await BaixarVideo(args).ConfigureAwait(false);
                    break;
                case "AUDIO":
                    await BaixarAudio(args).ConfigureAwait(false);
                    break;
                case "PLAYLIST":
                    await BaixarPlaylist(args).ConfigureAwait(false);
                    break;
                case "MOSTRAR":
                    await MostrarPlaylist(args).ConfigureAwait(false);
                    break;
                case "TORRENT":
                    await BaixarTorrent(args).ConfigureAwait(false);
                    break;
                case "HELP" or "-H" or "--HELP":
                    Console.WriteLine(localizer["Console_Uso"]);
                    break;
                default:
                    Console.WriteLine(localizer["Console_ComandoInvalido", args[0]]);
                    Console.WriteLine(localizer["Console_Uso"]);
                    break;
            }
        }
        catch (Exception ex)
        {
            // Erros de download ou de rede chegam aqui; exibe mensagem amigável.
            Console.WriteLine(localizer["Console_Erro", ex.Message]);
            throw;
        }
    }

    private async Task BaixarVideo(string[] args)
    {
        string? url = ObterUrl(args, "Console_UsoVideo");
        if (url is null)
        {
            return;
        }
        await ydl.VideoDLAsync(url).ConfigureAwait(false);
        return;
    }

    private async Task BaixarAudio(string[] args)
    {
        string? url = ObterUrl(args, "Console_UsoAudio");
        if (url is null)
        {
            return;
        }

        //await ys.DownloadAudioAsync(url).ConfigureAwait(false);
    }

    private async Task BaixarPlaylist(string[] args)
    {
        string? url = ObterUrl(args, "Console_UsoPlaylist");
        if (url is null)
        {
            return;
        }

        await ydl.VideoDLAsync(url).ConfigureAwait(false);
    }

    private async Task MostrarPlaylist(string[] args)
    {
        string? url = ObterUrl(args, "Console_UsoMostrar");
        if (url is null)
        {
            return;
        }

        //await ys.MostrarPlaylistAsync(url).ConfigureAwait(false);
    }

    private async Task BaixarTorrent(string[] args)
    {
        if (args.Length < 2)
        {
            Console.WriteLine(localizer["Console_UsoTorrent"]);
            return;
        }

        string input = args[1];
        Guid? id = null;

        if (MagnetLink.TryParse(input, out var magnet))
        {
            Console.WriteLine(localizer["Torrent_Iniciado"]);

            id = await torrent.BaixarAsync(magnet).ConfigureAwait(false);

            Console.WriteLine(localizer["Torrent_Concluido", id]);
        }
        else
        {
            try
            {
                Console.WriteLine(localizer["Torrent_Iniciado"]);

                id = await torrent.BaixarAsync(input).ConfigureAwait(false);

                Console.WriteLine(localizer["Torrent_Concluido", id]);
            }
            catch
            {
                Console.WriteLine(localizer["Console_TorrentInvalido", input]);
                return;
            }
        }
    }

    private string? ObterUrl(string[] args, string chaveUso)
    {
        if (args.Length < 2)
        {
            Console.WriteLine(localizer[chaveUso]);
            return null;
        }

        if (!EhUrlValida(args[1]))
        {
            Console.WriteLine(localizer["Console_UrlInvalida", args[1]]);
            return null;
        }

        return args[1];
    }

    private static bool EhUrlValida(string url)
    {
        return Uri.TryCreate(url, UriKind.Absolute, out Uri? uri) &&
               (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
    }
}
