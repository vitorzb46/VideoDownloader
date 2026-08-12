using LibVLCSharp.Shared;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
namespace VideoDownloader.Player;

public partial class PlayerWindow : Window
{
    private static readonly string LogPath = Path.Combine(AppContext.BaseDirectory, "player-debug.log");
    private readonly PlayerViewModel _viewModel;
    private readonly ControlsWindow _controls;
    private readonly DispatcherTimer _inactivityTimer;

    private static void Log(string mensagem)
    {
        try
        {
            File.AppendAllText(LogPath, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] {mensagem}{Environment.NewLine}");
        }
        catch { /* log é best-effort */ }
    }

    public PlayerWindow(string mediaUrl)
    {
        Log($"PlayerWindow ctor | mediaUrl={mediaUrl}");
        InitializeComponent();

        Core.Initialize();
        var libVLC = new LibVLC();
        var mediaPlayer = new MediaPlayer(libVLC);

        _viewModel = new PlayerViewModel(libVLC, mediaPlayer);
        DataContext = _viewModel;

        VideoView.MediaPlayer = mediaPlayer;
        Log("MediaPlayer associado ao VideoView");

        // Janela de controles separada (evita o airspace do HWND nativo bloquear os cliques).
        // Owner é atribuído no Loaded (a janela dona precisa estar visível antes).
        _controls = new ControlsWindow(_viewModel);
        _controls.FullscreenRequested += (_, _) => ToggleFullscreen();
        _controls.ActivityDetected += (_, _) => ReiniciarTimerInatividade();
        _controls.Closed += (_, _) => Close();

        _inactivityTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(3)
        };
        _inactivityTimer.Tick += (_, _) => HideControls();

        Loaded += (_, _) =>
        {
            Log("Loaded disparado — posicionando controles");
            // Owner garante que a janela de controles fique sempre à frente do player.
            _controls.Owner = this;

            // --- RESOLUÇÃO DO BUG DO MENU FLUTUANTE (Foco do Windows) ---
            this.Activated += (s, e) => _controls.Topmost = true;
            this.Deactivated += (s, e) => _controls.Topmost = false;

            PosicionarControles();
            _controls.Show();
        };
        Loaded += async (_, _) =>
        {
            try
            {
                await IniciarAsync(mediaUrl);
                Log("IniciarAsync concluído");
            }
            catch (Exception ex)
            {
                Log($"IniciarAsync EXCEPTION: {ex}");
                File.AppendAllText(
                    Path.Combine(AppContext.BaseDirectory, "vlc-errors.log"),
                    $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] IniciarAsync EXCEPTION: {ex}{Environment.NewLine}");
                MessageBox.Show($"Falha ao iniciar o player:\n{ex.Message}", "Player",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        };
        Closed += (_, _) =>
        {
            Log("Window fechada — dispose do ViewModel");
            _inactivityTimer.Stop();
            _controls.Close();
            _viewModel.Dispose();
            Environment.Exit(0);
        };

        // Sincroniza a janela de controles com a janela de vídeo.
        LocationChanged += (_, _) => PosicionarControles();
        SizeChanged += (_, _) => PosicionarControles();
        StateChanged += (_, _) => PosicionarControles();
    }

    private async Task IniciarAsync(string mediaUrl)
    {
        _viewModel.IsLoading = true;
        Log("Criando Media");
        // Sem 'using': o PlayerViewModel é o dono do Media e faz o Dispose no fechamento.
        var media = new Media(_viewModel.LibVLC, mediaUrl, FromType.FromLocation);

        // Opções de rede para streaming (buffering + sync) — essencial para URL/stream.
        media.AddOption(":network-caching=3000");
        media.AddOption(":file-caching=3000");
        media.AddOption(":live-caching=3000");
        media.AddOption(":skip-frames");
        media.AddOption(":clock-synchro=0");
        media.AddOption(":clock-jitter=5000");

        _viewModel.SetMedia(media);
        Log("SetMedia + Play chamados");
        await _viewModel.PopulateTracksAsync();
        Log("PopulateTracksAsync concluído");
        ShowControls();
    }

    /// <summary>Posiciona a janela de controles na parte inferior da janela de vídeo.</summary>
    private void PosicionarControles()
    {
        if (_controls is null) return;

        // Se estiver em modo cinema, usamos a matemática baseada na tela cheia
        if (_viewModel.IsFullscreen)
        {
            _controls.Width = SystemParameters.PrimaryScreenWidth;
            _controls.Left = 0; // Zera a propriedade esquerda permanentemente na tela cheia
            _controls.Top = SystemParameters.PrimaryScreenHeight - _controls.Height;
        } 
        // Janela Maximizada (Botão maximizar do windows (do player))
        else if (this.WindowState == WindowState.Maximized)
        {
            _controls.Width = SystemParameters.WorkArea.Width;
            _controls.Left = SystemParameters.WorkArea.Left;
            _controls.Top = SystemParameters.WorkArea.Bottom - _controls.Height;
        }
        // Se estiver em modo janela normal, usamos a matemática baseada no Player (this)
        else
        {
            _controls.Width = this.ActualWidth;
            _controls.Left = this.Left;
            _controls.Top = this.Top + this.ActualHeight - _controls.Height;
        }
    }

    private void ReiniciarTimerInatividade()
    {
        if (_viewModel.IsFullscreen)
        {
            ShowControls();
            _inactivityTimer.Stop();
            _inactivityTimer.Start();
        }
    }

    // --- Modo cinema ---
    private void ShowControls()
    {
        _controls.Visibility = Visibility.Visible;
        Mouse.OverrideCursor = Cursors.Arrow;

        _inactivityTimer.Stop();
        _inactivityTimer.Start();
    }

    private void HideControls()
    {
        _inactivityTimer.Stop();

        if (_viewModel.IsFullscreen)
        {
            _controls.Visibility = Visibility.Collapsed;
            Mouse.OverrideCursor = Cursors.None;
        }
    }
    private void Player_Mouse(object sender, MouseEventArgs e)
    {
        if (_controls != null && _controls.Visibility != Visibility.Visible)
        {
            ShowControls();
        }

        // Se estiver em tela cheia, avisa o sistema para resetar o timer de inatividade
        if (_viewModel.IsFullscreen)
        {
            MouseDetected?.Invoke(this, EventArgs.Empty);
        }
    }
    private void ToggleFullscreen()
    {    
        if (WindowState == WindowState.Normal)
        {
            // Entra em tela cheia
            WindowStyle = WindowStyle.None;
            WindowState = WindowState.Maximized;
            _viewModel.IsFullscreen = true;
        }
        else
        {
            // Volta para o modo janela
            WindowStyle = WindowStyle.SingleBorderWindow;
            WindowState = WindowState.Normal;
            _viewModel.IsFullscreen = false;
        }
        
        _viewModel.IsFullscreen = WindowState == WindowState.Maximized;

        // Executa o posicionamento um milissegundo depois, garantindo que o Windows já mudou de tamanho
        Dispatcher.BeginInvoke(new Action(() => {
            PosicionarControles();
            ShowControls();
        }), DispatcherPriority.Render);
    }
    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);

        if (e.Key == Key.Escape && _viewModel.IsFullscreen)
        {
            ToggleFullscreen();
        }
    }
    /// <summary>Dispara quando há movimento do mouse sobre os controles (modo cinema).</summary>
    public event EventHandler? MouseDetected;
}
