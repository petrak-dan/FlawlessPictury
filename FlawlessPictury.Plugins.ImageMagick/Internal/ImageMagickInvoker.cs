using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using FlawlessPictury.AppCore.Common;
using FlawlessPictury.AppCore.CrossCutting;
using AppLogLevel = FlawlessPictury.AppCore.CrossCutting.LogLevel;

namespace FlawlessPictury.Plugins.ImageMagick.Internal
{
    /// <summary>
    /// ImageMagick command-line invocations (portable-friendly).
    ///
    /// Key compatibility note:
    /// - Some builds (or wrapper EXEs) treat a token like "convert" as an INPUT filename.
    ///   To be robust, convert is invoked using the IM7 "direct" form:
    ///     magick &lt;input&gt; [options] &lt;output&gt;
    ///   (no "convert" subcommand).
    ///
    /// Compare is invoked using the IM7 primary command form:
    ///     magick compare -metric &lt;metric&gt; &lt;ref&gt; &lt;cand&gt; [options] null:
    ///
    /// This class is intentionally thin: it does not interpret "formats" or "metrics" beyond
    /// passing preset-provided strings to ImageMagick.
    /// </summary>
    internal static class ImageMagickInvoker
    {

/// <summary>Synchronization for one-time tool info logging.</summary>
private static readonly object _toolLogSync = new object();

/// <summary>The resolved ImageMagick executable path that was logged (to avoid noisy repeats).</summary>
private static string _loggedExePath;

/// <summary>Whether the tool version banner was logged.</summary>
private static bool _loggedVersion;


        public static async Task<Result<string>> RunConvertAsync(
            IProcessRunner runner,
            ILogger logger,
            IDictionary<string, string> baseEnv,
            string executablePath,
            string inputPath,
            string outputPath,
            string options,
            CancellationToken cancellationToken)
        {
            if (runner == null) return Result<string>.Fail(Error.Validation("Process runner is null."));
            if (string.IsNullOrWhiteSpace(executablePath)) return Result<string>.Fail(Error.Validation("ImageMagick executablePath is required."));
            if (string.IsNullOrWhiteSpace(inputPath)) return Result<string>.Fail(Error.Validation("inputPath is required."));
            if (string.IsNullOrWhiteSpace(outputPath)) return Result<string>.Fail(Error.Validation("outputPath is required."));

            var exe = ResolveExecutablePath(executablePath);
            var env = BuildEnvironment(baseEnv, exe);

            
            LogResolvedExecutableOnce(exe, logger);
            await LogVersionOnceAsync(runner, logger, exe, env, cancellationToken).ConfigureAwait(false);
            // Use the ImageMagick 7 primary command directly via magick.exe.
            var exeDir = SafeDirOf(exe);
            var fileName = exe;

            EnsureOutputDirectory(outputPath, logger);

            var args = CommandLine.Quote(inputPath) + " ";
            if (!string.IsNullOrWhiteSpace(options))
            {
                args += options.Trim() + " ";
            }
            args += CommandLine.Quote(outputPath);

            var opt = new ProcessExecutionOptions
            {
                FileName = fileName,
                Arguments = args,
                WorkingDirectory = TryGetMagickHome(env) ?? exeDir,
                Environment = env,
                MergeStdErrIntoStdOut = false
            };

            var r = await runner.RunAsync(opt, cancellationToken).ConfigureAwait(false);
            if (r.IsFailure)
            {
                return Result<string>.Fail(r.Error);
            }

            if (r.Value.ExitCode != 0)
            {
                var details = BuildFailureDetails(r.Value);
                logger?.Log(AppLogLevel.Warn, "ImageMagick convert failed (exit " + r.Value.ExitCode + ")." + details);
                return Result<string>.Fail(Error.NotSupported("ImageMagick convert failed.", details));
            }

            if (!File.Exists(outputPath))
            {
                var details = "Output file not found after successful convert: " + outputPath;
                logger?.Log(AppLogLevel.Warn, details);
                return Result<string>.Fail(Error.NotSupported("ImageMagick convert did not produce output.", details));
            }

            return Result<string>.Ok(outputPath);
        }

