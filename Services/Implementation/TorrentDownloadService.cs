using Microsoft.Extensions.Localization;
using MonoTorrent;
using MonoTorrent.Client;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text;
using VideoDownloader.Constantes;
using VideoDownloader.Progress;

namespace VideoDownloader.Services.Implementation;

[SuppressMessage("Performance", "CA1812:Avoid uninstantiated internal classes")]
internal sealed class TorrentDownloadService(ClientEngine engine, AppSettings appContext, IStringLocalizer<TorrentDownloadService> localizer)
{
    private readonly AppSettings AppContext = appContext;
    private ClientEngine Engine { get; set; } = engine;
    private StringBuilder SB { get; } = new(1024);
    private static int LinhaInicialDeLogs { get; } = 25;
    private IStringLocalizer<TorrentDownloadService> Localizer { get; } = localizer;

    // Cores ANSI (funcionam no Windows Terminal / VS Code integrado)
    private const string Verde = "\x1b[32m";
    private const string Amarelo = "\x1b[33m";
    private const string Vermelho = "\x1b[31m";
    private const string Ciano = "\x1b[36m";
    private const string Reset = "\x1b[0m";
    private readonly Dictionary<Guid, TorrentManager> _torrents = [];
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
            var managers = await RegistrarTorrentsEngine(Engine, AppContext, pastaTorrents, AppContext.PastaDownloads!).ConfigureAwait(false);

            if (managers.Count == 0)
            {
                Console.WriteLine(Localizer["Torrent_NenhumEncontrado", pastaTorrents]);
                return id;
            }

            foreach (var manager in managers)
            {
                _torrents[Guid.NewGuid()] = manager;
            }

            await EventoHandler().ConfigureAwait(false);

            await MainLoop(progress).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Console.WriteLine(Localizer["Torrent_ErroAoBaixar", ex.Message]);
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
            TorrentSettings settingsBuilder = TorrentsConfig(AppContext);

            var manager = await Engine.AddAsync(magnet, pastaDownload, settingsBuilder).ConfigureAwait(false);

            _torrents[id] = manager;

            await EventoHandler().ConfigureAwait(false);

