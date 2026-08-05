using YoutubeDLSharp;
using YoutubeDLSharp.Options;

internal sealed class YoutubeDLService(YoutubeDL ytDL)
{
    private static async Task ObterDependenciasAsync(string filePath)
    {
        if (File.Exists(Path.Combine(filePath, "yt-dlp.exe")) && File.Exists(Path.Combine(filePath, "ffmpeg.exe")))
        {
            return;
        }
        await Utils.DownloadYtDlp().ConfigureAwait(false);
        await Utils.DownloadFFmpeg().ConfigureAwait(false);
    }
    public async Task GetVideoAsync(string videoUrl)
    {        
        var app = Directory.GetCurrentDirectory();
        Directory.CreateDirectory("Resources");

        ytDL.YoutubeDLPath = Path.Combine(app, "yt-dlp.exe"); 
        ytDL.FFmpegPath = Path.Combine(app, "ffmpeg.exe");

        await ObterDependenciasAsync(app).ConfigureAwait(false);
        var cookies = Path.Combine(app, "Resources", "www.youtube.com_cookies.txt") ?? null;

        // browsers suportados
        // chrome firefox edge safari opera brave vivaldi chromium whale waterfox librewolf palemoon cyberfox basilisk icecat seamonkey sleipnir
        string nomeArquivo = "%(title)s.%(ext)s";
        string videoAudioQualidade = "bestvideo+bestaudio/best";
        string userAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/122.0.0.0 Safari/537.36";
        string browser = "firefox";
        var options = new OptionSet()
        {
            Paths = app,
            Output = nomeArquivo,
            NoOverwrites = true,
            Format = videoAudioQualidade,
            MergeOutputFormat = DownloadMergeFormat.Mp4, // possiveis formatos: mp4, mkv, webm, flv, ogg
            //LimitRate = "10M"
            RestrictFilenames = true,
            AddHeaders = userAgent,
            Cookies = cookies,
            CookiesFromBrowser = browser,
        };
        await ytDL.RunVideoDownload(videoUrl, overrideOptions: options).ConfigureAwait(false);
    }
}