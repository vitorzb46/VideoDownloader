using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Net;
using System.Text.RegularExpressions;
using VideoDownloader.Constantes;
using VideoDownloader.Progress;

namespace VideoDownloader.Services.Implementation;

[SuppressMessage("Performance", "CA1812:Avoid uninstantiated internal classes")]
internal sealed partial class YoutubeDLService(AppSettings appContext)
{
    [GeneratedRegex(@"\b(?<percent>\d{1,3}(?:\.\d+)?)%")]
    private static partial Regex PercentRegex();

    public async Task VideoDLAsync(string videoUrl, IProgress<double>? progress = null)
    {
        var app = Directory.GetCurrentDirectory();
        var pastaRecursos = appContext.PastaRecursos ?? "Resources";

        Directory.CreateDirectory(Path.Combine(app, pastaRecursos));
        using var progressBar = progress is null ? new ConsoleDownloadProgressBar("Baixando mídia") : null;
        var progressReporter = progress ?? progressBar!;

        if (await MidiaDiretaAsync(videoUrl).ConfigureAwait(false) && !SiteComExtractor(videoUrl))
        {
            await DownloadArquivoDiretoAsync(videoUrl, app, progressReporter).ConfigureAwait(false);
            return;
        }

        await ObterDependenciasAsync(app).ConfigureAwait(false);

        var ytDlpPath = ResolvePath(app, appContext.YtDlpExe);
        var ffmpegPath = ResolvePath(app, appContext.FfmpegExe);
        var outputTemplate = appContext.NomeArquivo ?? "%(title)s.%(ext)s";
        var arguments = CriarArgumentosYtDlp(app, ffmpegPath, pastaRecursos, outputTemplate);

        arguments.Add(videoUrl);

        await ExecutarYtDlpAsync(ytDlpPath, app, arguments, progressReporter).ConfigureAwait(false);
    }

    private List<string> CriarArgumentosYtDlp(string app, string ffmpegPath, string pastaRecursos, string outputTemplate)
    {
        var cookies = string.IsNullOrWhiteSpace(appContext.Cookies)
            ? null
            : Path.Combine(app, pastaRecursos, appContext.Cookies);

        var arguments = new List<string>
        {
            "--no-warnings",
            "--newline",
            "--output",
            outputTemplate,
            "--ffmpeg-location",
            Path.GetDirectoryName(ffmpegPath) ?? app,
            "--paths",
            app,
        };

        if (!string.IsNullOrWhiteSpace(appContext.UserAgent))
        {
            arguments.AddRange(["--add-header", $"User-Agent: {appContext.UserAgent}"]);
        }

        if (!string.IsNullOrWhiteSpace(cookies))
        {
            arguments.AddRange(["--cookies", cookies]);
        }

        return arguments;
    }

