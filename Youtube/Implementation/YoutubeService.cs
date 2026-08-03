using YoutubeExplode;
using YoutubeExplode.Converter;
using YoutubeExplode.Videos.Streams;

namespace VideoDownloader.Youtube.Implementation;

public class YoutubeService(YoutubeClient yt, ConversionRequestBuilder crb)
{
    public async Task DownloadVideoAsync(string videoUrl, string qualidade = "1080p60")
    {
        var streamManifest = await yt.Videos.Streams.GetManifestAsync(videoUrl);
        var audioStreamInfo = streamManifest
            .GetAudioStreams()
            .Where(s => s.Container == Container.Mp4)
            .GetWithHighestBitrate();

        var videoStreamInfo = streamManifest
            .GetVideoStreams()
            .Where(s => s.Container == Container.Mp4)
            .First(QualidadeDoVideo(qualidade));

        await yt.Videos.DownloadAsync([audioStreamInfo, videoStreamInfo], crb.Build());
    }
    public async Task DownloadPlaylistAsync(string playlistUrl)
    {
        await foreach (var batch in yt.Playlists.GetVideoBatchesAsync(playlistUrl))
        {
            foreach (var video in batch.Items)
            {
                await yt.Videos.DownloadAsync(video.Id, crb.Build());
            }
        }
    }
    private static Func<IVideoStreamInfo, bool> QualidadeDoVideo(string qualidade)
    {
        return qualidade switch
        {
            "2160p60" => s => s.VideoQuality.Label == "2160p60",
            "2160p" => s => s.VideoQuality.Label == "2160p",
            "1440p60" => s => s.VideoQuality.Label == "1440p60",
            "1440p" => s => s.VideoQuality.Label == "1440p",
            "1080p60" => s => s.VideoQuality.Label == "1080p60",
            "1080p" => s => s.VideoQuality.Label == "1080p",
            "720p60" => s => s.VideoQuality.Label == "720p60",
            "720p" => s => s.VideoQuality.Label == "720p",
            "480p" => s => s.VideoQuality.Label == "480p",
            "360p" => s => s.VideoQuality.Label == "360p",
            "240p" => s => s.VideoQuality.Label == "240p",
            "144p" => s => s.VideoQuality.Label == "144p",
            _ => throw new ArgumentException("Qualidade de vídeo inválida."),
        };
    }
}

