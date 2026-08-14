using Microsoft.Extensions.Localization;
using MonoTorrent;
using MonoTorrent.Client;
using MonoTorrent.Streaming;
using Spectre.Console;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;
using VideoDownloader.Constantes;
using VideoDownloader.Progress;

namespace VideoDownloader.Services.Implementation;

/// <summary>
/// Resultado do streaming: prefixo HTTP e URL completa do stream, para o Player WPF.
/// </summary>
public sealed record StreamResult(string HttpPrefix, string FullUri);
internal sealed class TorrentDownloadService(ClientEngine engine, AppSettings appContext, Log log, IStringLocalizer<TorrentDownloadService> localizer)
{
    private readonly AppSettings AppContext = appContext;
    private ClientEngine Engine { get; set; } = engine;
    private Log Log { get; set; } = log;
    private IStringLocalizer<TorrentDownloadService> Localizer { get; } = localizer;
    private string Verde { get; } = "[green]";
    private string Vermelho { get; } = "[red]";
    private string Amarelo { get; } = "[yellow]";
    private static readonly Dictionary<Guid, TorrentManager> _torrents = [];
    /// <summary>
    /// Baixa todos os arquivos torrents da pasta informada.
    /// </summary>
    /// <param name="pastaTorrents">Pasta onde estão os torrents.</param>
    /// <param name="progress">Barra de progresso.</param>
    /// <returns>Id único de um torrent.</returns>
    public async Task<Guid> BaixarAsync(string pastaTorrents, IProgress<double>? progress = null)
    {
        var id = Guid.NewGuid();
        using var cts = new CancellationTokenSource();
        CancellationToken cancellationToken = cts.Token;
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
                Log.Listar(Localizer["Torrent_NenhumEncontrado", pastaTorrents], true);
                return id;
            }

            foreach (var manager in managers)
            {
                try
                {
                    _torrents[Guid.NewGuid()] = manager;
                }
                catch
                {
                    Log.Listar($"[red]Erro ao registrar torrent: {manager.Name}[/]", true);
                }
                
            }

            await EventoHandler(cts, cancellationToken).ConfigureAwait(false);