        public static async Task<Result<byte[]>> RunPreviewPngAsync(
            IProcessRunner runner,
            ILogger logger,
            IDictionary<string, string> baseEnv,
            string executablePath,
            string inputPath,
            CancellationToken cancellationToken)
        {
            if (runner == null) return Result<byte[]>.Fail(Error.Validation("Process runner is null."));
            if (string.IsNullOrWhiteSpace(executablePath)) return Result<byte[]>.Fail(Error.Validation("ImageMagick executablePath is required."));
            if (string.IsNullOrWhiteSpace(inputPath)) return Result<byte[]>.Fail(Error.Validation("inputPath is required."));

            var exe = ResolveExecutablePath(executablePath);
            var env = BuildEnvironment(baseEnv, exe);
            LogResolvedExecutableOnce(exe, logger);
            await LogVersionOnceAsync(runner, logger, exe, env, cancellationToken).ConfigureAwait(false);

            var exeDir = SafeDirOf(exe);
            var opt = new ProcessExecutionOptions
            {
                FileName = exe,
                Arguments = CommandLine.Quote(inputPath) + " -auto-orient png:-",
                WorkingDirectory = TryGetMagickHome(env) ?? exeDir,
                Environment = env,
                MergeStdErrIntoStdOut = false,
                CaptureStandardOutputBytes = true
            };

            var r = await runner.RunAsync(opt, cancellationToken).ConfigureAwait(false);
            if (r.IsFailure)
            {
                return Result<byte[]>.Fail(r.Error);
            }

            if (r.Value.ExitCode != 0)
            {
                var details = BuildFailureDetails(r.Value);
                logger?.Log(AppLogLevel.Warn, "ImageMagick preview failed (exit " + r.Value.ExitCode + ")." + details);
                return Result<byte[]>.Fail(Error.NotSupported("ImageMagick preview failed.", details));
            }

            if (r.Value.StandardOutputBytes == null || r.Value.StandardOutputBytes.Length == 0)
            {
                return Result<byte[]>.Fail(Error.NotSupported("ImageMagick preview did not produce image bytes."));
            }

            return Result<byte[]>.Ok(r.Value.StandardOutputBytes);
        }

        public static async Task<Result<double>> RunCompareMetricAsync(
            IProcessRunner runner,
            ILogger logger,
            IDictionary<string, string> baseEnv,
            string executablePath,
            string metricName,
            string referencePath,
            string candidatePath,
            string options,
            CancellationToken cancellationToken)
        {
            if (runner == null) return Result<double>.Fail(Error.Validation("Process runner is null."));
            if (string.IsNullOrWhiteSpace(executablePath)) return Result<double>.Fail(Error.Validation("ImageMagick executablePath is required."));
            if (string.IsNullOrWhiteSpace(metricName)) return Result<double>.Fail(Error.Validation("metricName is required."));
            if (string.IsNullOrWhiteSpace(referencePath)) return Result<double>.Fail(Error.Validation("referencePath is required."));
            if (string.IsNullOrWhiteSpace(candidatePath)) return Result<double>.Fail(Error.Validation("candidatePath is required."));

            var exe = ResolveExecutablePath(executablePath);
            var env = BuildEnvironment(baseEnv, exe);

            
            LogResolvedExecutableOnce(exe, logger);
            await LogVersionOnceAsync(runner, logger, exe, env, cancellationToken).ConfigureAwait(false);
var exeDir = SafeDirOf(exe);

            var fileName = exe;
            var args = "compare -metric " + metricName + " " +
                       CommandLine.Quote(referencePath) + " " +
                       CommandLine.Quote(candidatePath) + " ";

            if (!string.IsNullOrWhiteSpace(options))
            {
                args += options.Trim() + " ";
            }

            args += "null:";

            var opt = new ProcessExecutionOptions
            {
                FileName = fileName,
                Arguments = args,
                WorkingDirectory = TryGetMagickHome(env) ?? exeDir,
                Environment = env,
                MergeStdErrIntoStdOut = false
            };

            var r = await runner.RunAsync(opt, cancellationToken).ConfigureAwait(false);
            if (r.IsFailure)
            {
                return Result<double>.Fail(r.Error);
            }

            // ImageMagick: 0 = identical, 1 = different (not an error), 2 = error
            if (r.Value.ExitCode == 2)
            {
                var details = BuildFailureDetails(r.Value);
                logger?.Log(AppLogLevel.Warn, "ImageMagick compare failed (exit 2)." + details);
                return Result<double>.Fail(Error.NotSupported("ImageMagick compare failed.", details));
            }

            var parsed = MetricParser.TryParse(r.Value.StandardError) ?? MetricParser.TryParse(r.Value.StandardOutput);
            if (!parsed.HasValue)
            {
                var details = BuildFailureDetails(r.Value);
                logger?.Log(AppLogLevel.Warn, "Failed to parse metric from ImageMagick output." + details);
                return Result<double>.Fail(Error.NotSupported("Failed to parse metric from ImageMagick output.", details));
            }

            return Result<double>.Ok(parsed.Value);
        }

