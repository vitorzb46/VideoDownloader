using MonoTorrent;
using VideoDownloader.Constantes;

namespace VideoDownloader.Services.Implementation;

internal sealed class TorrentDownloadService(AppSettings appContext)
{
    public async Task<Guid> BaixarAsync(MagnetLink magnet)
    {
        await Task.CompletedTask.ConfigureAwait(false);
        return Guid.NewGuid();
    }
}
