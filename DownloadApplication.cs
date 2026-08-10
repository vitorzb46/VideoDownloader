using Microsoft.Extensions.Localization;
using MonoTorrent;
using System.Diagnostics.CodeAnalysis;
using VideoDownloader.Services.Implementation;
using VideoDownloader.Youtube.Implementation;

namespace VideoDownloader;

[SuppressMessage("Performance", "CA1812:Avoid uninstantiated internal classes")]
internal sealed class DownloadApplication(YoutubeExplodeService ys, YoutubeDLService ydl, TorrentDownloadService torrent, IStringLocalizer<DownloadApplication> localizer)
{
    // ys reservado para quando os métodos de áudio/playlist do YoutubeExplodeService forem reativados.
#pragma warning disable CS9113, CA1823
    private readonly YoutubeExplodeService _ys = ys;
#pragma warning restore CS9113, CA1823

    public async Task Executar(string[] args)
    {


        args = ["torrent", "torrents"];


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
                case "STREAM":
                    await StreamarTorrent(args).ConfigureAwait(false);
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

    [SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "O fallback de pasta não pode interromper o fluxo quando a entrada não é um magnet válido.")]
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

    [SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Falhas de streaming são reportadas como mensagem amigável ao usuário.")]
    private async Task StreamarTorrent(string[] args)
    {
        if (args.Length < 2)
        {
            Console.WriteLine(localizer["Console_UsoStream"]);
            return;
        }

        if (!MagnetLink.TryParse(args[2], out var magnet))
        {
            Console.WriteLine(localizer["Console_MagnetInvalido", args[1]]);
            return;
        }

        try
        {
            Console.WriteLine(localizer["Stream_Iniciado"]);
            using var stream = await torrent.StreamAsync(magnet).ConfigureAwait(false);
            Console.WriteLine(localizer["Stream_Pronto", stream.ToString()!]);
            Console.ReadKey(true);
        }
        catch (Exception ex)
        {
            Console.WriteLine(localizer["Stream_Erro", ex.Message]);
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
