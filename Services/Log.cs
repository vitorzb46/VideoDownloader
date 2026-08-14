using Spectre.Console;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text;

namespace VideoDownloader.Services;

[SuppressMessage("Design", "CA1515:Avoid uninstantiated internal classes")]
public class Log
{
    private static readonly ConcurrentQueue<string> HistoricoDeLogs = new();
    private static readonly string LogPath = Path.Combine(AppContext.BaseDirectory, "player-debug.log");
    private static string? Mensagem { get; set; }
    public static string CliAtual { get; set; } = string.Empty;
    private static int MaxLogsNaTela { get; set; } = 15;
    private static bool ExibirArquivoLog { get; set; }
    public static StringBuilder SB { get; set; } = new();
    public static StringBuilder SBLog { get; set; } = new();
    public static void Limpar() => SB.Clear();
    /// <summary>
    /// Adiciona uma mensagem ao CLI estático.
    /// </summary>
    /// <param name="mensagem">Mensagem a ser adicionada ao CLI.</param>
    public static void Adicionar(string mensagem)
    {
        SB.AppendLine(mensagem?.PadRight(110));
    }
    /// <summary>
    /// Adiciona uma mensagem ao histórico de logs e, se <paramref name="salvarLog"/> for<c>true,</c>salva o log em <see cref="player-debug.log"/>.
    /// </summary>
    /// <param name="mensagem">Mensagem a ser adicionada ao log.</param>
    /// <param name="salvarLog">Se <c>true</c>, salva o log.</param>
    public static void Listar(string mensagem) => Listar(mensagem, false);
    public static void Listar(string mensagem, bool salvarLog)
    {
        Mensagem = mensagem;
        HistoricoDeLogs.Enqueue(mensagem);
        if (salvarLog is true)
        {
            ExibirArquivoLog = true;
            Salvar();
        }
        while (HistoricoDeLogs.Count > MaxLogsNaTela)
        {
            HistoricoDeLogs.TryDequeue(out _);
        }
    }
    /// <summary>
    /// Imprime no console as últimas linhas do histórico de logs. <see cref="MaxLogsNaTela"/>.
    /// </summary>
    public static void Imprimir()
    {
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

        string cli = SB.ToString();
        if (cli != CliAtual)
        {
            Console.SetCursorPosition(0, 0);
            // Apaga rastros se a string encolheu (ex: se um torrent foi removido)
            if (cli.Length < CliAtual.Length)
            {
                int diferenca = CliAtual.Length - cli.Length;
                cli += new string(' ', diferenca);
            }
            try
            {
                AnsiConsole.MarkupLine(cli);
            }
            catch
            {
                Console.WriteLine(cli);
            }

            CliAtual = SB.ToString();
        }
    }
    private static void Salvar()
    {
        if (ExibirArquivoLog)
        {
            try
            {
                File.AppendAllText(LogPath, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] VM: {Mensagem}{Environment.NewLine}");
            }
            catch
            {
                SB.AppendLine(CultureInfo.InvariantCulture, $"Erro ao salvar string em log: {Mensagem}");
            }
            Imprimir();
            ExibirArquivoLog = false;
        }
    }
    public static string Multi(int vezes = 0, char c = '\t')
    {
        return $"{new string(c, vezes)}";
    }
}