using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using FlawlessPictury.AppCore.Common;
using FlawlessPictury.AppCore.CrossCutting;
using FlawlessPictury.AppCore.Plugins.Execution;
using FlawlessPictury.Plugins.ExifTool.Internal;

namespace FlawlessPictury.Plugins.ExifTool.Steps
{
    public sealed class CopyMetadataFromOriginalStep : IStep
    {
        private readonly AppCore.Plugins.Parameters.ParameterValues _parameters;

        public CopyMetadataFromOriginalStep(AppCore.Plugins.Parameters.ParameterValues parameters)
        {
            _parameters = parameters ?? new AppCore.Plugins.Parameters.ParameterValues();
        }

        public async Task<Result<StepOutput>> ExecuteAsync(StepInput input, StepContext context, IProgress<StepProgress> progress, CancellationToken cancellationToken)
        {
            if (input == null || input.Primary == null)
            {
                return Result<StepOutput>.Fail(Error.Validation("Primary artifact is required."));
            }
            if (input.Original == null || string.IsNullOrWhiteSpace(input.Original.Locator) || !File.Exists(input.Original.Locator))
            {
                return Result<StepOutput>.Fail(Error.Validation("Original source artifact is required to copy metadata."));
            }
            if (context == null || context.ProcessRunner == null)
            {
                return Result<StepOutput>.Fail(Error.Validation("ExifTool requires a process runner."));
            }

            var sourcePath = input.Original.Locator;
            var targetPath = input.Primary.Locator;
            if (string.IsNullOrWhiteSpace(targetPath) || !File.Exists(targetPath))
            {
                return Result<StepOutput>.Fail(Error.Validation("Target artifact does not exist."));
            }

            progress?.Report(new StepProgress(10, "Copying metadata with ExifTool"));
            var excludeOrientationTag = _parameters.GetBoolean("excludeOrientationTag", false);
            var executablePath = _parameters.GetString("executablePath", "exiftool");
            var args = ExifToolInvoker.BuildCopyArguments(sourcePath, targetPath, excludeOrientationTag);
            var process = await ExifToolInvoker.RunAsync(context.ProcessRunner, context.Environment, executablePath, args, cancellationToken).ConfigureAwait(false);
            if (process.IsFailure)
            {
                return Result<StepOutput>.Fail(process.Error);
            }

            var run = process.Value;
            var output = new StepOutput();
            if (run.ExitCode != 0)
            {
                var detail = (run.StandardError ?? run.StandardOutput ?? string.Empty).Trim();
                var warningText = string.IsNullOrWhiteSpace(detail)
                    ? "Metadata could not be copied to the converted file format."
                    : "Metadata could not be copied to the converted file format. " + detail;

                context.Logger?.Log(LogLevel.Warn, warningText);
                output.Metrics["nonFatalWarning"] = true;
                output.Metrics["nonFatalWarningText"] = warningText;
            }

            var artifact = new AppCore.Plugins.Artifacts.Artifact(Guid.NewGuid(), input.Primary.MediaType, targetPath, context.Clock == null ? DateTime.UtcNow : context.Clock.UtcNow);
            foreach (var kv in input.Primary.Metadata.ToDictionary())
            {
                artifact.Metadata.Set(kv.Key, kv.Value);
            }
            output.OutputArtifacts.Add(artifact);
            output.Metrics["metadataCopyBackend"] = "ExifTool";
            if (excludeOrientationTag)
            {
                output.Metrics["metadataCopyExcludedOrientation"] = true;
            }
            if (!string.IsNullOrWhiteSpace(run.StandardError))
            {
                context.Logger?.Log(LogLevel.Warn, "ExifTool metadata copy warning: " + run.StandardError.Trim());
            }
            else if (!string.IsNullOrWhiteSpace(run.StandardOutput))
            {
                context.Logger?.Log(LogLevel.Debug, "ExifTool metadata copy: " + run.StandardOutput.Trim());
            }
            progress?.Report(new StepProgress(100, "Metadata copied"));
            return Result<StepOutput>.Ok(output);
        }
    }
}
