# Arquitetura do Projeto 
 
## Estrutura de Pastas 
``` 
- Constantes 
- docs 
- Progress 
- Resources 
- Services 
  - Implementation 
``` 
 
## Código Fonte (Apenas Assinaturas) 
### Arquivo: DownloadApplication.cs 
```csharp 
namespace VideoDownloader;
internal sealed class DownloadApplication(YoutubeExplodeService ys, YoutubeDLService ydl, IStringLocalizer<DownloadApplication> localizer)
    public async Task Executar(string[] args)
    private async Task BaixarVideo(string[] args)
    private async Task BaixarAudio(string[] args)
    private async Task BaixarPlaylist(string[] args)
    private async Task MostrarPlaylist(string[] args)
    private string? ObterUrl(string[] args, string chaveUso)
    private static bool EhUrlValida(string url)
``` 
 
### Arquivo: Program.cs 
```csharp 
internal sealed class Program
    private static async Task Main(string[] args)
    private static ServiceProvider ConfigureServices()
    private static List<Cookie> ObterCookiesAutenticados()
``` 
 
### Arquivo: AppSettings.cs 
```csharp 
namespace VideoDownloader.Constantes;
internal sealed class AppSettings
    public string? NomeArquivo = "%(title)s.%(ext)s";
    public string? VideoAudioQualidade = "bestvideo+bestaudio/best";
    public string? UserAgent = string.Empty;
    public string? PastaRecursos = "Resources";
    public string? YtDlpExe = "yt-dlp.exe";
    public string? FfmpegExe = "ffmpeg.exe";
    public string? Cookies = string.Empty;
``` 
 
### Arquivo: ConsoleDownloadProgressBar.cs 
```csharp 
namespace VideoDownloader.Progress;
internal sealed class ConsoleDownloadProgressBar : IProgress<double>, IDisposable
    private readonly string _title;
    private readonly int _width;
    private readonly object _sync = new();
    private int _lastPercent = -1;
    private bool _disposed;
    public ConsoleDownloadProgressBar(string title = "Download", int width = 40)
    public void Report(double value)
    private void Render(int percentage)
    public void Dispose()
internal sealed class YoutubeDlProgressBridge : IProgress<DownloadProgress>
    private readonly IProgress<double> _progress;
    public YoutubeDlProgressBridge(IProgress<double> progress)
    public void Report(DownloadProgress value)
``` 
 
### Arquivo: DownloadApplication.Designer.cs 
```csharp 
namespace VideoDownloader.Resources {
    public class DownloadApplication {
        private static global::System.Resources.ResourceManager resourceMan;
        private static global::System.Globalization.CultureInfo resourceCulture;
        internal DownloadApplication() {
        public static global::System.Resources.ResourceManager ResourceManager {
        public static global::System.Globalization.CultureInfo Culture {
        public static string Baixando_Video {
        public static string Console_ComandoInvalido {
        public static string Console_Erro {
        public static string Console_UrlInvalida {
        public static string Console_Uso {
        public static string Console_UsoAudio {
        public static string Console_UsoMostrar {
        public static string Console_UsoPlaylist {
        public static string Console_UsoVideo {
``` 
 
### Arquivo: TorrentDownloadService.cs 
```csharp 
internal sealed class TorrentDownloadService
``` 
 
### Arquivo: YoutubeDLService.cs 
```csharp 
namespace VideoDownloader.Services.Implementation;
internal sealed partial class YoutubeDLService(AppSettings appContext)
    private static partial Regex PercentRegex();
    public async Task VideoDLAsync(string videoUrl, IProgress<double>? progress = null)
    private List<string> CriarArgumentosYtDlp(string app, string ffmpegPath, string pastaRecursos, string outputTemplate)
    private static async Task ExecutarYtDlpAsync(string ytDlpPath, string workingDirectory, IEnumerable<string> arguments, IProgress<double>? progress = null)
    private static void ReportarProgressoYtDlp(string line, IProgress<double>? progress)
    private static async Task DownloadArquivoDiretoAsync(string videoUrl, string app, IProgress<double>? progress)
    private async Task ObterDependenciasAsync(string basePath)
    private static string ResolvePath(string basePath, string? fileName)
    private static async Task<bool> MidiaDiretaAsync(string url)
    private static bool ConteudoMidia(HttpResponseMessage response)
    private static bool SiteComExtractor(string url)
    private static string ObterNomeArquivo(HttpResponseMessage response, Uri uri)
``` 
 
### Arquivo: YoutubeExplodeService.cs 
```csharp 
namespace VideoDownloader.Youtube.Implementation;
internal sealed class YoutubeExplodeService(YoutubeClient yt)
``` 
 
