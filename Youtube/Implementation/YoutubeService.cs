using YoutubeExplode;
using YoutubeExplode.Converter;
using YoutubeExplode.Videos.Streams;

namespace VideoDownloader.Youtube.Implementation;

public class YoutubeService(YoutubeClient yt, ConversionRequestBuilder crb)
{
    public async Task DownloadVideoAsync(string videoUrl)
    {
        var streamManifest = await yt.Videos.Streams.GetManifestAsync(videoUrl);
        var audioStreamInfo = streamManifest
            .GetAudioStreams()
            .Where(s => s.Container == Container.Mp4)
            .GetWithHighestBitrate();

        var videoStreamInfo = streamManifest
            .GetVideoStreams()
            .Where(s => s.Container == Container.Mp4)
            .First(s => s.VideoQuality.Label == "1080p60");

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
}

