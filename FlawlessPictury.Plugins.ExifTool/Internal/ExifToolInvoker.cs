using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FlawlessPictury.AppCore.Common;
using FlawlessPictury.AppCore.CrossCutting;

namespace FlawlessPictury.Plugins.ExifTool.Internal
{
    internal static class ExifToolInvoker
    {
        public static string ResolveExecutablePath(string executablePath)
        {
            var raw = (executablePath ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(raw))
            {
                raw = "exiftool";
            }

            if (Path.IsPathRooted(raw))
            {
                return raw;
            }

            if (string.Equals(raw, "exiftool", StringComparison.OrdinalIgnoreCase) || string.Equals(raw, "exiftool.exe", StringComparison.OrdinalIgnoreCase))
            {
                var exeDir = AppDomain.CurrentDomain.BaseDirectory;
                var candidate = Path.Combine(exeDir, "Tools", "ExifTool", "exiftool.exe");
                if (File.Exists(candidate))
                {
                    return candidate;
                }
                return "exiftool";
            }

            if (raw.IndexOf(Path.DirectorySeparatorChar) >= 0 || raw.IndexOf(Path.AltDirectorySeparatorChar) >= 0)
            {
                try
                {
                    return Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, raw));
                }
                catch
                {
                    return raw;
                }
            }

            return raw;
        }

        public static async Task<Result<ProcessExecutionResult>> RunAsync(IProcessRunner runner, IDictionary<string, string> env, string executablePath, string arguments, CancellationToken cancellationToken)
        {
            if (runner == null) return Result<ProcessExecutionResult>.Fail(Error.Validation("Process runner is null."));
            var exe = ResolveExecutablePath(executablePath);
            var options = new ProcessExecutionOptions
            {
                FileName = exe,
                Arguments = arguments ?? string.Empty,
                Environment = env,
                MergeStdErrIntoStdOut = false
            };
            return await runner.RunAsync(options, cancellationToken).ConfigureAwait(false);
        }

        public static string Quote(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return "\"\"";
            }

            if (value.IndexOf(' ') < 0 && value.IndexOf('"') < 0)
            {
                return value;
            }

            return "\"" + value.Replace("\"", "\\\"") + "\"";
        }

        public static string BuildPropertyReadArguments(string inputPath)
        {
            return "-a -G1 -s " + Quote(inputPath);
        }

        public static string BuildCopyArguments(string sourcePath, string targetPath, bool excludeOrientationTag)
        {
            var sb = new StringBuilder();
            sb.Append("-overwrite_original -P ");
            sb.Append("-TagsFromFile ");
            sb.Append(Quote(sourcePath));
            sb.Append(" -all:all ");
            if (excludeOrientationTag)
            {
                sb.Append("--Orientation ");
            }
            sb.Append(Quote(targetPath));
            return sb.ToString();
        }

        public static string BuildApplyArguments(string rawArguments, string targetPath)
        {
            return (rawArguments ?? string.Empty).Trim() + " " + Quote(targetPath);
        }
    }
}
