using YoutubeDLSharp;

namespace VideoDownloader.Progress;

internal sealed class ConsoleDownloadProgressBar : IProgress<double>, IDisposable
{
    private readonly string _title;
    private readonly int _width;
    private readonly object _sync = new();
    private int _lastPercent = -1;
    private bool _disposed;

    public ConsoleDownloadProgressBar(string title = "Download", int width = 40)
    {
        _title = string.IsNullOrWhiteSpace(title) ? "Download" : title.Trim();
        _width = Math.Max(10, width);
    }

    public void Report(double value)
    {
        if (_disposed)
        {
            return;
        }

        var percentage = Math.Clamp(value, 0d, 1d) * 100d;
        var rounded = (int)Math.Round(percentage);

        lock (_sync)
        {
            if (rounded == _lastPercent)
            {
                return;
            }

            _lastPercent = rounded;
            Render(rounded);
        }
    }

    private void Render(int percentage)
    {
        var preenchido = (int)Math.Round(_width * (percentage / 100d));
        var vazio = _width - preenchido;
        Console.Write($"\r{_title}: [{new string('=', preenchido)}{new string(' ', vazio)}] {percentage,3}%");

        if (percentage >= 100)
        {
            Console.WriteLine();
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        Report(1d);
    }
}

internal sealed class YoutubeDlProgressBridge : IProgress<DownloadProgress>
{
    private readonly IProgress<double> _progress;

    public YoutubeDlProgressBridge(IProgress<double> progress)
    {
        _progress = progress ?? throw new ArgumentNullException(nameof(progress));
    }

    public void Report(DownloadProgress value)
    {
        ArgumentNullException.ThrowIfNull(value);
        _progress.Report(value.Progress);
    }
}
