using Microsoft.Extensions.Localization;
using System.Diagnostics;
using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Text.RegularExpressions;
using VideoDownloader.Constantes;
using VideoDownloader.Progress;
using YoutubeDLSharp;

namespace VideoDownloader.Services.Implementation;

internal sealed class YoutubeDLService(AppSettings appContext, IStringLocalizer<YoutubeDLService> localizer)
{
    private static readonly Regex PercentRegex = new(@"\b(?<percent>\d{1,3}(?:\.\d+)?)%", RegexOptions.Compiled);

    private async Task ObterDependenciasAsync(string basePath)
    {
        var ytDlpPath = ResolvePath(basePath, appContext.YtDlpExe);
        var ffmpegPath = ResolvePath(basePath, appContext.FfmpegExe);

        if (File.Exists(ytDlpPath) && File.Exists(ffmpegPath))
        {
            return;
        }

        await Utils.DownloadYtDlp().ConfigureAwait(false);
        await Utils.DownloadFFmpeg().ConfigureAwait(false);
    }

    public async Task VideoDLAsync(string videoUrl, IProgress<double>? progress = null)
    {
        var app = Directory.GetCurrentDirectory();
        var pastaRecursos = appContext.PastaRecursos ?? "Resources";

        Directory.CreateDirectory(Path.Combine(app, pastaRecursos));

        using var progressBar = progress is null ? new ConsoleDownloadProgressBar("yt-dlp") : null;
        var progressReporter = progress ?? progressBar!;

        if (await EhUrlDiretaDeMidiaAsync(videoUrl).ConfigureAwait(false) && !EhUrlDeSiteComExtractor(videoUrl))
        {
            await DownloadArquivoDiretoAsync(videoUrl, app, progressReporter).ConfigureAwait(false);
            return;
        }

        if (EhUrlSiteComFallback(videoUrl))
        {
            var baixado = await DownloadFallbackManualAsync(videoUrl, app, progressReporter).ConfigureAwait(false);
            if (baixado)
            {
                return;
            }

            Console.WriteLine($"Erro: site não suportado ou sem mídia acessível: {new Uri(videoUrl).Host}");
            return;
        }

        await ObterDependenciasAsync(app).ConfigureAwait(false);

        var ytDlpPath = ResolvePath(app, appContext.YtDlpExe);
        var ffmpegPath = ResolvePath(app, appContext.FfmpegExe);
        var outputTemplate = appContext.NomeArquivo ?? "%(title)s.%(ext)s";
        var arguments = CriarArgumentosYtDlp(app, ffmpegPath, pastaRecursos, outputTemplate);

        arguments.Add(videoUrl);

        Console.WriteLine(localizer["Baixando_Video"]);
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
            "--no-cookies",
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

        var outputLines = new List<string>();
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

                outputLines.Add(line);
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
        await Task.WhenAll(outputTask, errorTask).ConfigureAwait(false);

        var output = string.Join(Environment.NewLine, outputLines);
        var error = string.Join(Environment.NewLine, errorLines);

        if (!string.IsNullOrWhiteSpace(output))
        {
            Console.WriteLine(output);
        }

        if (!string.IsNullOrWhiteSpace(error))
        {
            await Console.Error.WriteLineAsync(error).ConfigureAwait(false);
        }

        progress?.Report(1d);

        if (process.ExitCode != 0)
        {
            var details = string.IsNullOrWhiteSpace(error)
                ? output
                : string.Join(Environment.NewLine, new[] { output, error }.Where(static part => !string.IsNullOrWhiteSpace(part)));

            if (!string.IsNullOrWhiteSpace(details))
            {
                await Console.Error.WriteLineAsync(details).ConfigureAwait(false);
            }

            var mensagemAmigavel = details.Contains("Unable to extract universal data", StringComparison.OrdinalIgnoreCase)
                || details.Contains("Unable to extract", StringComparison.OrdinalIgnoreCase)
                || details.Contains("TikTok", StringComparison.OrdinalIgnoreCase)
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

        var match = PercentRegex.Match(line);
        if (!match.Success)
        {
            return;
        }

        if (double.TryParse(match.Groups["percent"].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out var percent))
        {
            progress.Report(Math.Clamp(percent / 100d, 0d, 1d));
        }
    }

    private static bool EhUrlSiteComFallback(string url)
    {
        return Uri.TryCreate(url, UriKind.Absolute, out var uri) &&
               !uri.Host.Contains("youtube", StringComparison.OrdinalIgnoreCase) &&
               !uri.Host.Contains("youtu.be", StringComparison.OrdinalIgnoreCase) &&
               uri.Scheme is "http" or "https";
    }

    private static async Task<bool> DownloadFallbackManualAsync(string videoUrl, string app, IProgress<double>? progress)
    {
        try
        {
            using var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0");

            using var response = await httpClient.GetAsync(videoUrl, HttpCompletionOption.ResponseHeadersRead).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();

            var html = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            var cookies = await TentarObterCookiesFallbackAsync(videoUrl).ConfigureAwait(false);
            if (cookies.Count > 0)
            {
                foreach (var cookie in cookies)
                {
                    httpClient.DefaultRequestHeaders.TryAddWithoutValidation("Cookie", $"{cookie.Name}={cookie.Value}");
                }
            }

            var mediaUrl = ExtrairUrlMidiaFallback(html);

            if (string.IsNullOrWhiteSpace(mediaUrl))
            {
                return false;
            }

            var fileName = Path.GetFileName(new Uri(mediaUrl).AbsolutePath);
            if (string.IsNullOrWhiteSpace(Path.GetExtension(fileName)))
            {
                fileName = $"{Path.GetFileNameWithoutExtension(fileName)}.mp4";
            }

            var fullPath = Path.Combine(app, fileName);
            await using var outputStream = File.Create(fullPath);
            await using var inputStream = await httpClient.GetStreamAsync(mediaUrl).ConfigureAwait(false);

            var buffer = new byte[81920];
            while (true)
            {
                var read = await inputStream.ReadAsync(buffer.AsMemory(0, buffer.Length)).ConfigureAwait(false);
                if (read == 0)
                {
                    break;
                }

                await outputStream.WriteAsync(buffer.AsMemory(0, read)).ConfigureAwait(false);
            }

            progress?.Report(1d);
            Console.WriteLine($"Download concluído: {fullPath}");
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static async Task<IReadOnlyList<Cookie>> TentarObterCookiesFallbackAsync(string videoUrl)
    {
        try
        {
            var host = new Uri(videoUrl).Host;
            var cookieStorePaths = new[]
            {
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Google", "Chrome", "User Data", "Default", "Network", "Cookies"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Google", "Chrome", "User Data", "Profile 1", "Network", "Cookies"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Microsoft", "Edge", "User Data", "Default", "Network", "Cookies"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Microsoft", "Edge", "User Data", "Profile 1", "Network", "Cookies")
            };

            foreach (var cookiePath in cookieStorePaths)
            {
                if (!File.Exists(cookiePath))
                {
                    continue;
                }

                var cookies = await LerCookiesSqliteAsync(cookiePath, host).ConfigureAwait(false);
                if (cookies.Count > 0)
                {
                    return cookies;
                }
            }
        }
        catch
        {
            // Ignora falhas e tenta o próximo caminho.
        }

        return Array.Empty<Cookie>();
    }

    private static async Task<List<Cookie>> LerCookiesSqliteAsync(string cookieDbPath, string host)
    {
        try
        {
            var bytes = await File.ReadAllBytesAsync(cookieDbPath).ConfigureAwait(false);
            if (bytes.Length == 0)
            {
                return new List<Cookie>();
            }

            var text = System.Text.Encoding.UTF8.GetString(bytes);
            if (text.Contains("sqlite", StringComparison.OrdinalIgnoreCase))
            {
                return new List<Cookie>();
            }

            var lines = text.Split(['\n', '\r'], StringSplitOptions.RemoveEmptyEntries);
            var cookies = new List<Cookie>();
            foreach (var line in lines)
            {
                if (!line.Contains(host, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var parts = line.Split('|');
                if (parts.Length >= 3)
                {
                    var cookieHost = parts[0].Trim();
                    var cookieName = parts[1].Trim();
                    var cookieValue = parts[2].Trim();
                    if (!string.IsNullOrWhiteSpace(cookieName) && !string.IsNullOrWhiteSpace(cookieValue))
                    {
                        cookies.Add(new Cookie(cookieName, cookieValue, "/", cookieHost));
                    }
                }
            }

            return cookies;
        }
        catch
        {
            return new List<Cookie>();
        }
    }

    private static string? ExtrairUrlMidiaFallback(string html)
    {
        var candidates = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (Match match in Regex.Matches(html, "https?://[^\\\"'\\s<>]+", RegexOptions.IgnoreCase | RegexOptions.Singleline))
        {
            AddCandidate(candidates, match.Value);
        }

        foreach (Match match in Regex.Matches(html, "(?:src|href|content|url|file|playback|video|media)\\s*=\\s*['\"]([^'\"]+)['\"]", RegexOptions.IgnoreCase | RegexOptions.Singleline))
        {
            if (match.Groups.Count > 1)
            {
                AddCandidate(candidates, match.Groups[1].Value);
            }
        }

        foreach (Match match in Regex.Matches(html, "https?%3A%2F%2F[^\\s\"'<>]+", RegexOptions.IgnoreCase | RegexOptions.Singleline))
        {
            AddCandidate(candidates, match.Value);
        }

        foreach (Match match in Regex.Matches(html, "\"(https?://[^\"\\s<>]+)\"", RegexOptions.IgnoreCase | RegexOptions.Singleline))
        {
            AddCandidate(candidates, match.Groups[1].Value);
        }

        foreach (var candidate in candidates)
        {
            if (EhUrlDeMidia(candidate))
            {
                return candidate;
            }
        }

        foreach (var candidate in candidates)
        {
            var decoded = Uri.UnescapeDataString(candidate);
            if (!string.Equals(decoded, candidate, StringComparison.Ordinal) && EhUrlDeMidia(decoded))
            {
                return decoded;
            }
        }

        foreach (var candidate in candidates)
        {
            var normalized = candidate.Replace("\\/", "/");
            if (!string.Equals(normalized, candidate, StringComparison.Ordinal) && EhUrlDeMidia(normalized))
            {
                return normalized;
            }
        }

        return null;
    }

    private static void AddCandidate(HashSet<string> candidates, string value)
    {
        var cleaned = value.Trim().TrimEnd('.', ',', ';', ')', ']', '}', '"', '\'');
        if (Uri.TryCreate(cleaned, UriKind.Absolute, out var uri) &&
            (uri.Scheme.Equals(Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase) ||
             uri.Scheme.Equals(Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase)))
        {
            candidates.Add(cleaned);
        }
        else if (cleaned.Contains("http", StringComparison.OrdinalIgnoreCase))
        {
            candidates.Add(cleaned);
        }
    }

    private static bool EhUrlDeMidia(string candidate)
    {
        if (!Uri.TryCreate(candidate, UriKind.Absolute, out var uri))
        {
            return false;
        }

        if (!uri.Scheme.Equals(Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase) &&
            !uri.Scheme.Equals(Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var path = uri.AbsolutePath;
        return path.Contains(".mp4", StringComparison.OrdinalIgnoreCase) ||
               path.Contains(".m3u8", StringComparison.OrdinalIgnoreCase) ||
               path.Contains("/video", StringComparison.OrdinalIgnoreCase) ||
               path.Contains("/media", StringComparison.OrdinalIgnoreCase) ||
               path.Contains("/stream", StringComparison.OrdinalIgnoreCase) ||
               candidate.Contains(".mp4", StringComparison.OrdinalIgnoreCase) ||
               candidate.Contains(".m3u8", StringComparison.OrdinalIgnoreCase);
    }

    private static async Task DownloadArquivoDiretoAsync(string videoUrl, string app, IProgress<double>? progress)
    {
        using var httpClient = new HttpClient();
        using var response = await httpClient.GetAsync(new Uri(videoUrl), HttpCompletionOption.ResponseHeadersRead).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        var fileName = ObterNomeArquivo(response, new Uri(videoUrl));
        var fullPath = Path.Combine(app, fileName);
        await using var outputStream = File.Create(fullPath);
        await using var inputStream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);

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

            await outputStream.WriteAsync(buffer.AsMemory(0, read)).ConfigureAwait(false);
            totalRead += read;

            if (totalBytes > 0)
            {
                progress?.Report((double)totalRead / totalBytes);
            }
        }

        progress?.Report(1d);
        Console.WriteLine($"Download concluído: {fullPath}");
    }

    private static string ResolvePath(string basePath, string? fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            return Path.Combine(basePath, "yt-dlp.exe");
        }

        return Path.Combine(basePath, fileName);
    }

    private static async Task<bool> EhUrlDiretaDeMidiaAsync(string url)
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

        var extensoes = new[] { ".mp4", ".mkv", ".webm", ".avi", ".mov", ".m4v", ".mp3", ".m4a", ".wav", ".flac" };
        if (extensoes.Any(ext => uri.AbsoluteUri.EndsWith(ext, StringComparison.OrdinalIgnoreCase)))
        {
            return true;
        }

        using var httpClient = new HttpClient();
        try
        {
            using var response = await httpClient.SendAsync(new HttpRequestMessage(HttpMethod.Head, uri), HttpCompletionOption.ResponseHeadersRead).ConfigureAwait(false);
            if (response.StatusCode == HttpStatusCode.MethodNotAllowed || response.StatusCode == HttpStatusCode.NotImplemented)
            {
                using var responseGet = await httpClient.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead).ConfigureAwait(false);
                return EhConteudoMidia(responseGet);
            }

            return EhConteudoMidia(response);
        }
        catch
        {
            return false;
        }
    }

    private static bool EhConteudoMidia(HttpResponseMessage response)
    {
        var contentType = response.Content.Headers.ContentType?.MediaType;
        if (string.IsNullOrWhiteSpace(contentType))
        {
            return false;
        }

        return contentType.StartsWith("video/", StringComparison.OrdinalIgnoreCase) ||
               contentType.StartsWith("audio/", StringComparison.OrdinalIgnoreCase) ||
               contentType.Equals("application/octet-stream", StringComparison.OrdinalIgnoreCase) ||
               contentType.Contains("mp4", StringComparison.OrdinalIgnoreCase) ||
               contentType.Contains("mpeg", StringComparison.OrdinalIgnoreCase) ||
               contentType.Contains("webm", StringComparison.OrdinalIgnoreCase);
    }

    private static bool EhUrlDeSiteComExtractor(string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
        {
            return false;
        }

        return uri.Host.Contains("tiktok", StringComparison.OrdinalIgnoreCase) ||
               uri.Host.Contains("instagram", StringComparison.OrdinalIgnoreCase) ||
               uri.Host.Contains("twitter", StringComparison.OrdinalIgnoreCase) ||
               uri.Host.Contains("facebook", StringComparison.OrdinalIgnoreCase) ||
               uri.Host.Contains("x.com", StringComparison.OrdinalIgnoreCase);
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