            await MainLoop(progress).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Console.WriteLine(Localizer["Torrent_ErroAoBaixar", ex.Message]);
            throw;
        }

        return id;
    }
    /// <summary>
    /// Inicia o streaming de um magnet link, retornando uma Stream nativa do .NET
    /// para assistir antes do download terminar (requer codec com metadados no início, ex.: MP4).
    /// </summary>
    /// <param name="magnet">Url magnética.</param>
    /// <param name="token">Token de cancelamento.</param>
    /// <returns>Stream do primeiro arquivo do torrent.</returns>
    public async Task<Stream> StreamAsync(MagnetLink magnet, CancellationToken? token = default)
    {
        var cancellationToken = token ?? CancellationToken.None;

        var pastaDownload = Path.Combine(Directory.GetCurrentDirectory(), AppContext.PastaDownloads ?? "Downloads");
        Directory.CreateDirectory(pastaDownload);

        var manager = await Engine.AddStreamingAsync(magnet, pastaDownload).ConfigureAwait(false);
        await manager.StartAsync().ConfigureAwait(false);

        var arquivo = manager.Files.FirstOrDefault();
        return arquivo is null || manager.StreamProvider is null
            ? throw new InvalidOperationException("Nenhum arquivo disponível para streaming.")
            : await manager.StreamProvider.CreateStreamAsync(arquivo, cancellationToken).ConfigureAwait(false);
    }
    private async Task EventoHandler()
    {
        foreach (var (id, manager) in _torrents)
        {
            manager.PeersFound += (o, e) =>
            {
                if (Console.GetCursorPosition().Top < LinhaInicialDeLogs) Console.SetCursorPosition(0, LinhaInicialDeLogs);

                if (e.NewPeers == 0) return;
                Console.WriteLine(Localizer["Torrent_PeersEncontrados", e.GetType().Name, e.NewPeers, e.TorrentManager.Name]);
            };
            manager.PeerConnected += (o, e) =>
            {
                if (Console.GetCursorPosition().Top < LinhaInicialDeLogs) Console.SetCursorPosition(0, LinhaInicialDeLogs);

                Console.WriteLine(Localizer["Torrent_ConexaoSucesso", e.Peer.Uri]);
            };
            manager.ConnectionAttemptFailed += (o, e) =>
            {
                if (Console.GetCursorPosition().Top < LinhaInicialDeLogs) Console.SetCursorPosition(0, LinhaInicialDeLogs);

                Console.WriteLine(Localizer["Torrent_ConexaoFalha", e.Peer.ConnectionUri]);
            };
            manager.TorrentStateChanged += async (o, e) =>
            {
                if (Console.GetCursorPosition().Top < LinhaInicialDeLogs) Console.SetCursorPosition(0, LinhaInicialDeLogs);

                Console.WriteLine(Localizer["Torrent_StatusMudanca", e.TorrentManager.Name, e.NewState]);
                if (e.NewState == TorrentState.Error)
                {
                    Console.WriteLine(Localizer["Torrent_ErroInterno"]);
                    await e.TorrentManager.StopAsync().ConfigureAwait(false);
                }
                if (e.NewState == TorrentState.Seeding)
                {
                    Console.WriteLine(Localizer["Torrent_Sucesso", e.TorrentManager.Name]);
                    // Adicionar Seeding depois
                    await AbortarAsync(id).ConfigureAwait(false);
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

            // Resumo global (todos os torrents)
            AppendSeparator(SB);
            AppendFormat(SB, $" {Ciano}{_torrents.Count} torrent(s) ativo(s) | ↓ {FormatarBytes(Engine.TotalDownloadRate)}/s | ↑ {FormatarBytes(Engine.TotalUploadRate)}/s{Reset}");
            AppendSeparator(SB);

            foreach (var (id, _) in _torrents)
            {
                var p = ObterProgresso(id);
                var tempoEstimado = TempoEstimado(p, p.Percentual * 100d);

                var cor = p.Estado switch
                {
                    TorrentEstado.Concluido or TorrentEstado.Semeando => Verde,
                    TorrentEstado.Erro => Vermelho,
                    _ => Amarelo,
                };

                AppendFormat(SB, "");
                AppendFormat(SB, $"{Ciano}{Multi(2)}{p.Nome} {Reset}[{Amarelo}{id:N}{Reset}]");
                AppendFormat(SB, $" Status: {cor}{p.Estado}{Reset} | Tempo restante: {tempoEstimado} | Download: {FormatarBytes(p.VelocidadeDownload)}/s ↓ | Upload: {FormatarBytes(p.VelocidadeUpload)}/s ↑ | Peers: {p.Seeds}/{p.Peers}");
                AppendFormat(SB, $" Baixado: {FormatarBytes(p.BytesBaixados)} de {FormatarBytes(p.TamanhoTotal)}");
                BarraDeProgresso(progress, SB, p.Percentual, $"Progresso: {p.Percentual * 100:0.0}% ");
            }

            // Rodapé de ajuda
            AppendSeparator(SB);
            AppendFormat(SB, " [Q] Abortar todos | [A] Abortar por id | Ctrl+C para sair");

            Console.WriteLine(SB.ToString());

            if (Console.KeyAvailable)
            {
                var tecla = Console.ReadKey(intercept: true).Key;
                if (tecla == ConsoleKey.Q)
                {
                    Console.WriteLine(Localizer["Torrent_AbortandoTodos"]);
                    foreach (var id in _torrents.Keys.ToList())
                    {
                        await AbortarAsync(id).ConfigureAwait(false);
                    }
                    break;
                }
                else if (tecla == ConsoleKey.A)
                {
                    Console.WriteLine(Localizer["Torrent_DigiteId"]);
                    if (Guid.TryParse(Console.ReadLine(), out var idAbortar))
                    {
                        await AbortarAsync(idAbortar).ConfigureAwait(false);
                    }
                    else
                    {
                        Console.WriteLine(Localizer["Torrent_IdInvalido"]);
                    }
                }
            }

            await Task.Delay(35).ConfigureAwait(false);
        }
    }
    private static string FormatarBytes(double bytes)
    {
        return bytes switch
        {
            >= 1_073_741_824 => $"{bytes / 1_073_741_824:0.00} GB",
            >= 1_048_576 => $"{bytes / 1_048_576:0.0} MB",
            >= 1024 => $"{bytes / 1024:0} KB",
            _ => $"{bytes:0} B",
        };
    }

    private static string TempoEstimado(TorrentProgress p, double progresso)
    {
        long bytesRestantes = p.TamanhoTotal - p.BytesBaixados;
        double velocidade = p.VelocidadeDownload;
        double etaSegundos = velocidade > 0 ? bytesRestantes / velocidade : double.PositiveInfinity;

        string etaTexto;
        if (p.Estado is TorrentEstado.Semeando or TorrentEstado.Concluido || progresso >= 100.0)
        {
            etaTexto = "Concluído ";
        }
        else if (double.IsPositiveInfinity(etaSegundos))
        {
            etaTexto = p.Estado == TorrentEstado.Baixando ? "Buscando peers... " : "Parado ";
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
    [SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Um arquivo com erro não pode impedir o carregamento dos demais torrents da pasta.")]
    private async Task<List<Torrent>> CarregarTodosTorrentsDaPasta(string caminhoDaPasta)
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
                    Console.WriteLine(Localizer["Torrent_AvisoCache", Path.GetFileName(arquivo)]);
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
                    Console.WriteLine(Localizer["Torrent_FalhaCarregar", Path.GetFileName(arquivo)]);
                }
                else
                {
                    Console.WriteLine(Localizer["Torrent_ErroProcessar", Path.GetFileName(arquivo), ex.Message]);
                }
            }
        });

        await Task.WhenAll(tarefas).ConfigureAwait(false);

        return listaDeTorrents;
    }
    private async Task<List<TorrentManager>> RegistrarTorrentsEngine(ClientEngine engine, AppSettings app, string pastaDosTorrents, string pastaDeDestino)
    {
        List<Torrent> listaDeTorrents = await CarregarTodosTorrentsDaPasta(pastaDosTorrents).ConfigureAwait(false);

        TorrentSettings settingsBuilder = TorrentsConfig(app);

        var listaDeManagers = new List<TorrentManager>();

        Console.WriteLine(Localizer["Torrent_Registrando", listaDeTorrents.Count]);

        foreach (var torrent in listaDeTorrents)
        {
            try
            {
                TorrentManager manager = await engine.AddAsync(torrent, pastaDeDestino, settingsBuilder).ConfigureAwait(false);
                listaDeManagers.Add(manager);
            }
            catch (Exception ex)
            {
                Console.WriteLine(Localizer["Torrent_FalhaRegistrar", torrent.Name, ex.Message]);
            }
        }
        return listaDeManagers;
    }

    private static TorrentSettings TorrentsConfig(AppSettings app)
    {
        return new TorrentSettingsBuilder
        {
            AllowDht = true,
            AllowInitialSeeding = true,
            AllowPeerExchange = true,
            CreateContainingDirectory = true,
            MaximumConnections = app.ConnectionsMaxima,
            UploadSlots = app.UploadSlotsMaximo,
            MaximumDownloadRate = app.TorrentLimiteDownload,
            MaximumUploadRate = app.TorrentLimiteUpload
        }.ToSettings();
    }
    private TorrentProgress ObterProgresso(Guid id)
    {
        var manager = _torrents[id];

        var estado = manager.State switch
        {
            TorrentState.Error => TorrentEstado.Erro,
            TorrentState.Seeding => TorrentEstado.Semeando,
            TorrentState.Stopped or TorrentState.Paused => TorrentEstado.Pausado,
            _ when manager.Progress >= 100d => TorrentEstado.Concluido,
            _ => TorrentEstado.Baixando,
        };

        return new TorrentProgress(
            id,
            manager.Torrent?.Name ?? manager.Name ?? id.ToString(),
            manager.Progress / 100d,
            manager.Monitor.DataBytesReceived,
            manager.Torrent?.Size ?? 0,
            manager.Monitor.DownloadRate,
            manager.Monitor.UploadRate,
            manager.Peers.Seeds,
            manager.Peers.Leechs,
            estado);
    }
    private async Task AbortarAsync(Guid id)
    {
        if (_torrents.TryGetValue(id, out var manager))
        {
            await manager.StopAsync().ConfigureAwait(false);
            await Engine.RemoveAsync(manager).ConfigureAwait(false);
            _torrents.Remove(id);
        }
    }
}
