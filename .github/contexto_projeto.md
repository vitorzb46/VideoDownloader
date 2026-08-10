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
internal sealed class DownloadApplication(YoutubeExplodeService ys, YoutubeDLService ydl, TorrentDownloadService torrent, IStringLocalizer<DownloadApplication> localizer)
    private readonly YoutubeExplodeService _ys = ys;
    public async Task Executar(string[] args)
    private async Task BaixarVideo(string[] args)
    private async Task BaixarAudio(string[] args)
    private async Task BaixarPlaylist(string[] args)
    private async Task MostrarPlaylist(string[] args)
    private async Task BaixarTorrent(string[] args)
    private async Task StreamarTorrent(string[] args)
    private string? ObterUrl(string[] args, string chaveUso)
    private static bool EhUrlValida(string url)
``` 
 
### Arquivo: Program.cs 
```csharp 
internal sealed partial class Program
    private static async Task Main(string[] args)
    private static ServiceProvider ConfigureServices()
    private static int ObterPortaLivre()
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
    public string? PastaDownloads = "Downloads";
    public string? PastaTorrents = "Torrents";
    public string? PastaCache = "Cache";
    public int TorrentPorta = 51413;
    public int ConnectionsMaxima = 150;
    public int UploadSlotsMaximo = 4;
    public int TorrentLimiteDownload;
    public int TorrentLimiteUpload;
    public bool TorrentSemear = true;
    public bool TorrentStreaming = true;
    public string[] TorrentTrackers = [];
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
    public ConsoleDownloadProgressBar(string title = "Download", int width = 30)
    public void Report(double value)
    public void Report(double value, StringBuilder? sb)
    private void Render(int percentage, StringBuilder? sb)
    public void Dispose()
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
 
### Arquivo: TorrentProgress.cs 
```csharp 
namespace VideoDownloader.Services;
internal enum TorrentEstado
internal sealed record TorrentProgress(
``` 
 
### Arquivo: TorrentDownloadService.cs 
```csharp 
namespace VideoDownloader.Services.Implementation;
internal sealed class TorrentDownloadService(ClientEngine engine, AppSettings appContext, IStringLocalizer<TorrentDownloadService> localizer)
    private readonly AppSettings AppContext = appContext;
    private ClientEngine Engine { get; set; } = engine;
    private readonly ConcurrentQueue<string> HistoricoDeLogs = new();
    private StringBuilder SB { get; } = new(1024);
    private static int MaxLogsNaTela { get; } = 5;
    private static int LinhaInicialDeLogs { get; set; }
    private IStringLocalizer<TorrentDownloadService> Localizer { get; } = localizer;
    private string Verde { get; } = "[green]";
    private string Vermelho { get; } = "[red]";
    private string Amarelo { get; } = "[yellow]";
    private readonly Dictionary<Guid, TorrentManager> _torrents = [];
    private string CliAtual { get; set; } = string.Empty;
    public async Task<Guid> BaixarAsync(string pastaTorrents, IProgress<double>? progress = null)
    public async Task<Guid> BaixarAsync(MagnetLink magnet, IProgress<double>? progress = null)
    public async Task<IHttpStream> StreamAsync(MagnetLink magnet, IProgress<double>? progress = null)
    private async Task EventoHandler(CancellationTokenSource cts, CancellationToken token)
    private async Task MainLoop(CancellationTokenSource cts, CancellationToken token, IProgress<double>? progress)
    private async Task AtualizarFilaDeDownloadsAsync()
    private async Task<CancellationTokenRegistration> CicloStreaming(IProgress<double>? progress, IHttpStream stream, MediaPlayer mediaPlayer, CancellationTokenSource cts, CancellationToken cancellationToken)
    private static async Task<(IHttpStream stream, LibVLC libVLC, MediaPlayer mediaPlayer, Media media)> VLC(TorrentManager manager, CancellationToken cancellationToken)
    private static string FormatarBytes(double bytes)
    private async Task<List<Torrent>> CarregarTodosTorrentsDaPasta(string caminhoDaPasta)
    private async Task<List<TorrentManager>> RegistrarTorrentsEngine(ClientEngine engine, AppSettings app, string pastaDosTorrents, string pastaDeDestino)
    private static string TempoEstimado(TorrentProgress p, double progresso)
    private static void BarraDeProgresso(IProgress<double>? progress, StringBuilder sb, double progresso, string titulo)
    private static TorrentSettings TorrentsConfig(AppSettings app)
    private TorrentProgress ObterProgresso(Guid id)
    private async Task AbortarAsync(Guid id)
    private void AdicionarLog(string mensagem)
    private static string Multi(int vezes = 0, char c = '\t')
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
    private readonly YoutubeClient _yt = yt;
``` 
 
## Análise de Dependências (Graphify) 
# Graph Report - C:\Users\Vitor\Documents\Repos\Projects\VideoDownloader  (2026-08-10)

## Corpus Check
- cluster-only mode — file stats not available

## Summary
- 165 nodes · 230 edges · 39 communities (12 shown, 27 thin omitted)
- Extraction: 100% EXTRACTED · 0% INFERRED · 0% AMBIGUOUS
- Token cost: 0 input · 0 output

## Graph Freshness
- Built from commit: `8dc77196`
- Run `git rev-parse HEAD` and compare to check if the graph is stale.
- Run `graphify update .` after code changes (no API cost).

