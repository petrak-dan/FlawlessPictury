using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using FlawlessPictury.AppCore.Common;
using FlawlessPictury.AppCore.Plugins.Execution;

namespace FlawlessPictury.Plugins.Sample.Steps
{
    /// <summary>
    /// Emits basic file info metrics.
    /// </summary>
    public sealed class FileInfoAnalyzerStep : IStep
    {
        /// <inheritdoc />
        public Task<Result<StepOutput>> ExecuteAsync(
            StepInput input,
            StepContext context,
            IProgress<StepProgress> progress,
            CancellationToken cancellationToken)
        {
            if (input == null || input.Primary == null)
            {
                return Task.FromResult(Result<StepOutput>.Fail(Error.Validation("Primary artifact is missing.")));
            }

            progress?.Report(new StepProgress(null, "Reading file info..."));

            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                var path = input.Primary.Locator;
                if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
                {
                    return Task.FromResult(Result<StepOutput>.Fail(Error.Validation("Input file does not exist.", path)));
                }

                var fi = new FileInfo(path);

                var output = new StepOutput();
                output.Metrics["file_size_bytes"] = fi.Length;
                output.Metrics["last_write_utc"] = fi.LastWriteTimeUtc.ToString("o");

                // Analyzer: no output artifacts, so pipeline continues with same current artifact.
                progress?.Report(new StepProgress(100, "File info collected."));
                return Task.FromResult(Result<StepOutput>.Ok(output));
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                context.Logger?.Log(AppCore.CrossCutting.LogLevel.Error, "FileInfoAnalyzerStep failed.", ex);
                return Task.FromResult(Result<StepOutput>.Fail(Error.NotSupported("Analyzer failed.", ex.Message)));
            }
        }
    }
}
