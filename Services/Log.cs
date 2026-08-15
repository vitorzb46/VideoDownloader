using Spectre.Console;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace VideoDownloader.Services;

[SuppressMessage("Design", "CA1515:Avoid uninstantiated internal classes")]
public class Log
{
    private static readonly ConcurrentQueue<string> HistoricoDeLogs = new();
    private static readonly string LogPath = Path.Combine(AppContext.BaseDirectory, "player-debug.log");
    private static readonly object SyncRoot = new();
    private static DateTime UltimaRenderizacao = DateTime.MinValue;
    private static readonly TimeSpan IntervaloMinimo = TimeSpan.FromMilliseconds(250);
    public static string CliAtual { get; set; } = string.Empty;
    private static int MaxLogsNaTela { get; set; } = 10;
    public static StringBuilder SB { get; set; } = new();
    public static void Limpar() => SB.Clear();
    /// <summary>
    /// Adiciona uma mensagem ao CLI estático.
    /// </summary>
    /// <param name="mensagem">Mensagem a ser adicionada ao CLI.</param>
    public static void Adicionar(string mensagem)
    {
        SB.AppendLine(mensagem);
    }
    /// <summary>
    /// Adiciona uma mensagem ao histórico de logs.
    /// </summary>
    /// <param name="mensagem">Mensagem a ser adicionada ao log.</param>
    public static void Listar(string mensagem)
    {
        HistoricoDeLogs.Enqueue(mensagem);
        while (HistoricoDeLogs.Count > MaxLogsNaTela)
        {
            HistoricoDeLogs.TryDequeue(out _);
        }
    }
    /// <summary>
    /// Imprime no console o painel estático + as últimas linhas do histórico de logs.
    /// Serializado por lock e com coalescência de ticks — os eventos do MonoTorrent disparam
    /// centenas de vezes por segundo em threads diferentes; renderizar a cada evento
    /// embaralha o cursor e faz a tela rolar.
    /// </summary>
    public static void Imprimir()
    {
        // Coalesce: se a última renderização foi há menos que o intervalo, ignora
        // (os logs já foram enfileirados e serão exibidos na próxima passagem).
        lock (SyncRoot)
        {
            var agora = DateTime.UtcNow;
            if (agora - UltimaRenderizacao < IntervaloMinimo)
            {
                return;
            }
            UltimaRenderizacao = agora;

            // Preserva o painel estático montado pelo MainLoop (Adicionar).
            string painel = SB.ToString();

            var sb = new StringBuilder();
            sb.Append(painel);
            sb.Append($"[cyan]{Multi(110, '-')}[/]");
            sb.AppendLine();
            sb.Append($"{Multi(30, ' ')}[cyan]=== ÚLTIMOS LOGS DO SISTEMA ===[/]".PadRight(110));
            sb.AppendLine();

            var exibirLog = HistoricoDeLogs.ToArray().Reverse();
            foreach (var log in exibirLog)
            {
                sb.Append($" {log}".PadRight(110));
                sb.AppendLine();
            }

            int linhasVazias = MaxLogsNaTela - HistoricoDeLogs.Count;
            for (int i = 0; i < linhasVazias; i++)
            {
                sb.AppendLine(new string(' ', 110));
            }

            string cli = sb.ToString();
            if (cli != CliAtual)
            {
                // Apaga rastros se a string encolheu (ex: se um torrent foi removido)
                if (cli.Length < CliAtual.Length)
                {
                    int diferenca = CliAtual.Length - cli.Length;
                    cli += new string(' ', diferenca);
                }

                try
                {
                    // Volta o cursor ao topo e sobrescreve — evita rolar a tela/duplicar.
                    Console.SetCursorPosition(0, 0);
                    AnsiConsole.Markup(cli);
                }
                catch
                {
                    Console.SetCursorPosition(0, 0);
                    Console.WriteLine(Markup.Escape(cli));
                }

                CliAtual = cli;
            }
        }
    }
    public static void Salvar(string mensagem)
    {
        try
        {
            File.AppendAllText(LogPath, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] {mensagem}{Environment.NewLine}");
        }
        catch
        {
            // Ignora falhas de escrita em log (best-effort).
        }
    }
    public static string Multi(int vezes = 0, char c = '\t')
    {
        return $"{new string(c, vezes)}";
    }
}