## Análise de Dependências (Graphify) 
# Graph Report - C:\Users\Vitor\Documents\Repos\Projects\VideoDownloader  (2026-08-06)

## Corpus Check
- cluster-only mode — file stats not available

## Summary
- 99 nodes · 121 edges · 24 communities (7 shown, 17 thin omitted)
- Extraction: 100% EXTRACTED · 0% INFERRED · 0% AMBIGUOUS
- Token cost: 0 input · 0 output

## Graph Freshness
- Built from commit: `61b779ea`
- Run `git rev-parse HEAD` and compare to check if the graph is stale.
- Run `graphify update .` after code changes (no API cost).

## Community Hubs (Navigation)
- YoutubeDLService
- ConsoleDownloadProgressBar
- Program.cs
- VideoDownloader.csproj
- DownloadApplication
- Program
- DownloadApplication
- Container
- ConversionRequest
- Cookie
- Func
- IAudioStreamInfo
- IReadOnlyList
- IVideoStreamInfo
- Cookie
- HashSet
- IProgress
- List
- HashSet
- IProgress
- Task
- StreamManifest
- Task

## God Nodes (most connected - your core abstractions)
1. `YoutubeDLService` - 13 edges
2. `ConsoleDownloadProgressBar` - 10 edges
3. `DownloadApplication` - 8 edges
4. `Program` - 4 edges
5. `YoutubeDlProgressBridge` - 4 edges
6. `VideoDownloader.Services.Implementation` - 4 edges
7. `DownloadApplication` - 3 edges
8. `VideoDownloader.Youtube.Implementation` - 3 edges
9. `VideoDownloader.Constantes` - 2 edges
10. `AppSettings` - 2 edges

## Surprising Connections (you probably didn't know these)
- `ConsoleDownloadProgressBar` --implements--> `IProgress`  [EXTRACTED]
  Progress/ConsoleDownloadProgressBar.cs →   _Bridges community 1 → community 0_

## Import Cycles
- None detected.

## Communities (24 total, 17 thin omitted)

### Community 0 - "YoutubeDLService"
Cohesion: 0.19
Nodes (9): GeneratedRegex, HttpResponseMessage, IEnumerable, IProgress, List, Regex, Task, YoutubeDLService (+1 more)

### Community 1 - "ConsoleDownloadProgressBar"
Cohesion: 0.18
Nodes (9): bool, VideoDownloader.Progress, DownloadProgress, IDisposable, int, object, string, ConsoleDownloadProgressBar (+1 more)

### Community 2 - "Program.cs"
Cohesion: 0.16
Nodes (8): string, AppSettings, VideoDownloader, VideoDownloader.Services.Implementation, VideoDownloader.Constantes, VideoDownloader.Youtube.Implementation, TorrentDownloadService, YoutubeExplodeService

### Community 3 - "VideoDownloader.csproj"
Cohesion: 0.18
Nodes (10): net10.0, Microsoft.Extensions.DependencyInjection (11.0.0-preview.6.26359.118), Microsoft.Extensions.Localization (11.0.0-preview.6.26359.118), Microsoft.Extensions.Logging (10.0.10), MonoTorrent (3.0.2), Xabe.FFmpeg.Downloader (6.0.2), YoutubeDLSharp (1.2.0), YoutubeExplode (6.6.1-a.1) (+2 more)

### Community 5 - "Program"
Cohesion: 0.32
Nodes (5): Cookie, List, Task, Program, ServiceProvider

### Community 6 - "DownloadApplication"
Cohesion: 0.40
Nodes (4): VideoDownloader.Resources, CultureInfo, ResourceManager, DownloadApplication

## Knowledge Gaps
- **14 isolated node(s):** `VideoDownloader.Progress`, `VideoDownloader.Resources`, `YoutubeExplodeService`, `net10.0`, `Microsoft.Extensions.DependencyInjection (11.0.0-preview.6.26359.118)` (+9 more)
  These have ≤1 connection - possible missing edges or undocumented components.
- **17 thin communities (<3 nodes) omitted from report** — run `graphify query` to explore isolated nodes.

## Suggested Questions
_Questions this graph is uniquely positioned to answer:_

- **Why does `YoutubeDLService` connect `YoutubeDLService` to `Program.cs`?**
  _High betweenness centrality (0.246) - this node is a cross-community bridge._
- **Why does `ConsoleDownloadProgressBar` connect `ConsoleDownloadProgressBar` to `YoutubeDLService`?**
  _High betweenness centrality (0.112) - this node is a cross-community bridge._
- **What connects `VideoDownloader.Progress`, `VideoDownloader.Resources`, `YoutubeExplodeService` to the rest of the system?**
  _14 weakly-connected nodes found - possible documentation gaps or missing edges._ 
