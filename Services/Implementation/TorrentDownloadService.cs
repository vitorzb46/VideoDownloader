using MonoTorrent;
using MonoTorrent.Client;
using System.Globalization;
using System.Text;
using VideoDownloader.Constantes;
using VideoDownloader.Progress;

namespace VideoDownloader.Services.Implementation;

internal sealed class TorrentDownloadService(ClientEngine engine, AppSettings appContext)
{
    private readonly AppSettings AppContext = appContext;
    private ClientEngine Engine { get; } = engine;
    private StringBuilder SB { get; } = new(1024);
    private static int LinhaInicialDeLogs { get; } = 25;

    /// <summary>
    /// Baixa todos os arquivos torrents da pasta informada.
    /// </summary>
    /// <param name="pastaTorrents">Pasta onde estão os torrents.</param>
    /// <param name="progress">Barra de progresso.</param>
    /// <returns>Id único de um torrent.</returns>
    public async Task<Guid> BaixarAsync(string pastaTorrents, IProgress<double>? progress = null)
    {
        var id = Guid.NewGuid();
        var app = Directory.GetCurrentDirectory();
        Directory.CreateDirectory(Path.Combine(app, AppContext.PastaTorrents!));
        Directory.CreateDirectory(Path.Combine(app, AppContext.PastaCache!));
        // var torrents = Path.Combine(app, pastaTorrents!);

        try
        {
            //var torrentSetings = new TorrentSettings();
            List<TorrentManager> managers = await RegistrarTorrentsEngine(Engine, pastaTorrents, AppContext.PastaDownloads!).ConfigureAwait(false);

            await EventoHandler().ConfigureAwait(false);

            await MainLoop(progress).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
            throw;
        }

        return id;
    }
    /// <summary>
    /// Baixa urls magnéticas informadas.
    /// </summary>
    /// <param name="magnet">Url magnética.</param>
    /// <param name="progress">Barra de progresso.</param>
    /// <returns>Id único de um torrent.</returns>
    public async Task<Guid> BaixarAsync(MagnetLink magnet, CancellationToken? token = default, IProgress<double>? progress = null)
    {
        var id = Guid.NewGuid();

        var app = Directory.GetCurrentDirectory();
        Directory.CreateDirectory(Path.Combine(app, AppContext.PastaDownloads!));
        Directory.CreateDirectory(Path.Combine(app, AppContext.PastaCache!));
        var pastaDownload = Path.Combine(app, AppContext.PastaDownloads!);

        try
        {
            var torrentSetings = new TorrentSettings();
            await Engine.AddAsync(magnet, pastaDownload, torrentSetings).ConfigureAwait(false);
            await EventoHandler().ConfigureAwait(false);

            await MainLoop(progress).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erro ao baixar torrent: {ex.Message}");
            throw;
        }

        return id;
    }

    private async Task EventoHandler()
    {
        foreach (TorrentManager manager in Engine.Torrents)
        {
            manager.PeersFound += (o, e) =>
            {
                if (Console.GetCursorPosition().Top < LinhaInicialDeLogs) Console.SetCursorPosition(0, LinhaInicialDeLogs);

                if (e.NewPeers == 0) return;
                Console.WriteLine($"{e.GetType().Name}: {e.NewPeers} peers for {e.TorrentManager.Name}");
            };
            manager.PeerConnected += (o, e) =>
            {
                if (Console.GetCursorPosition().Top < LinhaInicialDeLogs) Console.SetCursorPosition(0, LinhaInicialDeLogs);

                Console.WriteLine($"Connection succeeded: {e.Peer.Uri}");
            };
            manager.ConnectionAttemptFailed += (o, e) =>
            {
                if (Console.GetCursorPosition().Top < LinhaInicialDeLogs) Console.SetCursorPosition(0, LinhaInicialDeLogs);

                Console.WriteLine($"Connection failed: {e.Peer.ConnectionUri}");
            };
            manager.TorrentStateChanged += async (o, e) =>
            {
                if (Console.GetCursorPosition().Top < LinhaInicialDeLogs) Console.SetCursorPosition(0, LinhaInicialDeLogs);

                Console.WriteLine($"[Status] {e.TorrentManager.Name}: {e.NewState}");
                if (e.NewState == TorrentState.Error)
                {
                    Console.WriteLine($"[Erro] O torrent parou devido a uma falha interna.");
                    await e.TorrentManager.StopAsync().ConfigureAwait(false);
                }
                if (e.NewState == TorrentState.Seeding)
                {
                    Console.WriteLine($"[Sucesso] {e.TorrentManager.Name} finalizado!");
                    // Adicionar Seeding depois
                    await e.TorrentManager.StopAsync().ConfigureAwait(false);
                }
            };
            await manager.StartAsync().ConfigureAwait(false);
        }
    }

