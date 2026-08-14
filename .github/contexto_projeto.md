# Arquitetura do Projeto 
 
## Estrutura de Pastas 
``` 
- Constantes 
- docs 
- Player 
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
public class AcessoProjetoPrincipal
    public string? HttpPrefix { get; set; }
    public void SetPrefix(string prefix)
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
 
### Arquivo: App.xaml.cs 
```csharp 
namespace VideoDownloader.Player;
public partial class App : Application
    private AcessoProjetoPrincipal Acesso { get; } = new();
    protected override void OnStartup(StartupEventArgs e)
``` 
 
### Arquivo: AssemblyInfo.cs 
```csharp 
``` 
 
### Arquivo: ControlsWindow.xaml.cs 
```csharp 
namespace VideoDownloader.Player;
public partial class ControlsWindow : Window
    private Log Log { get; set; } = new();
    private readonly PlayerViewModel _viewModel;
    private bool _isSeeking;
    public event EventHandler? FullscreenRequested;
    public event EventHandler? ActivityDetected;    
    public ControlsWindow(PlayerViewModel viewModel)
    private void TimelineSlider_PreviewMouseDown(object sender, MouseButtonEventArgs e)
    private void TimelineSlider_PreviewMouseUp(object sender, MouseButtonEventArgs e)
    private void AudioMenuItem_Click(object sender, RoutedEventArgs e)
    private void SubtitleMenuItem_Click(object sender, RoutedEventArgs e)
    private void PlayPauseButton_Click(object sender, RoutedEventArgs e)
    private void FullscreenButton_Click(object sender, RoutedEventArgs e)
    private void LoadSubtitleButton_Click(object sender, RoutedEventArgs e)
    private void Window_MouseMove(object sender, MouseEventArgs e)
    private void SettingsButton_Click(object sender, RoutedEventArgs e)
    private void FecharMenuConfiguracoes(DependencyObject elemento)
``` 
 
### Arquivo: PlayerViewModel.cs 
```csharp 
namespace VideoDownloader.Player;
public sealed class PlayerViewModel : INotifyPropertyChanged, IDisposable
    private Log Log {get; set;} = new();
    private readonly LibVLC _libVLC;
    private readonly MediaPlayer _mediaPlayer;
    private Media? _media;
    private bool _disposed;
    private bool _isLoading;
    private bool _isFullscreen;
    private bool _isPlaying;
    private double _position;
    private int _volume = 100;
    public PlayerViewModel(LibVLC libVLC, MediaPlayer mediaPlayer)
    public ObservableCollection<TrackItem> AudioTracks { get; } = [];
    public ObservableCollection<TrackItem> SubtitleTracks { get; } = [];
    public LibVLC LibVLC => _libVLC;
    public ICommand TogglePlayCommand { get; }
    public ICommand ToggleFullscreenCommand { get; }
    public ICommand LoadExternalSubtitleCommand { get; }
    public bool IsLoading
    public bool IsFullscreen
    public bool IsPlaying
    public double Position
    public int Volume
    public void SetMedia(Media media)
    public void SeekTo(double percent)
    public void TogglePlay()
    public void ToggleFullscreen()
    public void SelectAudioTrack(int trackId)
    public void SelectSubtitleTrack(int spuId)
    public void LoadExternalSubtitle(object? filePath)
    public async Task PopulateTracksAsync(CancellationToken ct = default)
    private static readonly Dictionary<string, string> Idiomas = new(StringComparer.OrdinalIgnoreCase)
    private static string NomeDaFaixa(string? nome, string tipo, int id)
    private void OnPositionChanged(object? sender, MediaPlayerPositionChangedEventArgs e)
    private void OnPlaying(object? sender, EventArgs e)
    private void OnPaused(object? sender, EventArgs e)
    private void OnStopped(object? sender, EventArgs e)
    private void OnEndReached(object? sender, EventArgs e)
    private void OnPlayerBuffering(object? sender, MediaPlayerBufferingEventArgs e)
    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string? name = null)
    public void Dispose()
