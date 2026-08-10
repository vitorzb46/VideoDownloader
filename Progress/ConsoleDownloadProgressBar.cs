using System.Globalization;
using System.Text;
using Spectre.Console;

namespace VideoDownloader.Progress;

internal sealed class ConsoleDownloadProgressBar : IProgress<double>, IDisposable
{
    private readonly string _title;
    private readonly int _width;
    private readonly object _sync = new();
    private int _lastPercent = -1;
    private bool _disposed;
    /// <summary>
    /// Classe responsável por renderizar a barra de progresso de download no console.
    /// </summary>
    public ConsoleDownloadProgressBar(string title = "Download", int width = 30)
    {
        _title = string.IsNullOrWhiteSpace(title) ? "Download" : title.Trim();
        _width = Math.Max(10, width);
    }
    // Satisfaz a interface IProgress
    public void Report(double value)
    {
        Report(value, null);
    }

    public void Report(double value, StringBuilder? sb)
    {
        if (_disposed) return;

        var percentage = Math.Clamp(value, 0d, 1d) * 100d;
        var rounded = (int)Math.Round(percentage);

        lock (_sync)
        {
            if (sb == null)
            {
                if (rounded == _lastPercent) return;

                _lastPercent = rounded;
                Render(rounded, null);
                return;
            }

            if (rounded == _lastPercent) return;

            _lastPercent = rounded;
            Render(rounded, sb);
        }
    }
    private void Render(int percentage, StringBuilder? sb)
    {
        var preenchido = (int)Math.Round(_width * (percentage / 100d));
        var vazio = _width - preenchido;

        if (sb == null)
        {

            AnsiConsole.WriteLine($"\r{_title}: [[{new string('■', preenchido)}{new string(' ', vazio)}]] {percentage,3}%");

            if (percentage >= 100)
            {
                Console.WriteLine();
            }
            return;
        }
        else
        {
            sb.Append(CultureInfo.InvariantCulture, $" {_title}: [[{new string('■', preenchido)}{new string(' ', vazio)}]]");

            if (percentage >= 100)
            {
                sb.AppendLine();
            }
        }
    }
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        // Report(1d);
    }
}