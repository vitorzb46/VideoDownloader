using System.IO;
using System.Windows;

namespace VideoDownloader.Player;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    private AcessoProjetoPrincipal Acesso { get; } = new();

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Logs limpos a cada execução para o timeline não misturar sessões.
        foreach (var arquivo in new[] { "player-debug.log", "vlc-errors.log" })
        {
            try
            {
                var caminho = Path.Combine(AppContext.BaseDirectory, arquivo);
                if (File.Exists(caminho)) File.Delete(caminho);
            }
            catch { /* best-effort */ }
        }

        // Uso: VideoDownloader.Player.exe <url-do-stream>
        var mediaUrl = e.Args.Length > 0
            ? e.Args[0]
            : Acesso.HttpPrefix;

        var window = new PlayerWindow(mediaUrl!);
        window.Show();
    }
}