namespace VideoDownloader.Constantes;

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
    public bool TorrentSemear;
    public bool TorrentStreaming;
    public long? TorrentLimiteDownload;
    public long? TorrentLimiteUpload;
    public string[] TorrentTrackers = [];
}
