using LibVLCSharp.Shared;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;

namespace VideoDownloader.Player;

public sealed class PlayerViewModel : INotifyPropertyChanged, IDisposable
{
    private static readonly string LogPath = Path.Combine(AppContext.BaseDirectory, "player-debug.log");
    private readonly LibVLC _libVLC;
    private readonly MediaPlayer _mediaPlayer;
    private Media? _media;
    private bool _disposed;

    private bool _isLoading;
    private bool _isFullscreen;
    private bool _isPlaying;
    private double _position;
    private int _volume = 100;

    private static void Log(string mensagem)
    {
        try
        {
            File.AppendAllText(LogPath, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] VM: {mensagem}{Environment.NewLine}");
        }
        catch { /* log é best-effort */ }
    }

    public PlayerViewModel(LibVLC libVLC, MediaPlayer mediaPlayer)
    {
        _libVLC = libVLC;
        _mediaPlayer = mediaPlayer;

        _mediaPlayer.PositionChanged += OnPositionChanged;
        _mediaPlayer.Playing += OnPlaying;
        _mediaPlayer.Paused += OnPaused;
        _mediaPlayer.Stopped += OnStopped;
        _mediaPlayer.EndReached += OnEndReached;
        _mediaPlayer.Buffering += OnPlayerBuffering;
        _mediaPlayer.EncounteredError += (_, _) =>
        {
            Log($"EncounteredError | State={_mediaPlayer.State} | Mrl={_mediaPlayer.Media?.Mrl}");
            // Diagnóstico visível: escreve o erro do VLC em um arquivo de log local.
            try
            {
                File.AppendAllText(
                    Path.Combine(AppContext.BaseDirectory, "vlc-errors.log"),
                    $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] EncounteredError | State={_mediaPlayer.State} | Mrl={_mediaPlayer.Media?.Mrl}{Environment.NewLine}");
            }
            catch { /* log é best-effort */ }
        };

        TogglePlayCommand = new RelayCommand(TogglePlay);
        ToggleFullscreenCommand = new RelayCommand(ToggleFullscreen);
        LoadExternalSubtitleCommand = new RelayCommand(LoadExternalSubtitle);
    }

    public ObservableCollection<TrackItem> AudioTracks { get; } = [];
    public ObservableCollection<TrackItem> SubtitleTracks { get; } = [];

    public LibVLC LibVLC => _libVLC;

    public ICommand TogglePlayCommand { get; }
    public ICommand ToggleFullscreenCommand { get; }
    public ICommand LoadExternalSubtitleCommand { get; }

    public bool IsLoading
    {
        get => _isLoading;
        set { _isLoading = value; OnPropertyChanged(); }
    }

    public bool IsFullscreen
    {
        get => _isFullscreen;
        set { _isFullscreen = value; OnPropertyChanged(); }
    }

    public bool IsPlaying
    {
        get => _isPlaying;
        set { _isPlaying = value; OnPropertyChanged(); }
    }

    public double Position
    {
        get => _position;
        set
        {
            if (Math.Abs(_position - value) < 0.01) return;
            _position = value;
            OnPropertyChanged();
        }
    }

    public int Volume
    {
        get => _volume;
        set
        {
            var novo = Math.Clamp(value, 0, 100);
            if (novo == _volume) return;
            Log($"Volume {_volume} -> {novo}");
            _volume = novo;
            _mediaPlayer.Volume = _volume;
            OnPropertyChanged();
        }
    }

    public void SetMedia(Media media)
    {
        _media?.Dispose();
        _media = media;
        Log($"SetMedia | Mrl={media.Mrl}");
        _mediaPlayer.Play(_media);
        Log($"Play() chamado | State={_mediaPlayer.State}");
        IsLoading = true;
    }

    public void SeekTo(double percent)
    {
        Log($"SeekTo | percent={percent:0.0}");
        _mediaPlayer.Position = (float)Math.Clamp(percent / 100.0, 0.0, 1.0);
    }

    public void TogglePlay()
    {
        Log($"TogglePlay | IsPlaying={_mediaPlayer.IsPlaying} State={_mediaPlayer.State}");
        if (_mediaPlayer.IsPlaying)
        {
            _mediaPlayer.Pause();
        }
        else
        {
            _mediaPlayer.Play();
        }
    }

    public void ToggleFullscreen()
    {
        IsFullscreen = !IsFullscreen;
    }

