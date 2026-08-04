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
                case "mostrar":
                    await MostrarPlaylist(args).ConfigureAwait(false);
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
        string? url = ObterUrl(args, "Console_UsoVideo");
        if (url is null)
        {
            return;
        }

        string qualidade = args.Length >= 3 ? args[2] : "1080p";
        await ys.DownloadVideoAsync(url, qualidade).ConfigureAwait(false);
    }

    private async Task BaixarAudio(string[] args)
    {
        string? url = ObterUrl(args, "Console_UsoAudio");
        if (url is null)
        {
            return;
        }

        await ys.DownloadAudioAsync(url).ConfigureAwait(false);
    }

    private async Task BaixarPlaylist(string[] args)
    {
        string? url = ObterUrl(args, "Console_UsoPlaylist");
        if (url is null)
        {
            return;
        }

        await ys.DownloadPlaylistAsync(url).ConfigureAwait(false);
    }

    private async Task MostrarPlaylist(string[] args)
    {
        string? url = ObterUrl(args, "Console_UsoMostrar");
        if (url is null)
        {
            return;
        }

        await ys.MostrarPlaylistAsync(url).ConfigureAwait(false);
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