            await MainLoop(cts, cancellationToken, progress).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Log.Listar(Localizer["Torrent_ErroAoBaixar", ex.Message], true);
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
    public async Task<Guid> BaixarAsync(MagnetLink magnet, IProgress<double>? progress = null)
    {
        var id = Guid.NewGuid();
        using var cts = new CancellationTokenSource();
        CancellationToken cancellationToken = cts.Token;

        var app = Directory.GetCurrentDirectory();
        Directory.CreateDirectory(Path.Combine(app, AppContext.PastaDownloads!));
        Directory.CreateDirectory(Path.Combine(app, AppContext.PastaCache!));
        var pastaDownload = Path.Combine(app, AppContext.PastaDownloads!);

        try
        {
            TorrentSettings settingsBuilder = TorrentsConfig(AppContext);

            var manager = await Engine.AddAsync(magnet, pastaDownload, settingsBuilder).ConfigureAwait(false);

            _torrents[id] = manager;

            await EventoHandler(cts, cancellationToken).ConfigureAwait(false);

            await MainLoop(cts, cancellationToken, progress).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Log.Listar(Localizer["Torrent_ErroAoBaixar", ex.Message], true);
            throw;
        }

        return id;
    }
    /// <summary>
    /// Inicia o streaming de um magnet link, abrindo o Player WPF e monitorando os eventos
    /// do manager até o Player ser fechado.
    /// </summary>
    /// <param name="magnet">Url magnética.</param>
    /// <param name="progress">Barra de progresso.</param>
    /// <returns>Resultado do streaming (HttpPrefix + FullUri).</returns>
    public async Task<StreamResult> StreamAsync(MagnetLink magnet, IProgress<double>? progress = null)
    {
        var id = Guid.NewGuid();
        using var cts = new CancellationTokenSource();
        CancellationToken cancellationToken = cts.Token;

        var pastaDownload = Path.Combine(Directory.GetCurrentDirectory(), AppContext.PastaDownloads ?? "Downloads");
        Directory.CreateDirectory(pastaDownload);

        MonoTorrent.Streaming.IHttpStream? stream = null;
        System.Diagnostics.Process? player = null;

        try
        {
            TorrentSettings settingsBuilder = TorrentsConfig(AppContext);
            var manager = await Engine.AddStreamingAsync(magnet, pastaDownload, settingsBuilder).ConfigureAwait(false);

            if (manager == null)
            {
                Log.Listar(Localizer["Torrent_ErroAoBaixar"], true);
                throw new InvalidOperationException("Falha ao criar o gerenciador de streaming.");
            }

            _torrents[id] = manager;

            await EventoHandler(cts, cancellationToken).ConfigureAwait(false);

            var maiorArquivo = manager.Files.OrderBy(t => t.Length).Last();

            stream = await manager.StreamProvider!.CreateHttpStreamAsync(maiorArquivo, true, cancellationToken).ConfigureAwait(false);

            // Buffer inicial para evitar travamentos de reprodução
            await StreamBuffer(manager, cancellationToken).ConfigureAwait(false);

            player = await LoopPlayer(stream, cancellationToken).ConfigureAwait(false);

            return new StreamResult(stream.HttpPrefix, stream.FullUri);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            Log.Listar(Localizer["Torrent_ErroAoBaixar", ex.Message], true);
            throw;
        }
        finally
        {
            // Quando o player fechar (ou ocorrer um erro), encerra o servidor HTTP com segurança
            if (stream != null)
            {
                Log.Listar("[red]Fechando o servidor de streaming HTTP local...[/]", true);
                stream.Dispose();
            }

            player?.Dispose();
        }
    }
    private async Task EventoHandler(CancellationTokenSource cts, CancellationToken token)
    {
        foreach (var (id, manager) in _torrents)
        {
            string nomeEscapado = Markup.Escape(manager.Name);
            manager.PeersFound += (o, e) =>
            {
                if (e.NewPeers == 0) return;
                string peers = $"[cyan]{e.NewPeers}[/]";
                Log.Listar(Localizer["Torrent_PeersEncontrados", e.GetType().Name, peers, nomeEscapado]);
            };
            manager.PeerConnected += (o, e) =>
            {
                Log.Listar(Localizer["Torrent_ConexaoSucesso", Markup.Escape(e.Peer.Uri.ToString())]);
            };
            manager.ConnectionAttemptFailed += (o, e) =>
            {
                Log.Listar(Localizer["Torrent_ConexaoFalha", Markup.Escape(e.Peer.ConnectionUri.ToString())]);
            };
            manager.TorrentStateChanged += async (o, e) =>
            {
                Log.Listar(Localizer["Torrent_StatusMudanca", nomeEscapado, e.NewState]);
                if (e.NewState == TorrentState.Error)
                {
                    Log.Listar(Localizer["Torrent_ErroInterno"]);
                    await e.TorrentManager.StopAsync().ConfigureAwait(false);
                }
                if (e.NewState == TorrentState.Seeding)
                {
                    Log.Listar(Localizer["Torrent_Sucesso", nomeEscapado]);
                    if (!AppContext.TorrentSemear)
                    {
                        await e.TorrentManager.StopAsync().ConfigureAwait(false);
                    }
                }
            };

            await manager.StartAsync().ConfigureAwait(false);
            await manager.DhtAnnounceAsync().ConfigureAwait(false);
            await manager.LocalPeerAnnounceAsync().ConfigureAwait(false);
        }
    }
    private async Task MainLoop(CancellationTokenSource cts, CancellationToken token, IProgress<double>? progress)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            Console.WindowWidth = 110;
            Console.WindowHeight = 25;
            Console.BufferWidth = 110;
        }
        else
        {
            //fallback para Linux / macOS no futuro - talvez...
        }

        Console.CancelKeyPress += (sender, eventArgs) =>
        {
            eventArgs.Cancel = true;
            Log.Listar("Ctrl+C pressionado, encerrando...", true);
            cts.Cancel();
        };

        Console.CursorVisible = false;
        Console.Clear();

        while (Engine.IsRunning && !token.IsCancellationRequested)
        {
            await AtualizarFilaDeDownloadsAsync().ConfigureAwait(false);

            // Se o seeding está desabilitado e não há mais torrents ativos (todos parados/erro),
            // encerra o loop — nada mais a fazer.
            if (!AppContext.TorrentSemear && _torrents.Count > 0 &&
                _torrents.Values.All(m => m.State is TorrentState.Stopped or TorrentState.Error))
            {
                Log.Listar("Sem torrent ativo ou semeando, encerrando...", true);
                break;
            }

            Log.Limpar();

            string headerFormat = $" [cyan]{_torrents.Count} torrent(s) ativo(s) | ↓ {FormatarBytes(Engine.TotalDownloadRate)}/s | ↑ {FormatarBytes(Engine.TotalUploadRate)}/s[/]".PadRight(110);

            // CLI Torrents
            Log.Adicionar($"[cyan]{Log.Multi(110, '=')}[/]");
            Log.Adicionar(headerFormat);
            Log.Adicionar("");
            Log.Adicionar("  [[Q]] Abortar todos | [[A]] Abortar por id | Ctrl+C para sair");
            Log.Adicionar(" >: ");
            Log.Adicionar($"[cyan]{Log.Multi(110, '=')}[/]");
            foreach (var (id, _) in _torrents)
            {
                var p = ObterProgresso(id);
                var tempoEstimado = TempoEstimado(p, p.Percentual * 100d);
                string nomeEscape = Spectre.Console.Markup.Escape(p.Nome);

                var cor = p.Estado switch
                {
                    TorrentEstado.Concluido or TorrentEstado.Semeando => Verde,
                    TorrentEstado.Erro => Vermelho,
                    _ => Amarelo,
                };
                string downloadFormat = $" Download: {FormatarBytes(p.VelocidadeDownload)}/s ↓ | Upload: {FormatarBytes(p.VelocidadeUpload)}/s ↑ | Baixado: {FormatarBytes(p.BytesBaixados)} de {FormatarBytes(p.TamanhoTotal)}".PadRight(110);
                string statusFormat = $" Status: {cor}{p.Estado}[/] | Tempo restante: {tempoEstimado} | Peers: {p.Seeds}/{p.Peers}".PadRight(110);

                Log.Adicionar("");
                Log.Adicionar($" Arquivo: [cyan]{nomeEscape}[/] [[[yellow]{id:N}[/]]]");
                Log.Adicionar(downloadFormat);
                Log.Adicionar(statusFormat);
                BarraDeProgresso(progress, Log.SB, p.Percentual, $"Progresso - {p.Percentual * 100:0.0}%");
            }
            // CLI Logs
            Log.Imprimir();

            if (Console.KeyAvailable)
            {
                var tecla = Console.ReadKey(intercept: true).Key;
                if (tecla == ConsoleKey.Q)
                {
                    cts.Cancel();
                    Log.Listar($"[yellow]{Localizer["Torrent_AbortandoTodos"]}[/]", true);

                    foreach (var id in _torrents.Keys.ToList())
                    {
                        await AbortarAsync(id).ConfigureAwait(false);
                    }
                    break;
                }
                else if (tecla == ConsoleKey.A)
                {
                    // Move o cursor para uma linha segura abaixo do painel de logs para não quebrar o layout
                    //var linhaInput = 5;
                    //Console.SetCursorPosition(5, linhaInput);

                    Log.Listar($"[yellow]{Localizer["Torrent_DigiteId"]}: [/]");

                    Console.CursorVisible = true;
                    string? input = Console.ReadLine();
                    Console.CursorVisible = false;

                    if (Guid.TryParse(input, out var idAbortar))
                    {
                        await AbortarAsync(idAbortar).ConfigureAwait(false);
                    }
                    else
                    {
                        Log.Listar($"[red]{Localizer["Torrent_IdInvalido"]}[/]");
                    }

                    // Força um Clear real apenas após a leitura do input para limpar a linha escrita pelo usuário
                    Console.Clear();
                    Log.CliAtual = string.Empty;
                }
            }

            try
            {
                await Task.Delay(1000, token).ConfigureAwait(false);
            }
            catch (TaskCanceledException)
            {
                break;
            }
        }
    }
    private async Task<Process?> LoopPlayer(IHttpStream? stream, CancellationToken cancellationToken)
    {
        Process? player;
        var playerExe = Path.Combine(System.AppContext.BaseDirectory, "VideoDownloader.Player.exe");
        if (!File.Exists(playerExe))
        {
            playerExe = Path.Combine(Directory.GetCurrentDirectory(), "VideoDownloader.Player.exe");
        }

        if (!File.Exists(playerExe))
        {
            Log.Listar($"[red]{Localizer["Torrent_PlayerNaoEncontrado"]}[/]", true);
            throw new FileNotFoundException($"Player não encontrado: {playerExe}");
        }

        // Inicia o player WPF passando a URL do servidor HTTP local do MonoTorrent
        player = Process.Start(new ProcessStartInfo
        {
            FileName = playerExe,
            Arguments = $"\"{stream.FullUri}\"",
            UseShellExecute = true,
        });

        // O loop mantém o método vivo enquanto o player assiste ao vídeo
        if (player is not null)
        {
            int? ultimoSeed = null;
            while (!player.HasExited)
            {
                Log.Limpar();
                cancellationToken.ThrowIfCancellationRequested();
                
                foreach (var (_, nome, estado, seeds, peers) in EstadoDosTorrents())
                {
                    if (ultimoSeed == null || seeds != ultimoSeed)
                    {
                        string nomeEscape = Markup.Escape(nome);
                        Log.Listar($"[[{nomeEscape}]] [cyan]{estado}[/] | seeds: {seeds} | peers: {peers}");
                        ultimoSeed = seeds;
                    }
                    Log.Imprimir();
                }
                await Task.Delay(100, cancellationToken).ConfigureAwait(false);
            }
        }

        return player;
    }

    private static async Task StreamBuffer(TorrentManager manager, CancellationToken cancellationToken)
    {
        double buffer = 5.0; // Porcentagem baixada para o buffer inicial

        // verifica se o player retomou algum torrent baixado com porcentagem maior que o buffer
        double progresso = manager.Bitfield.PercentComplete;
        if (progresso >= buffer) return;
        while (true)
        {            
            cancellationToken.ThrowIfCancellationRequested();

            double progressoTorrent = manager.Bitfield.PercentComplete;
            Log.Listar($"[yellow]Enchendo Buffer Inicial:[/] {progressoTorrent:0.0}% / {buffer}% | Seeds: {manager.Peers.Seeds}", true);

            if (progressoTorrent >= buffer)
            {
                Log.Listar("[cyan]Buffer inicial concluído![/]", true);
                break;
            }
            await Task.Delay(1000, cancellationToken).ConfigureAwait(false);
        }
    }
    private static async Task AtualizarFilaDeDownloadsAsync()
    {
        const int LimiteMaximoDownloadsAtivos = 10;

        int baixandoAtualmente = _torrents.Values.Count(m => m.State == TorrentState.Downloading);

        if (baixandoAtualmente < LimiteMaximoDownloadsAtivos)
        {
            // Pega os próximos torrents da lista que estão parados esperando
            var proximosDaFila = _torrents.Values
                .Where(m => m.State == TorrentState.Stopped && m.Progress < 1.0)
                .Take(LimiteMaximoDownloadsAtivos - baixandoAtualmente);

            foreach (var manager in proximosDaFila)
            {
                await manager.StartAsync().ConfigureAwait(false);
                Log.Listar($"[green][Fila][/] Iniciando download agendado de: {manager.Name}");
            }
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
                    Log.Listar(Localizer["Torrent_AvisoCache", Path.GetFileName(arquivo)], true);
                }
                else
                {
                    lock (listaDeTorrents)
                    {
                        listaDeTorrents.Add(torrent);
                        Log.Listar($"Torrent carregado: {Path.GetFileName(arquivo)}");
                    }
                }
            }
            catch (Exception ex)
            {
                if (ex.Message.Contains("torrent", StringComparison.OrdinalIgnoreCase))
                {
                    Log.Listar(Localizer["Torrent_FalhaCarregar", Path.GetFileName(arquivo)], true);
                }
                else
                {
                    Log.Listar(Localizer["Torrent_ErroProcessar", Path.GetFileName(arquivo), ex.Message], true);
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

        Log.Listar(Localizer["Torrent_Registrando", listaDeTorrents.Count]);

        foreach (var torrent in listaDeTorrents)
        {
            try
            {
                TorrentManager manager = await engine.AddAsync(torrent, pastaDeDestino, settingsBuilder).ConfigureAwait(false);
                listaDeManagers.Add(manager);
            }
            catch (Exception ex)
            {
                Log.Listar(Localizer["Torrent_FalhaRegistrar", torrent.Name, ex.Message], true);
            }
        }
        return listaDeManagers;
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
                temp.Report(progresso, sb);
            }
            Log.Adicionar("");
        }
        else if (progress is ConsoleDownloadProgressBar consoleBar)
        {
            consoleBar.Report(progresso, sb);
            Log.Adicionar("");
        }
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
    private static TorrentProgress ObterProgresso(Guid id)
    {
        var manager = _torrents[id];

        var estado = manager.State switch
        {
            TorrentState.Error => TorrentEstado.Erro,
            TorrentState.Seeding => TorrentEstado.Semeando,
            TorrentState.Stopped => TorrentEstado.Parado,
            TorrentState.Downloading => TorrentEstado.Baixando,
            TorrentState.Paused => TorrentEstado.Pausado,
            TorrentState.FetchingHashes => TorrentEstado.BuscandoHashs,
            TorrentState.Hashing => TorrentEstado.VerificandoHash,
            TorrentState.HashingPaused => TorrentEstado.HashPausado,
            TorrentState.Starting => TorrentEstado.Iniciando,
            TorrentState.Stopping => TorrentEstado.Parando,
            TorrentState.Metadata => TorrentEstado.Metadata,
            _ when manager.Progress >= 100d => TorrentEstado.Concluido,
            _ => TorrentEstado.Erro,
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
            manager.Peers.Available,
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
    /// <summary>
    /// Snapshot do estado atual dos torrents ativos (para o CLI mostrar peers/estado no streaming).
    /// </summary>
    public IReadOnlyList<(Guid Id, string Nome, TorrentEstado Estado, int Seeds, int Peers)> EstadoDosTorrents()
    {
        return [.. _torrents.Select(p => (
        p.Key,
        p.Value.Torrent?.Name ?? p.Value.Name ?? "?",
        ConverterEstado(p.Value),
        p.Value.Peers.Seeds,
        p.Value.Peers.Available))];
    }
    private static TorrentEstado ConverterEstado(TorrentManager manager)
    {
        return manager.State switch
        {
            TorrentState.Error => TorrentEstado.Erro,
            TorrentState.Seeding => TorrentEstado.Semeando,
            TorrentState.Stopped => TorrentEstado.Parado,
            TorrentState.Downloading => TorrentEstado.Baixando,
            TorrentState.Paused => TorrentEstado.Pausado,
            TorrentState.FetchingHashes => TorrentEstado.BuscandoHashs,
            TorrentState.Hashing => TorrentEstado.VerificandoHash,
            TorrentState.HashingPaused => TorrentEstado.HashPausado,
            TorrentState.Starting => TorrentEstado.Iniciando,
            TorrentState.Stopping => TorrentEstado.Parando,
            TorrentState.Metadata => TorrentEstado.Metadata,
            _ when manager.Progress >= 100d => TorrentEstado.Concluido,
            _ => TorrentEstado.Erro,
        };
    }
}