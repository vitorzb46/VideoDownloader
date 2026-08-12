using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using Microsoft.Win32;

namespace VideoDownloader.Player;

public partial class ControlsWindow : Window
{
    private static readonly string LogPath = Path.Combine(AppContext.BaseDirectory, "player-debug.log");
    private readonly PlayerViewModel _viewModel;
    private bool _isSeeking;

    /// <summary>Dispara quando o usuário pede alternar tela cheia (a janela de vídeo executa).</summary>
    public event EventHandler? FullscreenRequested;

    private static void Log(string mensagem)
    {
        try
        {
            File.AppendAllText(LogPath, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] {mensagem}{Environment.NewLine}");
        }
        catch { /* log é best-effort */ }
    }

    public ControlsWindow(PlayerViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = viewModel;
    }

    // --- Timeline (proteção contra loop) ---
    private void TimelineSlider_PreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
        _isSeeking = true;
    }

    private void TimelineSlider_PreviewMouseUp(object sender, MouseButtonEventArgs e)
    {
        _isSeeking = false;
        _viewModel.SeekTo(TimelineSlider.Value);
    }

    // --- Áudio / Legendas ---
    private void AudioCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (AudioCombo.SelectedItem is TrackItem track)
        {
            Log($"AudioCombo: faixa {track.Id} ({track.Name})");
            _viewModel.SelectAudioTrack(track.Id);
        }
    }

    private void SubtitleCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (SubtitleCombo.SelectedItem is TrackItem track)
        {
            Log($"SubtitleCombo: faixa {track.Id} ({track.Name})");
            _viewModel.SelectSubtitleTrack(track.Id);
        }
    }

    // --- Botões ---
    private void PlayPauseButton_Click(object sender, RoutedEventArgs e)
    {
        _viewModel.TogglePlay();
        PlayPauseButton.Content = _viewModel.IsPlaying ? "Pausar" : "Play";
    }

    private void FullscreenButton_Click(object sender, RoutedEventArgs e)
    {
        FullscreenRequested?.Invoke(this, EventArgs.Empty);
    }

    private void LoadSubtitleButton_Click(object sender, RoutedEventArgs e)
    {
        Log("LoadSubtitleButton_Click");
        var dialog = new OpenFileDialog
        {
            Filter = "Legendas (*.srt;*.vtt)|*.srt;*.vtt|Todos os arquivos (*.*)|*.*",
            Title = "Carregar legenda externa"
        };

        if (dialog.ShowDialog(this) == true)
        {
            _viewModel.LoadExternalSubtitle(dialog.FileName);
        }
    }

    // --- Modo cinema: qualquer movimento do mouse na janela de controles reinicia o timer de inatividade ---
    private void Window_MouseMove(object sender, MouseEventArgs e)
    {
        if (_viewModel.IsFullscreen)
        {
            // Sinaliza atividade para a janela de vídeo reiniciar o timer de inatividade.
            ActivityDetected?.Invoke(this, EventArgs.Empty);
        }
    }

    /// <summary>Dispara quando há movimento do mouse sobre os controles (modo cinema).</summary>
    public event EventHandler? ActivityDetected;
}
