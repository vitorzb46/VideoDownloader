using Microsoft.Extensions.Localization;
using System.Diagnostics.CodeAnalysis;
using VideoDownloader.Youtube.Implementation;

namespace VideoDownloader;

[SuppressMessage("Performance", "CA1812:Avoid uninstantiated internal classes")]
internal sealed class DownloadApplication(YoutubeService ys, IStringLocalizer<DownloadApplication> localizer)
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
            switch (args[0].ToLowerInvariant())
            {
                case "video":
                    await BaixarVideo(args).ConfigureAwait(false);
                    break;
                case "audio":
                    await BaixarAudio(args).ConfigureAwait(false);
                    break;
                case "playlist":
                    await BaixarPlaylist(args).ConfigureAwait(false);
                    break;
                case "help" or "-h" or "--help":
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
        }
    }

    private async Task BaixarVideo(string[] args)
    {
        if (args.Length < 2)
        {
            Console.WriteLine(localizer["Console_UsoVideo"]);
            return;
        }

        if (!EhUrlValida(args[1]))
        {
            Console.WriteLine(localizer["Console_UrlInvalida", args[1]]);
            return;
        }

        string qualidade = args.Length >= 3 ? args[2] : "1080p";
        await ys.DownloadVideoAsync(args[1], qualidade).ConfigureAwait(false);
    }

    private async Task BaixarAudio(string[] args)
    {
        if (args.Length < 2)
        {
            Console.WriteLine(localizer["Console_UsoAudio"]);
            return;
        }

        if (!EhUrlValida(args[1]))
        {
            Console.WriteLine(localizer["Console_UrlInvalida", args[1]]);
            return;
        }

        await ys.DownloadAudioAsync(args[1]).ConfigureAwait(false);
    }

    private async Task BaixarPlaylist(string[] args)
    {
        if (args.Length < 2)
        {
            Console.WriteLine(localizer["Console_UsoPlaylist"]);
            return;
        }

        if (!EhUrlValida(args[1]))
        {
            Console.WriteLine(localizer["Console_UrlInvalida", args[1]]);
            return;
        }

        await ys.DownloadPlaylistAsync(args[1]).ConfigureAwait(false);
    }

    private static bool EhUrlValida(string url)
    {
        return Uri.TryCreate(url, UriKind.Absolute, out Uri? uri) &&
               (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
    }
}