        private static string ResolveExecutablePath(string executablePath)
        {
            var raw = (executablePath ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(raw))
            {
                raw = "magick";
            }

            var normalized = raw.Replace('/', Path.DirectorySeparatorChar);

            if (Path.IsPathRooted(normalized))
            {
                return normalized;
            }

            if (string.Equals(normalized, "magick", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(normalized, "magick.exe", StringComparison.OrdinalIgnoreCase))
            {
                var exeDir = AppDomain.CurrentDomain.BaseDirectory;
                var toolsRoot = Path.Combine(exeDir, "Tools", "ImageMagick");

                var c1 = Path.Combine(toolsRoot, "magick.exe");
                if (File.Exists(c1)) return c1;

                var c2 = Path.Combine(toolsRoot, "bin", "magick.exe");
                if (File.Exists(c2)) return c2;

                return "magick";
            }

            if (normalized.IndexOf(Path.DirectorySeparatorChar) >= 0 || normalized.IndexOf(Path.AltDirectorySeparatorChar) >= 0)
            {
                try
                {
                    var exeDir = AppDomain.CurrentDomain.BaseDirectory;
                    return Path.GetFullPath(Path.Combine(exeDir, normalized));
                }
                catch
                {
                    return normalized;
                }
            }

            return normalized;
        }

        
/// <summary>
/// Logs the resolved ImageMagick executable path once per process to make troubleshooting easier.
/// </summary>
private static void LogResolvedExecutableOnce(string resolvedExePath, ILogger logger)
{
    if (logger == null)
    {
        return;
    }

    if (string.IsNullOrWhiteSpace(resolvedExePath))
    {
        return;
    }

    var key = resolvedExePath.Trim();

    lock (_toolLogSync)
    {
        if (string.Equals(_loggedExePath, key, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        _loggedExePath = key;
    }

    // Keep this in file logs for diagnostics, but do not spam normal UI status/log surfaces.
    logger.Log(AppLogLevel.Debug, "ImageMagick executable: " + key);
}

/// <summary>
/// Logs ImageMagick version banner once (best effort).
/// </summary>
private static async Task LogVersionOnceAsync(
    IProcessRunner runner,
    ILogger logger,
    string resolvedExePath,
    IDictionary<string, string> env,
    CancellationToken cancellationToken)
{
    if (runner == null || logger == null)
    {
        return;
    }

    lock (_toolLogSync)
    {
        if (_loggedVersion)
        {
            return;
        }

        _loggedVersion = true;
    }

    try
    {
        if (string.IsNullOrWhiteSpace(resolvedExePath))
        {
            return;
        }

        var wd = TryGetMagickHome(env) ?? SafeDirOf(resolvedExePath);

        var opt = new ProcessExecutionOptions
        {
            FileName = resolvedExePath,
            Arguments = "-version",
            WorkingDirectory = wd,
            Environment = env,
            MergeStdErrIntoStdOut = true
        };

        var r = await runner.RunAsync(opt, cancellationToken).ConfigureAwait(false);
        if (r.IsFailure)
        {
            return;
        }

        var text = (r.Value.StandardOutput ?? string.Empty).Replace('\r', '\n');
        foreach (var line in text.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries))
        {
            var trimmed = line.Trim();
            if (trimmed.Length == 0)
            {
                continue;
            }

            logger.Log(AppLogLevel.Debug, "ImageMagick version: " + TrimForLog(trimmed, 240));
            break;
        }
    }
    catch
    {
        // Best-effort only.
    }
}

private static IDictionary<string, string> BuildEnvironment(IDictionary<string, string> baseEnv, string resolvedExePath)
        {
            var env = baseEnv == null
                ? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                : new Dictionary<string, string>(baseEnv, StringComparer.OrdinalIgnoreCase);

            var magickHome = TryDeriveMagickHome(resolvedExePath);

            if (!env.ContainsKey("MAGICK_HOME") && !string.IsNullOrWhiteSpace(magickHome))
            {
                env["MAGICK_HOME"] = magickHome;
            }

            magickHome = TryGetMagickHome(env) ?? magickHome;

            if (!string.IsNullOrWhiteSpace(magickHome))
            {
                if (!env.ContainsKey("MAGICK_CONFIGURE_PATH"))
                {
                    env["MAGICK_CONFIGURE_PATH"] = magickHome;
                }

                var coders = Path.Combine(magickHome, "modules", "coders");
                if (Directory.Exists(coders) && !env.ContainsKey("MAGICK_CODER_MODULE_PATH"))
                {
                    env["MAGICK_CODER_MODULE_PATH"] = coders;
                }

                var filters = Path.Combine(magickHome, "modules", "filters");
                if (Directory.Exists(filters) && !env.ContainsKey("MAGICK_FILTER_MODULE_PATH"))
                {
                    env["MAGICK_FILTER_MODULE_PATH"] = filters;
                }

                // Ensure IM folders are on PATH so dependent DLLs can be found.
                try
                {
                    var existingPath = Environment.GetEnvironmentVariable("PATH") ?? string.Empty;
                    var bin = Path.Combine(magickHome, "bin");

                    var addPath = Directory.Exists(bin)
                        ? (bin + ";" + magickHome)
                        : magickHome;

                    env["PATH"] = addPath + ";" + existingPath;
                }
                catch
                {
                }
            }

            return env;
        }

        private static string TryGetMagickHome(IDictionary<string, string> env)
        {
            if (env == null) return null;

            string value;
            if (env.TryGetValue("MAGICK_HOME", out value))
            {
                return value;
            }

            return null;
        }

        private static string TryDeriveMagickHome(string resolvedExePath)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(resolvedExePath)) return null;
                if (!Path.IsPathRooted(resolvedExePath)) return null;

                var exeDir = Path.GetDirectoryName(resolvedExePath);
                if (string.IsNullOrWhiteSpace(exeDir)) return null;

                var di = new DirectoryInfo(exeDir);
                if (di != null &&
                    string.Equals(di.Name, "bin", StringComparison.OrdinalIgnoreCase) &&
                    di.Parent != null)
                {
                    return di.Parent.FullName;
                }

                return exeDir;
            }
            catch
            {
                return null;
            }
        }

