using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using FlawlessPictury.AppCore.Common;
using FlawlessPictury.AppCore.Plugins.Artifacts;
using FlawlessPictury.AppCore.Plugins.Execution;
using FlawlessPictury.AppCore.Plugins.Parameters;

namespace FlawlessPictury.Plugins.Sample.Steps
{
    /// <summary>
    /// Copies the input file to a new output file location.
    /// </summary>
    public sealed class CopyFileStep : IStep
    {
        private readonly ParameterValues _parameters;

        /// <summary>
        /// Initializes the step with configured parameters.
        /// </summary>
        public CopyFileStep(ParameterValues parameters)
        {
            _parameters = parameters ?? new ParameterValues();
        }

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

            if (context == null)
            {
                return Task.FromResult(Result<StepOutput>.Fail(Error.Validation("StepContext is missing.")));
            }

            // Report initial progress.
            progress?.Report(new StepProgress(0, "Preparing copy..."));

            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                var sourcePath = input.Primary.Locator;
                if (string.IsNullOrWhiteSpace(sourcePath) || !File.Exists(sourcePath))
                {
                    return Task.FromResult(Result<StepOutput>.Fail(Error.Validation("Input file does not exist.", sourcePath)));
                }

                // Resolve parameters (prefer step definition values passed via StepInput.Parameters if set).
                var suffix = input.Parameters.GetString("suffix", _parameters.GetString("suffix", "_out"));
                var forceExt = input.Parameters.GetString("extension", _parameters.GetString("extension", ""));

                // Determine destination directory.
                var destDir = !string.IsNullOrWhiteSpace(context.OutputDirectory)
                    ? context.OutputDirectory
                    : Path.GetDirectoryName(sourcePath);

                if (string.IsNullOrWhiteSpace(destDir))
                {
                    return Task.FromResult(Result<StepOutput>.Fail(Error.Validation("Could not determine destination directory.")));
                }

                Directory.CreateDirectory(destDir);

                // Build destination file name.
                var baseName = Path.GetFileNameWithoutExtension(sourcePath);
                var ext = string.IsNullOrWhiteSpace(forceExt) ? Path.GetExtension(sourcePath) : forceExt;

                if (!string.IsNullOrWhiteSpace(ext) && !ext.StartsWith(".", StringComparison.Ordinal))
                {
                    ext = "." + ext;
                }

                var candidate = Path.Combine(destDir, baseName + suffix + ext);

                // Avoid overwriting existing files: append numeric counter.
                int counter = 1;
                while (File.Exists(candidate))
                {
                    candidate = Path.Combine(destDir, $"{baseName}{suffix}_{counter}{ext}");
                    counter++;
                }

                progress?.Report(new StepProgress(50, "Copying..."));

                File.Copy(sourcePath, candidate, false);

                progress?.Report(new StepProgress(100, "Copy complete."));

                var outArtifact = Artifact.FromFilePath(candidate, input.Primary.MediaType);

                var output = new StepOutput();
                output.OutputArtifacts.Add(outArtifact);

                return Task.FromResult(Result<StepOutput>.Ok(output));
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                context.Logger?.Log(AppCore.CrossCutting.LogLevel.Error, "CopyFileStep failed.", ex);
                return Task.FromResult(Result<StepOutput>.Fail(Error.NotSupported("Copy failed.", ex.Message)));
            }
        }
    }
}