    private static async Task ExecutarYtDlpAsync(string ytDlpPath, string workingDirectory, IEnumerable<string> arguments, IProgress<double>? progress = null)
    {
        using var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = ytDlpPath,
                WorkingDirectory = workingDirectory,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            }
        };

        foreach (var argument in arguments)
        {
            process.StartInfo.ArgumentList.Add(argument);
        }

        process.Start();

        var errorLines = new List<string>();

        var outputTask = Task.Run(async () =>
        {
            while (true)
            {
                var line = await process.StandardOutput.ReadLineAsync().ConfigureAwait(false);
                if (line is null)
                {
                    break;
                }

                ReportarProgressoYtDlp(line, progress);
            }
        });

        var errorTask = Task.Run(async () =>
        {
            while (true)
            {
                var line = await process.StandardError.ReadLineAsync().ConfigureAwait(false);
                if (line is null)
                {
                    break;
                }

                errorLines.Add(line);
            }
        });

        await process.WaitForExitAsync().ConfigureAwait(false);
        var error = string.Join(Environment.NewLine, errorLines);

        if (!string.IsNullOrWhiteSpace(error))
        {
            await Console.Error.WriteLineAsync(error).ConfigureAwait(false);
        }

        progress?.Report(1d);

        if (process.ExitCode != 0)
        {
            if (!string.IsNullOrWhiteSpace(error))
            {
                await Console.Error.WriteLineAsync(error).ConfigureAwait(false);
            }

            var mensagemAmigavel = error.Contains("Unable to extract universal data", StringComparison.OrdinalIgnoreCase)
                || error.Contains("Unable to extract", StringComparison.OrdinalIgnoreCase)
                || error.Contains("TikTok", StringComparison.OrdinalIgnoreCase)
                ? "Não foi possível baixar este conteúdo com a versão atual do yt-dlp. O extrator do TikTok está falhando neste ambiente."
                : $"yt-dlp falhou com o código {process.ExitCode}";

            Console.WriteLine(mensagemAmigavel);
            return;
        }
    }

    private static void ReportarProgressoYtDlp(string line, IProgress<double>? progress)
    {
        if (progress is null)
        {
            return;
        }

        var match = PercentRegex().Match(line);
        if (!match.Success)
        {
            return;
        }

        if (double.TryParse(match.Groups["percent"].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out var percent))
        {
            progress.Report(Math.Clamp(percent / 100d, 0d, 1d));
        }
    }

    private static async Task DownloadArquivoDiretoAsync(string videoUrl, string app, IProgress<double>? progress)
    {
        using var httpClient = new HttpClient();
        using var response = await httpClient.GetAsync(new Uri(videoUrl),
                                                       HttpCompletionOption.ResponseHeadersRead).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        var fileName = ObterNomeArquivo(response, new Uri(videoUrl));
        var fullPath = Path.Combine(app, fileName);

        var fileStream = File.Create(fullPath);
        var inputStream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);

        //descarte assync
        await using var _ = fileStream.ConfigureAwait(false);
        await using var __ = inputStream.ConfigureAwait(false);

        var buffer = new byte[81920];
        var totalBytes = response.Content.Headers.ContentLength ?? -1;
        long totalRead = 0;

        while (true)
        {
            var read = await inputStream.ReadAsync(buffer.AsMemory(0, buffer.Length)).ConfigureAwait(false);
            if (read == 0)
            {
                break;
            }

            await fileStream.WriteAsync(buffer.AsMemory(0, read)).ConfigureAwait(false);
            totalRead += read;

            if (totalBytes > 0)
            {
                progress?.Report((double)totalRead / totalBytes);
            }
        }

        progress?.Report(1d);
        Console.WriteLine($"Download concluído: {fullPath}");
    }
    private async Task ObterDependenciasAsync(string basePath)
    {
        var ytDlpPath = ResolvePath(basePath, appContext.YtDlpExe);
        var ffmpegPath = ResolvePath(basePath, appContext.FfmpegExe);

        if (!File.Exists(ytDlpPath))
        {
            // DownloadYtDlp(string) baixa o yt-dlp para o caminho informado.
            await YoutubeDLSharp.Utils.DownloadYtDlp(ytDlpPath).ConfigureAwait(false);
        }

        if (!File.Exists(ffmpegPath))
        {
            await YoutubeDLSharp.Utils.DownloadFFmpeg(ffmpegPath).ConfigureAwait(false);
        }
    }
    private static string ResolvePath(string basePath, string? fileName)
    {
        return string.IsNullOrWhiteSpace(fileName) ? Path.Combine(basePath, "yt-dlp.exe") : Path.Combine(basePath, fileName);
    }
    private static async Task<bool> MidiaDiretaAsync(string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
        {
            return false;
        }

        if (uri.Host.Contains("youtube", StringComparison.OrdinalIgnoreCase) ||
            uri.Host.Contains("youtu.be", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var uriAbsolute = uri.AbsolutePath;
        var extensoes = Path.GetExtension(uriAbsolute);
        var extensoesValidas = new[] { ".mp4", ".mkv", ".webm", ".avi", ".mov", ".m4v", ".mp3", ".m4a", ".wav", ".flac" };
        if (extensoesValidas.Contains(extensoes, StringComparer.OrdinalIgnoreCase))
        {
            return true;
        }

        using var httpClient = new HttpClient();
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Head, uri);
            using var response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead).ConfigureAwait(false);
            if (response.StatusCode == HttpStatusCode.MethodNotAllowed || response.StatusCode == HttpStatusCode.NotImplemented)
            {
                using var responseGet = await httpClient.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead).ConfigureAwait(false);
                return ConteudoMidia(responseGet);
            }
            return ConteudoMidia(response);
        }
        catch
        {
            return false;
        }
    }

    private static bool ConteudoMidia(HttpResponseMessage response)
    {
        var contentType = response.Content.Headers.ContentType?.MediaType;
        return !string.IsNullOrWhiteSpace(contentType) && (contentType.StartsWith("video/", StringComparison.OrdinalIgnoreCase) ||
               contentType.StartsWith("audio/", StringComparison.OrdinalIgnoreCase) ||
               contentType.Equals("application/octet-stream", StringComparison.OrdinalIgnoreCase) ||
               contentType.Contains("mp4", StringComparison.OrdinalIgnoreCase) ||
               contentType.Contains("mpeg", StringComparison.OrdinalIgnoreCase) ||
               contentType.Contains("webm", StringComparison.OrdinalIgnoreCase));
    }

    private static bool SiteComExtractor(string url)
    {
        return Uri.TryCreate(url, UriKind.Absolute, out var uri) && (uri.Host.Contains("tiktok", StringComparison.OrdinalIgnoreCase) ||
               uri.Host.Contains("instagram", StringComparison.OrdinalIgnoreCase) ||
               uri.Host.Contains("twitter", StringComparison.OrdinalIgnoreCase) ||
               uri.Host.Contains("facebook", StringComparison.OrdinalIgnoreCase) ||
               uri.Host.Contains("x.com", StringComparison.OrdinalIgnoreCase));
    }

    private static string ObterNomeArquivo(HttpResponseMessage response, Uri uri)
    {
        var contentDisposition = response.Content.Headers.ContentDisposition;
        var suggestedName = contentDisposition?.FileNameStar ?? contentDisposition?.FileName;
        if (!string.IsNullOrWhiteSpace(suggestedName))
        {
            suggestedName = suggestedName.Trim('"');
            if (Path.HasExtension(suggestedName))
            {
                return Path.GetFileName(suggestedName);
            }
        }

        var pathName = Path.GetFileName(uri.AbsolutePath);
        if (!string.IsNullOrWhiteSpace(pathName) && Path.HasExtension(pathName))
        {
            return pathName;
        }

        var contentType = response.Content.Headers.ContentType?.MediaType;
        var extension = contentType switch
        {
            null => ".bin",
            var type when type.Contains("mp4", StringComparison.OrdinalIgnoreCase) => ".mp4",
            var type when type.Contains("webm", StringComparison.OrdinalIgnoreCase) => ".webm",
            var type when type.Contains("mpeg", StringComparison.OrdinalIgnoreCase) => ".mp3",
            var type when type.Contains("wav", StringComparison.OrdinalIgnoreCase) => ".wav",
            var type when type.Contains("ogg", StringComparison.OrdinalIgnoreCase) => ".ogg",
            _ => ".bin"
        };

        return string.IsNullOrWhiteSpace(pathName) ? $"arquivo_{Guid.NewGuid():N}{extension}" : $"{Path.GetFileNameWithoutExtension(pathName)}{extension}";
    }
}