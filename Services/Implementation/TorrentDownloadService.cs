using LibVLCSharp.Shared;
using Microsoft.Extensions.Localization;
using MonoTorrent;
using MonoTorrent.Client;
using MonoTorrent.Streaming;
using Spectre.Console;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;
using VideoDownloader.Constantes;
using VideoDownloader.Progress;

namespace VideoDownloader.Services.Implementation;

[SuppressMessage("Performance", "CA1812:Avoid uninstantiated internal classes")]
internal sealed class TorrentDownloadService(ClientEngine engine, AppSettings appContext, IStringLocalizer<TorrentDownloadService> localizer)
{
    private readonly AppSettings AppContext = appContext;
    private ClientEngine Engine { get; set; } = engine;
    private readonly ConcurrentQueue<string> HistoricoDeLogs = new();
    private StringBuilder SB { get; } = new(1024);
    private static int MaxLogsNaTela { get; } = 5;
    private static int LinhaInicialDeLogs { get; set; }
    private IStringLocalizer<TorrentDownloadService> Localizer { get; } = localizer;
    private string Verde { get; } = "[green]";
    private string Vermelho { get; } = "[red]";
    private string Amarelo { get; } = "[yellow]";
    private readonly Dictionary<Guid, TorrentManager> _torrents = [];
    private string CliAtual { get; set; } = string.Empty;
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
                Console.WriteLine(Localizer["Torrent_NenhumEncontrado", pastaTorrents]);
                return id;
            }

            foreach (var manager in managers)
            {
                _torrents[Guid.NewGuid()] = manager;
            }

            await EventoHandler(cts, cancellationToken).ConfigureAwait(false);

            await MainLoop(cts, cancellationToken, progress).ConfigureAwait(false);
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
            Console.WriteLine(Localizer["Torrent_ErroAoBaixar", ex.Message]);
            throw;
        }

        return id;
    }
    /// <summary>
    /// Inicia o streaming de um magnet link, retornando uma Stream nativa do .NET
    /// para assistir antes do download terminar
    /// </summary>
    /// <param name="magnet">Url magnética.</param>
    /// <param name="token">Token de cancelamento.</param>
    /// <returns>Stream do primeiro arquivo do torrent.</returns>
    public async Task<IHttpStream> StreamAsync(MagnetLink magnet, IProgress<double>? progress = null)
    {
        var id = Guid.NewGuid();
        using var cts = new CancellationTokenSource();
        CancellationToken cancellationToken = cts.Token;

        var pastaDownload = Path.Combine(Directory.GetCurrentDirectory(), AppContext.PastaDownloads ?? "Downloads");
        Directory.CreateDirectory(pastaDownload);

        try
        {
            TorrentSettings settingsBuilder = TorrentsConfig(AppContext);
            var manager = await Engine.AddStreamingAsync(magnet, pastaDownload, settingsBuilder).ConfigureAwait(false);

            if (manager == null) return null!;

            _torrents[id] = manager;

            await EventoHandler(cts, cancellationToken).ConfigureAwait(false);

            // Configurações de Stream e VLC
            (IHttpStream stream, LibVLC libVLC, MediaPlayer mediaPlayer, Media media) = await VLC(manager, cancellationToken).ConfigureAwait(false);

            // Gerenciador do ciclo de vida de execução
            using CancellationTokenRegistration disposer = await CicloStreaming(progress, stream, mediaPlayer, cts, cancellationToken).ConfigureAwait(false);

            return stream;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            Console.WriteLine(Localizer["Torrent_ErroAoBaixar", ex.Message]);
            throw;
        }

    }
    private async Task EventoHandler(CancellationTokenSource cts, CancellationToken token)
    {
        foreach (var (id, manager) in _torrents)
        {
            string nomeEscapado = Markup.Escape(manager.Name);
            manager.PeersFound += (o, e) =>
            {
                if (Console.GetCursorPosition().Top < LinhaInicialDeLogs) Console.SetCursorPosition(0, LinhaInicialDeLogs);

                if (e.NewPeers == 0) return;
                string peers = $"[cyan]{e.NewPeers}[/]";
                AdicionarLog(Localizer["Torrent_PeersEncontrados", e.GetType().Name, peers, nomeEscapado]);
            };
            manager.PeerConnected += (o, e) =>
            {
                if (Console.GetCursorPosition().Top < LinhaInicialDeLogs) Console.SetCursorPosition(0, LinhaInicialDeLogs);

                AdicionarLog(Localizer["Torrent_ConexaoSucesso", e.Peer.Uri]);
            };
            manager.ConnectionAttemptFailed += (o, e) =>
            {
                if (Console.GetCursorPosition().Top < LinhaInicialDeLogs) Console.SetCursorPosition(0, LinhaInicialDeLogs);

                AdicionarLog(Localizer["Torrent_ConexaoFalha", e.Peer.ConnectionUri]);
            };
            manager.TorrentStateChanged += async (o, e) =>
            {
                if (Console.GetCursorPosition().Top < LinhaInicialDeLogs) Console.SetCursorPosition(0, LinhaInicialDeLogs);

                AdicionarLog(Localizer["Torrent_StatusMudanca", nomeEscapado, e.NewState]);
                if (e.NewState == TorrentState.Error)
                {
                    AdicionarLog(Localizer["Torrent_ErroInterno"]);
                    await e.TorrentManager.StopAsync().ConfigureAwait(false);
                }
                if (e.NewState == TorrentState.Seeding)
                {
                    AdicionarLog(Localizer["Torrent_Sucesso", nomeEscapado]);
                    // Seeding controlado por config: se TorrentSemear=false, para ao concluir;
                    // se true, mantém o manager semeando para ajudar a comunidade.
                    if (!AppContext.TorrentSemear)
                    {
                        await e.TorrentManager.StopAsync().ConfigureAwait(false);
                    }
                }
            };
            await manager.StartAsync().ConfigureAwait(false);
            if (manager.HasMetadata) Console.WriteLine($"{manager.Name} - aguardando metadados!");
            await manager.WaitForMetadataAsync(token).ConfigureAwait(false);
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
            Console.WriteLine("Ctrl+C pressionado, encerrando...");
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
                break;
            }

            SB.Clear();

            string headerFormat = $" [cyan]{_torrents.Count} torrent(s) ativo(s) | ↓ {FormatarBytes(Engine.TotalDownloadRate)}/s | ↑ {FormatarBytes(Engine.TotalUploadRate)}/s[/]".PadRight(110);

            // CLI Torrents
            SB.AppendLine(CultureInfo.InvariantCulture, $"[cyan]{Multi(110, '=')}[/]");
            SB.AppendLine(headerFormat);
            SB.AppendLine();
            SB.AppendLine("  [[Q]] Abortar todos | [[A]] Abortar por id | Ctrl+C para sair");
            SB.AppendLine(" >: ");
            SB.AppendLine(CultureInfo.InvariantCulture, $"[cyan]{Multi(110, '=')}[/]");
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

                SB.AppendLine("");
                SB.AppendLine(CultureInfo.InvariantCulture, $" Arquivo: [cyan]{nomeEscape}[/] [[[yellow]{id:N}[/]]]");
                SB.AppendLine(downloadFormat);
                SB.AppendLine(statusFormat);
                BarraDeProgresso(progress, SB, p.Percentual, $"Progresso - {p.Percentual * 100:0.0}%");
            }
            // CLI Logs
            SB.AppendLine(CultureInfo.InvariantCulture, $"[cyan]{Multi(110, '-')}[/]");
            SB.AppendLine(CultureInfo.InvariantCulture, $"{Multi(30, ' ')}[cyan]=== ÚLTIMOS LOGS DO SISTEMA ===[/]");
            var exibirLog = HistoricoDeLogs.ToArray().Reverse();
            foreach (var log in exibirLog)
            {
                string logFormat = $" {log}".PadRight(110);
                SB.AppendLine(logFormat);
            }

            int linhasVazias = MaxLogsNaTela - HistoricoDeLogs.Count;
            for (int i = 0; i < linhasVazias; i++)
            {
                SB.AppendLine(new string(' ', 110));
            }

            string cliAtual = SB.ToString();
            if (cliAtual != CliAtual)
            {
                Console.SetCursorPosition(0, 0);
                // Apaga rastros se a string encolheu (ex: se um torrent foi removido)
                if (cliAtual.Length < CliAtual.Length)
                {
                    int diferenca = CliAtual.Length - cliAtual.Length;
                    cliAtual += new string(' ', diferenca);
                }

                AnsiConsole.Markup(cliAtual);
                CliAtual = SB.ToString();
            }

            if (Console.KeyAvailable)
            {
                var tecla = Console.ReadKey(intercept: true).Key;
                if (tecla == ConsoleKey.Q)
                {
                    cts.Cancel();
                    Console.Clear();
                    AnsiConsole.MarkupLine($"[yellow]{Localizer["Torrent_AbortandoTodos"]}[/]");

                    foreach (var id in _torrents.Keys.ToList())
                    {
                        await AbortarAsync(id).ConfigureAwait(false);
                    }
                    break;
                }
                else if (tecla == ConsoleKey.A)
                {
                    // Move o cursor para uma linha segura abaixo do painel de logs para não quebrar o layout
                    var linhaInput = 5;
                    Console.SetCursorPosition(5, linhaInput);

                    AnsiConsole.Markup($"[yellow]{Localizer["Torrent_DigiteId"]}: [/]");

                    Console.CursorVisible = true;
                    string? input = Console.ReadLine();
                    Console.CursorVisible = false;

                    if (Guid.TryParse(input, out var idAbortar))
                    {
                        await AbortarAsync(idAbortar).ConfigureAwait(false);
                    }
                    else
                    {
                        AdicionarLog($"[red]{Localizer["Torrent_IdInvalido"]}[/]");
                    }

                    // Força um Clear real apenas após a leitura do input para limpar a linha escrita pelo usuário
                    Console.Clear();
                    CliAtual = string.Empty;
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
    private async Task AtualizarFilaDeDownloadsAsync()
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
                AdicionarLog($"[green][Fila][/] Iniciando download agendado de: {manager.Name}");
            }
        }
    }
    private async Task<CancellationTokenRegistration> CicloStreaming(IProgress<double>? progress, IHttpStream stream, MediaPlayer mediaPlayer, CancellationTokenSource cts, CancellationToken cancellationToken)
    {
        var tcs = new TaskCompletionSource<bool>();
        var disposer = cancellationToken.Register(() =>
        {
            mediaPlayer.Stop();
            stream.Dispose();
            tcs.TrySetResult(true);
        });

        mediaPlayer.EndReached += (o, e) =>
        {
            stream.Dispose();
            tcs.TrySetResult(true);
        };

        mediaPlayer.EncounteredError += (o, e) =>
        {
            stream.Dispose();
            tcs.TrySetResult(false);
        };

        while (!tcs.Task.IsCompleted && !cancellationToken.IsCancellationRequested)
        {
            await MainLoop(cts, cancellationToken, progress).ConfigureAwait(false);
        }

        await tcs.Task.ConfigureAwait(false);

        cancellationToken.ThrowIfCancellationRequested();
        return disposer;
    }

    private static async Task<(IHttpStream stream, LibVLC libVLC, MediaPlayer mediaPlayer, Media media)> VLC(TorrentManager manager, CancellationToken cancellationToken)
    {
        var maiorArquivo = manager.Files.OrderBy(t => t.Length).Last();

        var stream = await manager.StreamProvider!.CreateHttpStreamAsync(maiorArquivo, true, cancellationToken).ConfigureAwait(false);

        string routableAddress = stream.FullUri;

        Core.Initialize();

        var opcoesGlobais = new string[]
        {
                "--no-metadata-network-lookup",
                "--no-embedded-video",
                "--no-video-decorations",
                "--fullscreen",
                $"--video-title=Assistindo: {manager.Name}"
        };
        using var libVLC = new LibVLC(opcoesGlobais);
        using var mediaPlayer = new MediaPlayer(libVLC);

        using var media = new Media(libVLC, routableAddress, FromType.FromLocation);

        media.AddOption(":network-caching=2000");
        media.AddOption(":clock-synchro=0");
        media.AddOption(":clock-jitter=5000");
        media.AddOption(":audio-language=por,eng");
        media.AddOption(":sub-language=por");
        media.AddOption(":sub-margin=50");
        media.AddOption(":freetype-rel-fontsize=16");
        media.AddOption(":freetype-color=16777215");

        mediaPlayer.Play(media);
        return (stream, libVLC, mediaPlayer, media);
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
            sb.AppendLine();
        }
        else if (progress is ConsoleDownloadProgressBar consoleBar)
        {
            consoleBar.Report(progresso, sb);
            sb.AppendLine();
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
    private void AdicionarLog(string mensagem)
    {
        HistoricoDeLogs.Enqueue(mensagem);

        while (HistoricoDeLogs.Count > MaxLogsNaTela)
        {
            HistoricoDeLogs.TryDequeue(out _);
        }
    }
    private static string Multi(int vezes = 0, char c = '\t')
    {
        return $"{new string(c, vezes)}";
    }
}
