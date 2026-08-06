using System.Diagnostics.CodeAnalysis;
using YoutubeExplode;

namespace VideoDownloader.Youtube.Implementation;

[SuppressMessage("Performance", "CA1812:Avoid uninstantiated internal classes")]
internal sealed class YoutubeExplodeService(YoutubeClient yt)
{
    // Como a classe YoutubeDLService implementa a mesma funcionalidade, a classe YoutubeExplodeService foi descontinuada
    //public async Task DownloadAudioAsync(string videoUrl, IProgress<double>? progress = null)
    //{
    //    using var progressBar = progress is null ? new ConsoleDownloadProgressBar("YoutubeExplode") : null;
    //    var progressReporter = progress ?? progressBar!;

    //    var video = await yt.Videos.GetAsync(videoUrl).ConfigureAwait(false);
    //    IAudioStreamInfo audioStreamInfo = await AudioStream(videoUrl).ConfigureAwait(false);
    //    string extensao = audioStreamInfo.Container == Container.Mp4 ? "m4a" : audioStreamInfo.Container.Name;
    //    var caminho = SaidaDoArquivo(video.Title, extensao);
    //    await yt.Videos.Streams.DownloadAsync(audioStreamInfo, caminho, progressReporter).ConfigureAwait(false);
    //}
    //public async Task DownloadVideoAsync(string videoUrl, string qualidade = "1080p", IProgress<double>? progress = null)
    //{
    //    using var progressBar = progress is null ? new ConsoleDownloadProgressBar("YoutubeExplode") : null;
    //    var progressReporter = progress ?? progressBar!;

    //    await VerificarFFmpeg().ConfigureAwait(false);
    //    var video = await yt.Videos.GetAsync(videoUrl).ConfigureAwait(false);
    //    var request = Builder(video.Title);

    //    IAudioStreamInfo audioStreamInfo = await AudioStream(videoUrl).ConfigureAwait(false);
    //    IVideoStreamInfo videoStreamInfo = await VideoStream(videoUrl, qualidade).ConfigureAwait(false);
    //    await yt.Videos.DownloadAsync([audioStreamInfo, videoStreamInfo], request, progressReporter).ConfigureAwait(false);

    //    Console.WriteLine($"Qualidade do vídeo: {qualidade}\n");
    //}
    //public async Task DownloadPlaylistAsync(string playlistUrl, IProgress<double>? progress = null)
    //{
    //    await VerificarFFmpeg().ConfigureAwait(false);
    //    await foreach (var batch in yt.Playlists.GetVideoBatchesAsync(playlistUrl).ConfigureAwait(false))
    //    {
    //        foreach (var video in batch.Items)
    //        {
    //            await yt.Videos.DownloadAsync(video.Id, Builder(video.Title), progress).ConfigureAwait(false);
    //        }
    //    }
    //}
    //public async Task MostrarPlaylistAsync(string playlistUrl)
    //{
    //    var playlist = await yt.Playlists.GetAsync(playlistUrl).ConfigureAwait(false);
    //    Console.WriteLine($"Playlist: {playlist.Title}");
    //    Console.WriteLine($"Autor: {playlist.Author?.ChannelTitle ?? "Desconhecido"}");
    //    var contar = 0;
    //    await foreach (var video in yt.Playlists.GetVideosAsync(playlistUrl).ConfigureAwait(false))
    //    {
    //        Console.WriteLine(video.Id);
    //        contar++;
    //    }
    //    Console.WriteLine($"{contar} vídeos encontrados!\n");
    //}
    //private static readonly HashSet<string> QualidadesValidas =
    //[
    //    "2160p60", "2160p", "1440p60", "1440p", "1080p60", "1080p",
    //    "720p60", "720p", "480p", "360p", "240p", "144p",
    //];

