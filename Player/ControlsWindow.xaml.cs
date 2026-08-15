using Microsoft.Win32;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using VideoDownloader.Services;

namespace VideoDownloader.Player;

public partial class ControlsWindow : Window
{
    private Log Log { get; set; } = new();
    private readonly PlayerViewModel _viewModel;
    private bool _isSeeking;

    /// <summary>Dispara quando o usuário alternar tela cheia (a janela de vídeo executa).</summary>
    public event EventHandler? FullscreenRequested;
    /// <summary>Dispara quando há movimento do mouse sobre os controles (modo cinema).</summary>
    public event EventHandler? ActivityDetected;    

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

    // --- Manipulador de clique para os sub-itens do menu de Áudio ---
    private void AudioMenuItem_Click(object sender, RoutedEventArgs e)
    {
        if (e.OriginalSource is MenuItem menuItem && menuItem.DataContext is TrackItem track)
        {
            Log.Salvar($"ContextMenu Subtitle: faixa {track.Id} ({track.Name})");
            _viewModel.SelectSubtitleTrack(track.Id);

            // FECHAMENTO AUTOMÁTICO: Localiza o menu pai e fecha
            FecharMenuConfiguracoes(menuItem);
        }
    }

    // --- Manipulador de clique para os sub-itens do menu de Legendas ---
    private void SubtitleMenuItem_Click(object sender, RoutedEventArgs e)
    {
        if (e.OriginalSource is MenuItem menuItem && menuItem.DataContext is TrackItem track)
        {
            Log.Salvar($"ContextMenu Subtitle: faixa {track.Id} ({track.Name})");
            _viewModel.SelectSubtitleTrack(track.Id);
        }
    }

    // --- Botões ---
    private void PlayPauseButton_Click(object sender, RoutedEventArgs e)
    {
        _viewModel.TogglePlay();
        PlayPauseButton.Content = _viewModel.IsPlaying ? "⏸" : "▶";
    }

    private void FullscreenButton_Click(object sender, RoutedEventArgs e)
    {
        FullscreenRequested?.Invoke(this, EventArgs.Empty);
    }

    private void LoadSubtitleButton_Click(object sender, RoutedEventArgs e)
    {
        Log.Salvar("LoadSubtitleButton_Click");
        var dialog = new OpenFileDialog
        {
            Filter = "Legendas (*.srt;*.vtt)|*.srt;*.vtt|Todos os arquivos (*.*)|*.*",
            Title = "Carregar legenda externa"
        };

        if (dialog.ShowDialog(this) == true)
        {
            Log.Salvar("Legenda {dialog.FileName} carregada!");
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

    private void SettingsButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.ContextMenu != null)
        {
            btn.ContextMenu.PlacementTarget = btn;
            btn.ContextMenu.Placement = System.Windows.Controls.Primitives.PlacementMode.Top;
            btn.ContextMenu.HorizontalOffset = -120;
            btn.ContextMenu.VerticalOffset = -25;
            btn.ContextMenu.IsOpen = true;
        }
    }

    /// <summary>
    /// Método auxiliar para encontrar o ContextMenu ancestral e fechá-lo de forma segura.
    /// </summary>
    private void FecharMenuConfiguracoes(DependencyObject elemento)
    {
        var atual = elemento;

        // Sobe na árvore de elementos até encontrar o ContextMenu pai
        while (atual != null && atual is not System.Windows.Controls.ContextMenu)
        {
            // Tenta pegar o pai lógico ou o pai visual usando um cast seguro
            atual = (atual as FrameworkElement)?.Parent ?? System.Windows.Media.VisualTreeHelper.GetParent(atual);
        }

        if (atual is ContextMenu menu)
        {
            menu.IsOpen = false;
        }
    }
}
