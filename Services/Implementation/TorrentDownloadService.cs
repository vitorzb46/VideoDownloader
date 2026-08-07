using MonoTorrent;
using MonoTorrent.Client;
using MonoTorrent.Trackers;
using System.Text;
using VideoDownloader.Constantes;

namespace VideoDownloader.Services.Implementation;

internal sealed class TorrentDownloadService(ClientEngine engine, AppSettings appContext)
{
    private readonly AppSettings AppContext = appContext;
    private ClientEngine Engine { get; } = engine;

    public async Task<Guid> BaixarAsync(MagnetLink magnet, CancellationToken? token = default)
    {
        var id = Guid.NewGuid();

        var app = Directory.GetCurrentDirectory();
        Directory.CreateDirectory(Path.Combine(app, AppContext.PastaDownloads!));
        Directory.CreateDirectory(Path.Combine(app, "cache"));
        var pastaDownload = Path.Combine(app, AppContext.PastaDownloads!);
        try
        {
            var torrentSetings = new TorrentSettings();
            await Engine.AddAsync(magnet, pastaDownload, torrentSetings).ConfigureAwait(false);

            foreach (TorrentManager manager in Engine.Torrents)
            {
                manager.PeersFound += (o, e) =>
                {
                    Console.WriteLine($"{e.GetType().Name}: {e.NewPeers} peers for {e.TorrentManager.Name}");
                };
                manager.PeerConnected += (o, e) =>
                {
                    Console.WriteLine($"Connection succeeded: {e.Peer.Uri}");
                };
                manager.ConnectionAttemptFailed += (o, e) =>
                {
                    //Console.WriteLine($"Connection failed: {e.Peer.ConnectionUri} - {e.Reason}");
                };
                manager.TorrentStateChanged += (o, e) =>
                {
                    //Console.WriteLine($"OldState: {e.OldState} NewState: {e.NewState}");
                };
                manager.TrackerManager.AnnounceComplete += (o, e) =>
                {
                    //Console.WriteLine($"{e.Successful}: {e.Tracker}");
                };
                await manager.StartAsync().ConfigureAwait(false);
            }

            Console.Clear(); // Limpa apenas uma vez antes do loop começar
            Console.CursorVisible = false;
            int count = 0;
            StringBuilder sb = new(1024);
            while (Engine.IsRunning)
            {
                Console.SetCursorPosition(0, 0);
                sb.Remove(0, sb.Length);
                foreach (TorrentManager manager in Engine.Torrents)
                {
                    double progresso = manager.Progress;

                    long bytesRestantes = Engine.TotalDownloadRate - manager.Monitor.DataBytesReceived;
                    double velocidade = manager.Monitor.DownloadRate;
                    double etaSegundos = velocidade > 0 ? bytesRestantes / velocidade : double.PositiveInfinity;

                    string etaTexto;
                    if (manager.State == TorrentState.Seeding || progresso >= 100)
                    {
                        etaTexto = "Concluído";
                    }
                    else if (double.IsPositiveInfinity(etaSegundos))
                    {
                        etaTexto = "Parado";
                    }
                    else
                    {
                        TimeSpan etaTimeSpan = TimeSpan.FromSeconds(etaSegundos);
                        etaTexto = etaTimeSpan.Days > 0
                            ? etaTimeSpan.ToString(@"d\.hh\:mm\:ss")
                            : etaTimeSpan.ToString(@"hh\:mm\:ss");
                    }

                    AppendSeparator(sb);
                    AppendFormat(sb, $" PROGRESSO DO DOWNLOAD");
                    AppendSeparator(sb);
                    AppendFormat(sb, $" Nome:                  {(manager.Torrent == null ? "MetaDataMode" : manager.Torrent.Name)}");
                    AppendFormat(sb, $" Status:                {manager.State}");
                    AppendFormat(sb, $" Progresso:             [{progresso}] {progresso:0.00}%");
                    AppendFormat(sb, "");
                    AppendFormat(sb, $" Velocidade de Download: {Engine.TotalDownloadRate / 1048576.0:0.00} MB/s ↓");
                    AppendFormat(sb, $" Velocidade de Upload:   {Engine.TotalUploadRate / 1048576.0:0.00} MB/s ↑");
                    AppendFormat(sb, $" Tempo Restante (ETA):   {etaTexto}");
                    AppendFormat(sb, $" Conexões Ativas:        {manager.Peers.Seeds} seeds conectados de {manager.Peers.Available} disponíveis");
                    AppendSeparator(sb);

                    AppendFormat(sb, $" Rastreamento (Tracker):  ({manager.TrackerManager.Tiers.Count} grupos ativos)");
                    AppendSeparator(sb);

                    // 2. Loop corrigido para listar cada tracker com base no seu TrackerState
                    AppendFormat(sb, $" LISTA DE RASTREADORES:");
                    foreach (var tier in manager.TrackerManager.Tiers)
                    {
                        foreach (var tracker in tier.Trackers)
                        {
                            // Define o ícone de acordo com o status real do enum TrackerState do MonoTorrent
                            string sinal = tracker.Status switch
                            {
                                TrackerState.Ok => "✔",
                                TrackerState.Offline => "❌",
                                TrackerState.Connecting => "⏳",
                                _ => "⏳" // Para estados como Unknown ou invalidados
                            };

                            AppendFormat(sb, $"   {sinal} {tracker.Uri}");
                        }
                    }
                    count++;
                }
                Console.WriteLine(sb.ToString());

                await Task.Delay(5000).ConfigureAwait(false);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erro ao baixar torrent: {ex.Message}");
            throw;
        }

        return id;
    }
    private static void AppendSeparator(StringBuilder sb)
    {
        AppendFormat(sb, "");
        AppendFormat(sb, "======================================================================");
        AppendFormat(sb, "");
    }
    private static void AppendFormat(StringBuilder sb, string str, params object[] formatting)
    {
        if (formatting != null && formatting.Length > 0)
            sb.AppendFormat(str, formatting);
        else
            sb.Append(str);
        sb.AppendLine();
    }
}