    //private static Func<IVideoStreamInfo, bool> QualidadeDoVideo(string qualidade)
    //{
    //    return !QualidadesValidas.Contains(qualidade)
    //        ? throw new ArgumentException("Qualidade de vídeo inválida.")
    //        : (s => s.VideoQuality.Label == qualidade);
    //}
    //private async Task<StreamManifest> Manifest(string videoUrl)
    //{
    //    return await yt.Videos.Streams.GetManifestAsync(videoUrl).ConfigureAwait(false);
    //}
    //private async Task<IVideoStreamInfo> VideoStream(string videoUrl, string qualidade)
    //{
    //    StreamManifest streamManifest = await Manifest(videoUrl).ConfigureAwait(false);

    //    // Prefere H.264 (avc1) — codec reproduzível no player padrão do Windows.
    //    var videoStreamInfo = streamManifest
    //        .GetVideoStreams()
    //        .Where(s => s.Container == Container.Mp4)
    //        .Where(s => s.VideoCodec.StartsWith("avc1", StringComparison.OrdinalIgnoreCase))
    //        .FirstOrDefault(QualidadeDoVideo(qualidade))
    //        ?? streamManifest
    //            .GetVideoStreams()
    //            .Where(s => s.Container == Container.Mp4)
    //            .First(QualidadeDoVideo(qualidade));
    //    return videoStreamInfo;
    //}
    //private async Task<IAudioStreamInfo> AudioStream(string videoUrl)
    //{
    //    StreamManifest streamManifest = await Manifest(videoUrl).ConfigureAwait(false);

    //    // Prefere AAC (mp4a) — codec reproduzível no player padrão do Windows.
    //    var audioMp4 = streamManifest
    //        .GetAudioStreams()
    //        .Where(s => s.Container == Container.Mp4)
    //        .ToList();
    //    var audioAac = audioMp4
    //        .Where(s => s.AudioCodec.StartsWith("mp4a", StringComparison.OrdinalIgnoreCase))
    //        .ToList();

    //    return (IAudioStreamInfo)(audioAac.Count > 0 ? audioAac.GetWithHighestBitrate() : audioMp4.GetWithHighestBitrate());
    //}
    //private static async Task VerificarFFmpeg()
    //{
    //    bool ffmpeg = File.Exists(GetFFmpeg());
    //    if (!ffmpeg)
    //    {
    //        // Baixa para AppContext.BaseDirectory, onde o YoutubeExplode.Converter procura o ffmpeg.
    //        await FFmpegDownloader.GetLatestVersion(FFmpegVersion.Official, AppContext.BaseDirectory).ConfigureAwait(false);
    //    }
    //}
    //private static string GetFFmpeg()
    //{
    //    return Directory.EnumerateFiles(AppContext.BaseDirectory).FirstOrDefault(f => string.Equals(Path.GetFileName(f), "ffmpeg.exe", StringComparison.OrdinalIgnoreCase)) ?? "ffmpeg.exe";
    //}
    //private static string SaidaDoArquivo(string nomeArquivo, string extensao)
    //{
    //    string caminhoExecutavel = Environment.CurrentDirectory;
    //    string caminho = Path.Combine(caminhoExecutavel, $"{TituloLimpo(nomeArquivo)}.{extensao}");
    //    return caminho;
    //}
    //private static ConversionRequest Builder(string nomeArquivo)
    //{
    //    var container = Container.Mp4;
    //    var extensao = container.Name;
    //    var preset = ConversionPreset.UltraFast;
    //    var arquivo = SaidaDoArquivo(nomeArquivo, extensao);
    //    return new ConversionRequestBuilder(arquivo)
    //                        .SetContainer(container)
    //                        .SetPreset(preset)
    //                        .Build();
    //}
    //private static string TituloLimpo(string titulo)
    //{
    //    foreach (char c in Path.GetInvalidFileNameChars())
    //    {
    //        titulo = titulo.Replace(c, '_');
    //    }
    //    return titulo;
    //}
}