``` 
 
### Arquivo: PlayerWindow.xaml.cs 
```csharp 
namespace VideoDownloader.Player;
public partial class PlayerWindow : Window
    private Log Log { get; set; } = new();
    private readonly PlayerViewModel _viewModel;
    private readonly ControlsWindow _controls;
    private readonly DispatcherTimer _inactivityTimer;
    public PlayerWindow(string mediaUrl)
    private async Task IniciarAsync(string mediaUrl)
    private void PosicionarControles()
    private void ReiniciarTimerInatividade()
    private void ShowControls()
    private void HideControls()
    private void Player_Mouse(object sender, MouseEventArgs e)
    private void ToggleFullscreen()
    protected override void OnKeyDown(KeyEventArgs e)
    public event EventHandler? MouseDetected;
``` 
 
### Arquivo: RelayCommand.cs 
```csharp 
namespace VideoDownloader.Player;
internal sealed class RelayCommand : ICommand
    private readonly Action<object?> _execute;
    private readonly Func<object?, bool>? _canExecute;
    public RelayCommand(Action execute, Func<bool>? canExecute = null)
    public RelayCommand(Action<object?> execute, Func<object?, bool>? canExecute = null)
    public event EventHandler? CanExecuteChanged;
    public bool CanExecute(object? parameter) => _canExecute?.Invoke(parameter) ?? true;
    public void Execute(object? parameter) => _execute(parameter);
    public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
public sealed record TrackItem(int Id, string Name);
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
 
