using VideoDownloader.Youtube.Implementation;

namespace VideoDownloader;

public class DownloadApplication(YoutubeService ys)
{
    public async Task Executar()
    {
        await ys.DownloadVideoAsync("https://www.youtube.com/watch?v=dQw4w9WgXcQ");
    }
}
