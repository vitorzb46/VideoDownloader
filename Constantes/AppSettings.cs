using System.Diagnostics.CodeAnalysis;

namespace VideoDownloader.Constantes;

[SuppressMessage("Performance", "CA1812:Avoid uninstantiated internal classes")]
internal sealed class AppSettings
{
    public string? NomeArquivo = "%(title)s.%(ext)s";
    public string? VideoAudioQualidade = "bestvideo+bestaudio/best";
    public string? UserAgent = string.Empty;
    public string? PastaRecursos = "Resources";
    public string? YtDlpExe = "yt-dlp.exe";
    public string? FfmpegExe = "ffmpeg.exe";
    public string? Cookies = string.Empty;

    // Torrent (MonoTorrent)
    public string? PastaDownloads = "Downloads";
    public string? PastaTorrents = "Torrents";
    public string? PastaCache = "Cache";
    public int TorrentPorta = 51413;
    public int ConnectionsMaxima = 150;
    public int UploadSlotsMaximo = 4;
    // Limites em bytes/s; 0 = ilimitado. Campos mantêm o default (0) até serem configurados.
#pragma warning disable CS0649
    public int TorrentLimiteDownload;
    public int TorrentLimiteUpload;
#pragma warning restore CS0649
    public bool TorrentSemear = true;
    public bool TorrentStreaming = true;
    public string[] TorrentTrackers = [];
}