        private static void EnsureOutputDirectory(string outputPath, ILogger logger)
        {
            try
            {
                var dir = Path.GetDirectoryName(outputPath);
                if (string.IsNullOrWhiteSpace(dir)) return;

                if (!Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }
            }
            catch (Exception ex)
            {
                logger?.Log(AppLogLevel.Warn, "Failed to ensure output directory exists for: " + outputPath, ex);
            }
        }

        private static string BuildFailureDetails(ProcessExecutionResult r)
        {
            if (r == null) return string.Empty;

            var err = TrimForDetails(r.StandardError, 4000);
            var outp = TrimForDetails(r.StandardOutput, 2000);

            var details = string.Empty;

            if (!string.IsNullOrWhiteSpace(err))
            {
                details += Environment.NewLine + "stderr: " + err;
            }

            if (!string.IsNullOrWhiteSpace(outp))
            {
                details += Environment.NewLine + "stdout: " + outp;
            }

            return details;
        }

        private static string TrimForDetails(string text, int maxChars)
        {
            if (string.IsNullOrWhiteSpace(text)) return null;

            text = text.Trim();

            if (maxChars <= 0) return text;
            if (text.Length <= maxChars) return text;

            return text.Substring(0, maxChars) + " ...";
        }

        private static string SafeDirOf(string path)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(path)) return string.Empty;
                return Path.GetDirectoryName(path) ?? string.Empty;
            }
            catch
            {
                return string.Empty;
            }
        }


/// <summary>
/// Trims text for logs (keeps the log readable and avoids multi-megabyte stderr/stdout dumps).
/// </summary>
private static string TrimForLog(string text, int maxChars)
{
    if (string.IsNullOrEmpty(text))
    {
        return text;
    }

    if (maxChars <= 0 || text.Length <= maxChars)
    {
        return text;
    }

    return text.Substring(0, maxChars) + "...";
}

    }
}