### Arquivo: Log.cs 
```csharp 
namespace VideoDownloader.Services;
public class Log
    private static readonly ConcurrentQueue<string> HistoricoDeLogs = new();
    private static readonly string LogPath = Path.Combine(AppContext.BaseDirectory, "player-debug.log");
    private static string? Mensagem { get; set; }
    public static string CliAtual { get; set; } = string.Empty;
    private static int MaxLogsNaTela { get; set; } = 15;
    private static bool ExibirArquivoLog { get; set; }
    public static StringBuilder SB { get; set; } = new();
    public static StringBuilder SBLog { get; set; } = new();
    public static void Limpar() => SB.Clear();
    public static void Adicionar(string mensagem)
    public static void Listar(string mensagem) => Listar(mensagem, false);
    public static void Listar(string mensagem, bool salvarLog)
    public static void Imprimir()
    private static void Salvar()
    public static string Multi(int vezes = 0, char c = '\t')
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
internal sealed record StreamResult(string HttpPrefix, string FullUri);
internal sealed class TorrentDownloadService(ClientEngine engine, AppSettings appContext, Log log, IStringLocalizer<TorrentDownloadService> localizer)
    private readonly AppSettings AppContext = appContext;
    private ClientEngine Engine { get; set; } = engine;
    private Log Log { get; set; } = log;
    private IStringLocalizer<TorrentDownloadService> Localizer { get; } = localizer;
    private string Verde { get; } = "[green]";
    private string Vermelho { get; } = "[red]";
    private string Amarelo { get; } = "[yellow]";
    private static readonly Dictionary<Guid, TorrentManager> _torrents = [];
    public async Task<Guid> BaixarAsync(string pastaTorrents, IProgress<double>? progress = null)
    public async Task<Guid> BaixarAsync(MagnetLink magnet, IProgress<double>? progress = null)
    public async Task<StreamResult> StreamAsync(MagnetLink magnet, IProgress<double>? progress = null)
    private async Task EventoHandler(CancellationTokenSource cts, CancellationToken token)
    private async Task MainLoop(CancellationTokenSource cts, CancellationToken token, IProgress<double>? progress)
    private async Task<Process?> LoopPlayer(IHttpStream? stream, CancellationToken cancellationToken)
    private static async Task StreamBuffer(TorrentManager manager, CancellationToken cancellationToken)
    private static async Task AtualizarFilaDeDownloadsAsync()
    private static string FormatarBytes(double bytes)
    private async Task<List<Torrent>> CarregarTodosTorrentsDaPasta(string caminhoDaPasta)
    private async Task<List<TorrentManager>> RegistrarTorrentsEngine(ClientEngine engine, AppSettings app, string pastaDosTorrents, string pastaDeDestino)
    private static string TempoEstimado(TorrentProgress p, double progresso)
    private static void BarraDeProgresso(IProgress<double>? progress, StringBuilder sb, double progresso, string titulo)
    private static TorrentSettings TorrentsConfig(AppSettings app)
    private static TorrentProgress ObterProgresso(Guid id)
    private async Task AbortarAsync(Guid id)
    public IReadOnlyList<(Guid Id, string Nome, TorrentEstado Estado, int Seeds, int Peers)> EstadoDosTorrents()
    private static TorrentEstado ConverterEstado(TorrentManager manager)
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
# Graph Report - C:\Users\Vitor\Documents\Repos\Projects\VideoDownloader  (2026-08-14)

## Corpus Check
- cluster-only mode — file stats not available

## Summary
- 295 nodes · 436 edges · 44 communities (13 shown, 31 thin omitted)
- Extraction: 98% EXTRACTED · 2% INFERRED · 0% AMBIGUOUS · INFERRED: 8 edges (avg confidence: 0.8)
- Token cost: 0 input · 0 output

## Graph Freshness
- Built from commit: `75b4254b`
- Run `git rev-parse HEAD` and compare to check if the graph is stale.
- Run `graphify update .` after code changes (no API cost).

## Community Hubs (Navigation)
- TorrentDownloadService
- Window
- PlayerViewModel
- RelayCommand
- YoutubeDLService
- VideoDownloader
- PlayerWindow
- DownloadApplication
- ConsoleDownloadProgressBar
- Program
- DownloadApplication
- YoutubeExplodeService.cs
- CancellationToken
- CancellationTokenRegistration
- bool
- int
- Container
- ConversionRequest
- libVLC
- media
- mediaPlayer
- Dictionary
- DownloadProgress
- IAudioStreamInfo
- IVideoStreamInfo
- List
- long
- Cookie
- bool
- int
- string
- string
- Cookie
- HashSet
- HashSet
- IProgress
- Task
- Stream
- StreamManifest
- StringBuilder
- SuppressMessage
- Task
- Task

## God Nodes (most connected - your core abstractions)
1. `PlayerViewModel` - 33 edges
2. `TorrentDownloadService` - 23 edges
3. `Window` - 17 edges
4. `ControlsWindow` - 17 edges
5. `PlayerWindow` - 15 edges
6. `VideoDownloader` - 13 edges
7. `YoutubeDLService` - 13 edges
8. `Log` - 13 edges
9. `DownloadApplication` - 11 edges
10. `ConsoleDownloadProgressBar` - 10 edges

## Surprising Connections (you probably didn't know these)
- `ControlsWindow` --references--> `Log`  [EXTRACTED]
  Player/ControlsWindow.xaml.cs → Services/Log.cs
- `PlayerWindow` --references--> `Log`  [EXTRACTED]
  Player/PlayerWindow.xaml.cs → Services/Log.cs
- `VideoDownloader.Player` --references--> `Microsoft.NET.Sdk`  [EXTRACTED]
  Player/VideoDownloader.Player.csproj → VideoDownloader.csproj
- `PlayerViewModel` --references--> `TrackItem`  [EXTRACTED]
  Player/PlayerViewModel.cs → Player/RelayCommand.cs
- `PlayerViewModel` --references--> `Log`  [EXTRACTED]
  Player/PlayerViewModel.cs → Services/Log.cs

## Import Cycles
- None detected.

## Communities (44 total, 31 thin omitted)

### Community 0 - "TorrentDownloadService"
Cohesion: 0.08
Nodes (29): AppSettings, CancellationTokenSource, ClientEngine, Estado, Guid, Id, IHttpStream, IReadOnlyList (+21 more)

### Community 1 - "Window"
Cohesion: 0.07
Nodes (24): AudioTracks, Position, SubtitleTracks, Volume, DependencyObject, MouseButtonEventArgs, Arrow, Border (+16 more)

### Community 2 - "PlayerViewModel"
Cohesion: 0.09
Nodes (19): ConcurrentQueue, double, EventArgs, INotifyPropertyChanged, LibVLC, Media, MediaPlayer, MediaPlayerBufferingEventArgs (+11 more)

### Community 3 - "RelayCommand"
Cohesion: 0.09
Nodes (13): AcessoProjetoPrincipal, Action, VideoDownloader.Player, VideoDownloader.Services, Func, ICommand, Application, App (+5 more)

### Community 4 - "YoutubeDLService"
Cohesion: 0.19
Nodes (9): GeneratedRegex, HttpResponseMessage, IEnumerable, Regex, IProgress, List, Task, YoutubeDLService (+1 more)

### Community 5 - "VideoDownloader"
Cohesion: 0.11
Nodes (18): net10.0, net10.0-windows, LibVLCSharp (3.10.1), LibVLCSharp.WPF (3.10.1), Microsoft.Extensions.DependencyInjection (11.0.0-preview.6.26359.118), Microsoft.Extensions.Localization (11.0.0-preview.6.26359.118), Microsoft.Extensions.Logging (10.0.10), MonoTorrent (3.0.2) (+10 more)

### Community 6 - "PlayerWindow"
Cohesion: 0.16
Nodes (10): BoolToVisibilityConverter, IsLoading, DispatcherTimer, KeyEventArgs, VideoView, Window, MouseEventArgs, PlayerWindow (+2 more)

### Community 7 - "DownloadApplication"
Cohesion: 0.24
Nodes (6): VideoDownloader.Services.Implementation, SuppressMessage, Task, AcessoProjetoPrincipal, DownloadApplication, YoutubeExplodeService

### Community 8 - "ConsoleDownloadProgressBar"
Cohesion: 0.14
Nodes (12): bool, string, AppSettings, VideoDownloader.Progress, VideoDownloader.Constantes, IDisposable, int, IProgress (+4 more)

### Community 9 - "Program"
Cohesion: 0.24
Nodes (6): Cookie, VideoDownloader, List, Task, Program, ServiceProvider

### Community 10 - "DownloadApplication"
Cohesion: 0.40
Nodes (4): VideoDownloader.Resources, CultureInfo, ResourceManager, DownloadApplication

### Community 11 - "YoutubeExplodeService.cs"
Cohesion: 0.50
Nodes (3): VideoDownloader.Youtube.Implementation, YoutubeExplodeService, YoutubeClient

## Knowledge Gaps
- **33 isolated node(s):** `VideoDownloader.Resources`, `VideoDownloader.Constantes`, `VideoDownloader.Youtube.Implementation`, `VideoDownloader.Progress`, `Track` (+28 more)
  These have ≤1 connection - possible missing edges or undocumented components.
- **31 thin communities (<3 nodes) omitted from report** — run `graphify query` to explore isolated nodes.

## Suggested Questions
_Questions this graph is uniquely positioned to answer:_

- **Why does `PlayerViewModel` connect `PlayerViewModel` to `ConsoleDownloadProgressBar`, `Window`, `RelayCommand`, `PlayerWindow`?**
  _High betweenness centrality (0.180) - this node is a cross-community bridge._
- **Why does `VideoDownloader.Services.Implementation` connect `DownloadApplication` to `TorrentDownloadService`?**
  _High betweenness centrality (0.139) - this node is a cross-community bridge._
- **What connects `VideoDownloader.Resources`, `VideoDownloader.Constantes`, `VideoDownloader.Youtube.Implementation` to the rest of the system?**
  _33 weakly-connected nodes found - possible documentation gaps or missing edges._
- **Should `TorrentDownloadService` be split into smaller, more focused modules?**
  _Cohesion score 0.0824829931972789 - nodes in this community are weakly interconnected._
- **Should `Window` be split into smaller, more focused modules?**
  _Cohesion score 0.07307692307692308 - nodes in this community are weakly interconnected._
- **Should `PlayerViewModel` be split into smaller, more focused modules?**
  _Cohesion score 0.0873015873015873 - nodes in this community are weakly interconnected._
- **Should `RelayCommand` be split into smaller, more focused modules?**
  _Cohesion score 0.08666666666666667 - nodes in this community are weakly interconnected._ 
