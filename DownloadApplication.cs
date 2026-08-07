using Microsoft.Extensions.Localization;
using MonoTorrent;
using System.Diagnostics.CodeAnalysis;
using VideoDownloader.Services.Implementation;
using VideoDownloader.Youtube.Implementation;

namespace VideoDownloader;

[SuppressMessage("Performance", "CA1812:Avoid uninstantiated internal classes")]
internal sealed class DownloadApplication(YoutubeExplodeService ys, YoutubeDLService ydl, TorrentDownloadService torrent, IStringLocalizer<DownloadApplication> localizer)
{
    public async Task Executar(string[] args)
    {
#if DEBUG
        args = ["torrent", "magnet:?xt=urn:btih:212488687F9CBDFD74CEDBA7A43EEB91FE82C271&dn=%5Bbitsearch.to%5D%20Silo.S03E04.1080p.HEVC.x265-MeGusta%5BEZTVx.to%5D.mkv&tr=DHT&tr=udp%3A%2F%2Fbittorrent-tracker.e-n-c-r-y-p-t.net%3A1337%2Fannounce&tr=udp%3A%2F%2Fevan.im%3A6969%2Fannounce&tr=udp%3A%2F%2Fexodus.desync.com%3A6969%2Fannounce&tr=udp%3A%2F%2Fexplodie.org%3A6969%2Fannounce&tr=udp%3A%2F%2Ftracker.bitsearch.to%3A1337%2Fannounce"];
#endif
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

        if (!MagnetLink.TryParse(args[1], out var magnet))
        {
            Console.WriteLine(localizer["Console_MagnetInvalido", args[1]]);
            return;
        }
        else
        {
            Console.WriteLine(localizer["Torrent_Iniciado"]);
            var id = await torrent.BaixarAsync(magnet).ConfigureAwait(false);
            Console.WriteLine(localizer["Torrent_Concluido", id]);
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
