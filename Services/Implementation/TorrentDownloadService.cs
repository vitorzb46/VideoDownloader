using Microsoft.Extensions.Localization;
using MonoTorrent;
using MonoTorrent.Client;
using VideoDownloader.Constantes;

namespace VideoDownloader.Services.Implementation;
internal sealed class TorrentDownloadService
{
    private readonly AppSettings AppContext;
    ClientEngine Engine { get; }
    public TorrentDownloadService(ClientEngine engine, AppSettings appContext)
    {
        AppContext = appContext;
        Engine = engine;
    }

    public async Task<Guid> BaixarAsync(string magnet, CancellationToken? token = default)
    {
        var id = Guid.NewGuid();
        // Implementação simples - Teste url magnética
        Directory.CreateDirectory(AppContext.PastaDownloads!);
        var pastaDownload = Path.Combine(Environment.CurrentDirectory, AppContext.PastaDownloads!);
        try
        {
            await Engine.AddAsync(magnet, pastaDownload).ConfigureAwait(false);
            while (Engine.IsRunning)
            {
                Console.Write("*");
            }
        }catch (Exception ex)
        {
            Console.WriteLine($"Erro ao baixar torrent: {ex.Message}");
            throw;
        }

        return id;
    }
}