## Community Hubs (Navigation)
- TorrentDownloadService
- YoutubeDLService
- ConsoleDownloadProgressBar
- Program
- VideoDownloader.csproj
- DownloadApplication
- .RegistrarTorrentsEngine
- .VLC
- DownloadApplication
- TorrentProgress.cs
- YoutubeExplodeService.cs
- bool
- int
- Container
- ConversionRequest
- DownloadProgress
- Func
- IAudioStreamInfo
- IReadOnlyList
- IVideoStreamInfo
- long
- Cookie
- bool
- int
- string
- IProgress
- List
- string
- StringBuilder
- SuppressMessage
- Task
- Cookie
- HashSet
- HashSet
- IProgress
- Task
- StreamManifest
- Task

## God Nodes (most connected - your core abstractions)
1. `TorrentDownloadService` - 24 edges
2. `YoutubeDLService` - 13 edges
3. `DownloadApplication` - 11 edges
4. `ConsoleDownloadProgressBar` - 10 edges
5. `Program` - 5 edges
6. `AppSettings` - 4 edges
7. `DownloadApplication` - 3 edges
8. `VideoDownloader.Services.Implementation` - 3 edges
9. `VideoDownloader.Constantes` - 2 edges
10. `YoutubeExplodeService` - 2 edges

## Surprising Connections (you probably didn't know these)
- `ConsoleDownloadProgressBar` --implements--> `IProgress`  [EXTRACTED]
  Progress/ConsoleDownloadProgressBar.cs →   _Bridges community 0 → community 2_
- `TorrentDownloadService` --references--> `ClientEngine`  [EXTRACTED]
  Services/Implementation/TorrentDownloadService.cs →   _Bridges community 6 → community 0_

## Import Cycles
- None detected.

## Communities (39 total, 27 thin omitted)

### Community 0 - "TorrentDownloadService"
Cohesion: 0.17
Nodes (14): CancellationToken, CancellationTokenRegistration, CancellationTokenSource, ConcurrentQueue, Dictionary, Guid, IHttpStream, IProgress (+6 more)

### Community 1 - "YoutubeDLService"
Cohesion: 0.19
Nodes (9): GeneratedRegex, HttpResponseMessage, IEnumerable, Regex, IProgress, List, Task, YoutubeDLService (+1 more)

### Community 2 - "ConsoleDownloadProgressBar"
Cohesion: 0.14
Nodes (11): bool, string, AppSettings, VideoDownloader.Progress, VideoDownloader.Constantes, IDisposable, int, object (+3 more)

### Community 3 - "Program"
Cohesion: 0.18
Nodes (7): Cookie, VideoDownloader, VideoDownloader.Services.Implementation, List, Task, Program, ServiceProvider

### Community 4 - "VideoDownloader.csproj"
Cohesion: 0.14
Nodes (13): net10.0, LibVLCSharp (3.10.1), Microsoft.Extensions.DependencyInjection (11.0.0-preview.6.26359.118), Microsoft.Extensions.Localization (11.0.0-preview.6.26359.118), Microsoft.Extensions.Logging (10.0.10), MonoTorrent (3.0.2), Spectre.Console (0.57.3-alpha.0.7), VideoLAN.LibVLC.Windows (3.0.23.1) (+5 more)

### Community 5 - "DownloadApplication"
Cohesion: 0.38
Nodes (4): SuppressMessage, Task, DownloadApplication, YoutubeExplodeService

### Community 6 - ".RegistrarTorrentsEngine"
Cohesion: 0.24
Nodes (7): AppSettings, ClientEngine, List, SuppressMessage, Torrent, TorrentManager, TorrentSettings

### Community 7 - ".VLC"
Cohesion: 0.25
Nodes (7): libVLC, media, mediaPlayer, LibVLC, Media, MediaPlayer, Stream

### Community 8 - "DownloadApplication"
Cohesion: 0.40
Nodes (4): VideoDownloader.Resources, CultureInfo, ResourceManager, DownloadApplication

### Community 9 - "TorrentProgress.cs"
Cohesion: 0.50
Nodes (3): VideoDownloader.Services, TorrentEstado, TorrentProgress

### Community 10 - "YoutubeExplodeService.cs"
Cohesion: 0.50
Nodes (3): VideoDownloader.Youtube.Implementation, YoutubeExplodeService, YoutubeClient

## Knowledge Gaps
- **19 isolated node(s):** `VideoDownloader.Resources`, `VideoDownloader.Services`, `TorrentEstado`, `TorrentProgress`, `VideoDownloader.Youtube.Implementation` (+14 more)
  These have ≤1 connection - possible missing edges or undocumented components.
- **27 thin communities (<3 nodes) omitted from report** — run `graphify query` to explore isolated nodes.

## Suggested Questions
_Questions this graph is uniquely positioned to answer:_

- **Why does `VideoDownloader.Services.Implementation` connect `Program` to `ConsoleDownloadProgressBar`?**
  _High betweenness centrality (0.238) - this node is a cross-community bridge._
- **Why does `TorrentDownloadService` connect `TorrentDownloadService` to `Program`, `.RegistrarTorrentsEngine`, `.VLC`?**
  _High betweenness centrality (0.228) - this node is a cross-community bridge._
- **Why does `YoutubeDLService` connect `YoutubeDLService` to `ConsoleDownloadProgressBar`?**
  _High betweenness centrality (0.141) - this node is a cross-community bridge._
- **What connects `VideoDownloader.Resources`, `VideoDownloader.Services`, `TorrentEstado` to the rest of the system?**
  _19 weakly-connected nodes found - possible documentation gaps or missing edges._
- **Should `ConsoleDownloadProgressBar` be split into smaller, more focused modules?**
  _Cohesion score 0.13970588235294118 - nodes in this community are weakly interconnected._
- **Should `VideoDownloader.csproj` be split into smaller, more focused modules?**
  _Cohesion score 0.14285714285714285 - nodes in this community are weakly interconnected._ 