    public void SelectAudioTrack(int trackId)
    {
        _mediaPlayer.SetAudioTrack(trackId);
    }

    public void SelectSubtitleTrack(int spuId)
    {
        _mediaPlayer.SetSpu(spuId);
    }

    public void LoadExternalSubtitle(object? filePath)
    {
        if (filePath is not string path || !File.Exists(path)) return;

        if (_mediaPlayer.AddSlave(MediaSlaveType.Subtitle, path, select: true))
        {
            SubtitleTracks.Add(new TrackItem(-99, Path.GetFileName(path)));
            OnPropertyChanged(nameof(SubtitleTracks));
        }
    }

    public async Task PopulateTracksAsync(CancellationToken ct = default)
    {
        // Aguarda o vídeo iniciar (o MediaPlayer precisa do media carregado), com timeout
        // para não travar a UI caso a mídia falhe (ex.: URL inacessível).
        var esperaInicio = Task.Delay(TimeSpan.FromSeconds(15), ct);
        while (!_mediaPlayer.IsPlaying && _mediaPlayer.State != VLCState.Ended && _mediaPlayer.State != VLCState.Error)
        {
            ct.ThrowIfCancellationRequested();
            if (await Task.WhenAny(Task.Delay(100, ct), esperaInicio).ConfigureAwait(true) == esperaInicio)
            {
                break; // timeout: segue para popular faixas mesmo se não iniciou
            }
        }

        // Garante que a mutação das ObservableCollection ocorra na UI thread (Dispatcher).
        if (Application.Current is { } app && !app.Dispatcher.CheckAccess())
        {
            await app.Dispatcher.InvokeAsync(() => PopulateTracksAsync(ct)).Task.ConfigureAwait(true);
            return;
        }

        var audioTracks = _mediaPlayer.AudioTrackDescription ?? [];
        AudioTracks.Clear();
        foreach (var t in audioTracks.Where(t => t.Id >= 0))
        {
            AudioTracks.Add(new TrackItem(t.Id, NomeDaFaixa(t.Name, "Áudio", t.Id)));
        }

        var spuTracks = _mediaPlayer.SpuDescription ?? [];
        SubtitleTracks.Clear();

        var legendasProcessadas = spuTracks
            .Where(t => t.Id >= 0 && !string.IsNullOrWhiteSpace(t.Name)) // Ignora faixas corrompidas ou nulas
            .Select(t => new TrackItem(t.Id, NomeDaFaixa(t.Name, "Legenda", t.Id)))
            .GroupBy(t => t.Name) // Remove legendas duplicadas com nomes idênticos no contêiner do torrent
            .Select(g => g.First())
            .OrderByDescending(t => t.Name.Contains("Português")) // Joga Português para o topo do ContextMenu
            .ThenByDescending(t => t.Name.Contains("Inglês"))     // Joga Inglês em segundo
            .ThenBy(t => t.Name);

        SubtitleTracks.Add(new TrackItem(-1, "❌ Desativar Legendas"));

        foreach (var item in legendasProcessadas)
        {
            SubtitleTracks.Add(item);
        }

        IsLoading = false;
    }

    private static readonly Dictionary<string, string> Idiomas = new(StringComparer.OrdinalIgnoreCase)
    {
        ["pt"] = "Português", ["por"] = "Português", ["pt-br"] = "Português (BR)",
        ["en"] = "Inglês", ["eng"] = "Inglês",
        ["es"] = "Espanhol", ["spa"] = "Espanhol",
        ["fr"] = "Francês", ["fre"] = "Francês", ["fra"] = "Francês",
        ["de"] = "Alemão", ["ger"] = "Alemão", ["deu"] = "Alemão",
        ["it"] = "Italiano", ["ita"] = "Italiano",
        ["ja"] = "Japonês", ["jpn"] = "Japonês",
        ["ko"] = "Coreano", ["kor"] = "Coreano",
        ["zh"] = "Chinês", ["zho"] = "Chinês",
        ["ru"] = "Russo", ["rus"] = "Russo",
        ["ar"] = "Árabe", ["ara"] = "Árabe",
        ["hi"] = "Hindi",
        ["nl"] = "Holandês", ["nld"] = "Holandês",
        ["sv"] = "Sueco", ["swe"] = "Sueco",
        ["pl"] = "Polonês", ["pol"] = "Polonês",
    };