    private async Task MainLoop(IProgress<double>? progress)
    {
        Console.CursorVisible = false;
        Console.Clear();
        while (Engine.IsRunning)
        {
            Console.SetCursorPosition(0, 0);

            SB.Remove(0, SB.Length);

            foreach (TorrentManager manager in Engine.Torrents)
            {
                double progresso = manager.Progress;

                if (manager.State == TorrentState.Seeding)
                {
                    progresso = 100.0;
                }
                string etaTexto = TempoEstimado(manager, progresso);
                double velocidadeDownload = Engine.TotalDownloadRate / 1048576.0;
                double velocidadeUpload = Engine.TotalUploadRate / 1048576.0;

                AppendSeparator(SB);
                AppendFormat(SB, "");
                AppendFormat(SB, $"{Multi(2)}{(manager.Torrent == null ? "Meta Data" : manager.Torrent.Name)}");
                AppendFormat(SB, "");
                AppendSeparator(SB);
                AppendFormat(SB, $" Status: {manager.State} | Tempo restante: {etaTexto} | Download: {velocidadeDownload:0.00} MB/s ↓ | Upload: {velocidadeUpload:0.00} MB/s ↑ | Peers: {manager.Peers.Seeds}/{manager.Peers.Available}");
                BarraDeProgresso(progress, SB, progresso, "Progresso: ");
            }

            Console.WriteLine(SB.ToString());
            await Task.Delay(35).ConfigureAwait(false);
        }
    }
    private string TempoEstimado(TorrentManager manager, double progresso)
    {
        long bytesRestantes = Engine.TotalDownloadRate - manager.Monitor.DataBytesReceived;
        double velocidade = manager.Monitor.DownloadRate;
        double etaSegundos = velocidade > 0 ? bytesRestantes / velocidade : double.PositiveInfinity;

        string etaTexto;
        if (manager.State == TorrentState.Seeding || progresso >= 100.0)
        {
            etaTexto = "Concluído ";
        }
        else if (double.IsPositiveInfinity(etaSegundos))
        {
            etaTexto = "Parado ";
        }
        else
        {
            TimeSpan etaTimeSpan = TimeSpan.FromSeconds(etaSegundos);
            etaTexto = etaTimeSpan.Days > 0
                ? etaTimeSpan.ToString(@"d\.hh\:mm\:ss", CultureInfo.CurrentCulture)
                : etaTimeSpan.ToString(@"hh\:mm\:ss", CultureInfo.CurrentCulture);
        }

        return etaTexto;
    }

    private static void BarraDeProgresso(IProgress<double>? progress, StringBuilder sb, double progresso, string titulo)
    {
        if (progress is null)
        {
            using (var temp = new ConsoleDownloadProgressBar(titulo))
            {
                double valorNormalizado = progresso / 100d;
                temp.Report(valorNormalizado, sb);
            }
            sb.AppendLine();
        }
        else if (progress is ConsoleDownloadProgressBar consoleBar)
        {
            double valorNormalizado = progresso / 100d;
            consoleBar.Report(valorNormalizado, sb);
            sb.AppendLine();
        }
    }

    private static void AppendSeparator(StringBuilder sb)
    {
        AppendFormat(sb, "");
        AppendFormat(sb, $"{Multi(100, '=')}");
        AppendFormat(sb, "");
    }
    private static void AppendFormat(StringBuilder sb, string str, params object[] formatting)
    {
        if (formatting != null && formatting.Length > 0)
            sb.AppendFormat(CultureInfo.InvariantCulture, str, formatting);
        else
            sb.Append(str);
        sb.AppendLine();
    }
    private static string Multi(int vezes = 0, char str = '\t')
    {
        return $"{new string(str, vezes)}";
    }
    private static async Task<List<Torrent>> CarregarTodosTorrentsDaPasta(string caminhoDaPasta)
    {
        var listaDeTorrents = new List<Torrent>();
        Directory.CreateDirectory(caminhoDaPasta);

        string[] arquivos = Directory.GetFiles(caminhoDaPasta, "*.torrent");

        var tarefas = arquivos.Select(async arquivo =>
        {
            try
            {
                Torrent torrent = await Torrent.LoadAsync(arquivo).ConfigureAwait(false);

                if (Path.GetExtension(torrent.Name) == ".scr")
                {
                    Console.WriteLine($"[Aviso] '{Path.GetFileName(arquivo)}' está tentando baixar cache externo e foi ignorado.");
                }
                else
                {
                    lock (listaDeTorrents)
                    {
                        listaDeTorrents.Add(torrent);
                    } 
                }                               
            }
            catch (Exception ex)
            {
                if (ex.Message.Contains("torrent", StringComparison.OrdinalIgnoreCase))
                {
                    Console.WriteLine($"[Erro] Falha ao carregar o arquivo '{Path.GetFileName(arquivo)}'");
                }
                else
                {
                    Console.WriteLine($"Erro ao processar '{Path.GetFileName(arquivo)}': {ex.Message}");
                }
            }
        });

        await Task.WhenAll(tarefas).ConfigureAwait(false);

        return listaDeTorrents;
    }
    private static async Task<List<TorrentManager>> RegistrarTorrentsEngine(ClientEngine engine, string pastaDosTorrents, string pastaDeDestino)
    {
        List<Torrent> listaDeTorrents = await CarregarTodosTorrentsDaPasta(pastaDosTorrents).ConfigureAwait(false);

        var listaDeManagers = new List<TorrentManager>();

        Console.WriteLine($"\nRegistrando {listaDeTorrents.Count} torrents no Engine...");

        foreach (var torrent in listaDeTorrents)
        {
            try
            {
                TorrentManager manager = await engine.AddAsync(torrent, pastaDeDestino).ConfigureAwait(false);
                listaDeManagers.Add(manager);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Erro] Não foi possível registrar {torrent.Name}: {ex.Message}");
            }
        }

        return listaDeManagers;
    }
}
