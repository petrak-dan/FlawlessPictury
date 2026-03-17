using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using FlawlessPictury.AppCore.CrossCutting;
using AppLogLevel = FlawlessPictury.AppCore.CrossCutting.LogLevel;

namespace FlawlessPictury.Infrastructure.SafeOutput
{
    public sealed class OutputCommitter : IOutputCommitter
    {
        private readonly ILogger _logger;

/// <summary>
/// Creates a committer without logging. Prefer the ILogger constructor when possible.
/// </summary>
public OutputCommitter()
    : this(null)
{
}

/// <summary>
/// Creates a committer that logs committed output paths at Info level.
/// </summary>
public OutputCommitter(ILogger logger)
{
    _logger = logger;
}

public async Task<OutputCommitResult> CommitFileAsync(string sourceFilePath, string destinationDirectory, string preferredFileName, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(sourceFilePath))
            {
                throw new ArgumentException("Source file path is required.", nameof(sourceFilePath));
            }

            if (!File.Exists(sourceFilePath))
            {
                throw new FileNotFoundException("Source file does not exist.", sourceFilePath);
            }

            if (string.IsNullOrWhiteSpace(destinationDirectory))
            {
                throw new ArgumentException("Destination directory is required.", nameof(destinationDirectory));
            }

            Directory.CreateDirectory(destinationDirectory);

            var fileName = !string.IsNullOrWhiteSpace(preferredFileName)
                ? preferredFileName
                : Path.GetFileName(sourceFilePath);

            if (string.IsNullOrWhiteSpace(fileName))
            {
                fileName = "output.bin";
            }

            var desired = Path.Combine(destinationDirectory, fileName);
            var finalPath = EnsureUniquePath(desired);

            const int bufferSize = 1024 * 1024;
            using (var source = new FileStream(sourceFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, bufferSize, useAsync: true))
            using (var target = new FileStream(finalPath, FileMode.CreateNew, FileAccess.Write, FileShare.None, bufferSize, useAsync: true))
            {
                await source.CopyToAsync(target, bufferSize, cancellationToken).ConfigureAwait(false);
            }

// Info-level "what happened" log: where the file was actually saved.
try
{
    long bytes = 0;
    try { bytes = new FileInfo(finalPath).Length; } catch { }

    var note = string.Empty;
    if (!string.Equals(desired, finalPath, StringComparison.OrdinalIgnoreCase))
    {
        note = " (collision renamed)";
    }

    _logger?.Log(AppLogLevel.Info,
        "Saved output: " + finalPath +
        (bytes > 0 ? (" (" + bytes.ToString(System.Globalization.CultureInfo.InvariantCulture) + " bytes)") : string.Empty) +
        note);
}
catch
{
    // Logging must never break the commit.
}

return new OutputCommitResult(finalPath);
        }

        private static string EnsureUniquePath(string desiredPath)
        {
            if (!File.Exists(desiredPath))
            {
                return desiredPath;
            }

            var dir = Path.GetDirectoryName(desiredPath) ?? string.Empty;
            var name = Path.GetFileNameWithoutExtension(desiredPath);
            var ext = Path.GetExtension(desiredPath);

            for (int i = 1; i < 10000; i++)
            {
                var candidate = Path.Combine(dir, $"{name} ({i}){ext}");
                if (!File.Exists(candidate))
                {
                    return candidate;
                }
            }

            return Path.Combine(dir, $"{name}_{Guid.NewGuid():N}{ext}");
        }
    }
}
