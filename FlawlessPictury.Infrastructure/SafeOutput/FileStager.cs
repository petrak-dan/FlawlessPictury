using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using FlawlessPictury.AppCore.CrossCutting;

namespace FlawlessPictury.Infrastructure.SafeOutput
{
    public sealed class FileStager : IFileStager
    {
        public async Task<string> StageAsync(string inputFilePath, string stagingDirectory, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(inputFilePath))
            {
                throw new ArgumentException("Input file path is required.", nameof(inputFilePath));
            }

            if (string.IsNullOrWhiteSpace(stagingDirectory))
            {
                throw new ArgumentException("Staging directory is required.", nameof(stagingDirectory));
            }

            if (!File.Exists(inputFilePath))
            {
                throw new FileNotFoundException("Input file does not exist.", inputFilePath);
            }

            Directory.CreateDirectory(stagingDirectory);

            var fileName = Path.GetFileName(inputFilePath);
            if (string.IsNullOrWhiteSpace(fileName))
            {
                fileName = "input.bin";
            }

            var dest = Path.Combine(stagingDirectory, fileName);
            dest = EnsureUniquePath(dest);

            const int bufferSize = 1024 * 1024;
            using (var source = new FileStream(inputFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, bufferSize, useAsync: true))
            using (var target = new FileStream(dest, FileMode.CreateNew, FileAccess.Write, FileShare.None, bufferSize, useAsync: true))
            {
                await source.CopyToAsync(target, bufferSize, cancellationToken).ConfigureAwait(false);
            }

            return dest;
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
