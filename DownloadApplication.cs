using Microsoft.Extensions.Localization;
using System.Diagnostics.CodeAnalysis;
using VideoDownloader.Services.Implementation;
using VideoDownloader.Youtube.Implementation;

namespace VideoDownloader;

[SuppressMessage("Performance", "CA1812:Avoid uninstantiated internal classes")]
internal sealed class DownloadApplication(YoutubeExplodeService ys, YoutubeDLService ydl, IStringLocalizer<DownloadApplication> localizer)
{
    public async Task Executar(string[] args)
    {
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