    /// <summary>
    /// Gera um nome legível para uma faixa: mapeia códigos de idioma (ex.: "por" → "Português"),
    /// converte "Track N" genérico em "Áudio N"/"Legenda N" e preserva descrições reais (ex.: "AC-3").
    /// </summary>
    private static string NomeDaFaixa(string? nome, string tipo, int id)
    {
        if (string.IsNullOrWhiteSpace(nome))
        {
            return $"{tipo} {id}";
        }

        var limpo = nome.Trim().ToLower();

        if (limpo.Contains("por") || limpo.Contains("pt") || limpo.Contains("portuguese"))
        {
            return "Português (BR)";
        }
        if (limpo.Contains("eng") || limpo.Contains("en") || limpo.Contains("english"))
        {
            return "Inglês";
        }
        if (limpo.Contains("spa") || limpo.Contains("es") || limpo.Contains("spanish") || limpo.Contains("espanol"))
        {
            return "Espanhol";
        }
        if (limpo.Contains("fre") || limpo.Contains("fr") || limpo.Contains("french"))
        {
            return "Francês";
        }
        if (limpo.Contains("ger") || limpo.Contains("de") || limpo.Contains("german"))
        {
            return "Alemão";
        }
        if (limpo.Contains("jap") || limpo.Contains("ja") || limpo.Contains("japanese"))
        {
            return "Japonês";
        }

        foreach (var idioma in Idiomas)
        {
            if (limpo.Contains(idioma.Key))
            {
                return idioma.Value;
            }
        }

        // Se for o padrão "Track N" do VLC sem metadados, deixa descritivo
        if (limpo.StartsWith("track", StringComparison.OrdinalIgnoreCase))
        {
            return $"{tipo} {id}";
        }
        
        return nome.Trim();
    }

    private void OnPositionChanged(object? sender, MediaPlayerPositionChangedEventArgs e)
    {
        // O VLC dispara em thread própria: marshall para a UI thread antes de tocar no binding.
        if (Application.Current is { } app && !app.Dispatcher.CheckAccess())
        {
            app.Dispatcher.BeginInvoke(() => OnPositionChanged(sender, e));
            return;
        }

        // O VLC pode reportar NaN enquanto ainda busca metadata/peers — ignora para não quebrar o slider.
        if (double.IsNaN(e.Position) || double.IsInfinity(e.Position)) return;

        Position = e.Position * 100.0;
    }

    private void OnPlaying(object? sender, EventArgs e)
    {
        Log($"EVENT Playing | State={_mediaPlayer.State}");
        IsPlaying = true;
        IsLoading = false;
    }
    private void OnPaused(object? sender, EventArgs e)
    {
        Log("EVENT Paused");
        IsPlaying = false;
    }
    private void OnStopped(object? sender, EventArgs e)
    {
        Log("EVENT Stopped");
        IsPlaying = false;
    }
    private void OnEndReached(object? sender, EventArgs e)
    {
        Log("EVENT EndReached");
        IsPlaying = false;
    }
    private void OnPlayerBuffering(object? sender, MediaPlayerBufferingEventArgs e)
    {
        Application.Current?.Dispatcher.BeginInvoke(() =>
        {
            float cachePreenchido = e.Cache;

            if (cachePreenchido < 100)
            {
                IsLoading = true;
                Log($"[ALERTA REDE] Buffer abaixo do padrão! Reabastecendo: {cachePreenchido:0.0}%");
            }
            else
            {
                IsLoading = false;
                Log("[ALERTA REDE] Buffer cheio. Continuando reprodução.");
            }
        });
    }   
    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string? name = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        Log("Dispose do ViewModel iniciado");

        // Remove imediatamente as inscrições de eventos para evitar callbacks fantasmas
        _mediaPlayer.PositionChanged -= OnPositionChanged;
        _mediaPlayer.Playing -= OnPlaying;
        _mediaPlayer.Paused -= OnPaused;
        _mediaPlayer.Stopped -= OnStopped;
        _mediaPlayer.EndReached -= OnEndReached;
        _mediaPlayer.Buffering -= OnPlayerBuffering;
        
        try
        {
            if (_mediaPlayer.IsPlaying)
            {
                _mediaPlayer.Stop();
            }

            _media?.Dispose();
            _media = null;

            // Descarta o MediaPlayer e depois a instância do LibVLC
            _mediaPlayer.Dispose();
            _libVLC.Dispose();
        }
        catch (Exception ex)
        {
            Log($"Erro durante o dispose nativo do VLC: {ex.Message}");
        }

        Log("Dispose do ViewModel concluído");
    }
}
