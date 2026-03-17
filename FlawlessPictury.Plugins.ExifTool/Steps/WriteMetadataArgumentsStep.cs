using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using FlawlessPictury.AppCore.Common;
using FlawlessPictury.AppCore.Plugins.Execution;
using FlawlessPictury.Plugins.ExifTool.Internal;

namespace FlawlessPictury.Plugins.ExifTool.Steps
{
    public sealed class WriteMetadataArgumentsStep : IStep
    {
        private readonly AppCore.Plugins.Parameters.ParameterValues _parameters;

        public WriteMetadataArgumentsStep(AppCore.Plugins.Parameters.ParameterValues parameters)
        {
            _parameters = parameters ?? new AppCore.Plugins.Parameters.ParameterValues();
        }

        public async Task<Result<StepOutput>> ExecuteAsync(StepInput input, StepContext context, IProgress<StepProgress> progress, CancellationToken cancellationToken)
        {
            if (input == null || input.Primary == null)
            {
                return Result<StepOutput>.Fail(Error.Validation("Primary artifact is required."));
            }
            if (context == null || context.ProcessRunner == null)
            {
                return Result<StepOutput>.Fail(Error.Validation("ExifTool requires a process runner."));
            }

            var path = input.Primary.Locator;
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            {
                return Result<StepOutput>.Fail(Error.Validation("Target file does not exist."));
            }

            var executablePath = _parameters.GetString("executablePath", "exiftool");
            var rawArguments = _parameters.GetString("arguments", string.Empty);
            if (string.IsNullOrWhiteSpace(rawArguments))
            {
                return Result<StepOutput>.Fail(Error.Validation("ExifTool arguments are required."));
            }

            var process = await ExifToolInvoker.RunAsync(context.ProcessRunner, context.Environment, executablePath, ExifToolInvoker.BuildApplyArguments(rawArguments, path), cancellationToken).ConfigureAwait(false);
            if (process.IsFailure)
            {
                return Result<StepOutput>.Fail(process.Error);
            }
            if (process.Value.ExitCode != 0)
            {
                return Result<StepOutput>.Fail(Error.NotSupported("ExifTool metadata write failed.", (process.Value.StandardError ?? process.Value.StandardOutput ?? string.Empty).Trim()));
            }

            var output = new StepOutput();
            var artifact = new AppCore.Plugins.Artifacts.Artifact(Guid.NewGuid(), input.Primary.MediaType, path, context.Clock == null ? DateTime.UtcNow : context.Clock.UtcNow);
            foreach (var kv in input.Primary.Metadata.ToDictionary())
            {
                artifact.Metadata.Set(kv.Key, kv.Value);
            }
            output.OutputArtifacts.Add(artifact);
            output.Metrics["metadataWriteBackend"] = "ExifTool";
            return Result<StepOutput>.Ok(output);
        }
    }
}
