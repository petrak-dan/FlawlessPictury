using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using FlawlessPictury.AppCore.Common;
using FlawlessPictury.AppCore.Plugins.Execution;
using FlawlessPictury.AppCore.Plugins.View;
using FlawlessPictury.Plugins.ExifTool.Internal;

namespace FlawlessPictury.Plugins.ExifTool.Steps
{
    public sealed class ReadMetadataPropertiesStep : IStep
    {
        private readonly AppCore.Plugins.Parameters.ParameterValues _parameters;

        public ReadMetadataPropertiesStep(AppCore.Plugins.Parameters.ParameterValues parameters)
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
                return Result<StepOutput>.Fail(Error.Validation("Input file does not exist."));
            }

            var executablePath = _parameters.GetString("executablePath", "exiftool");
            var args = ExifToolInvoker.BuildPropertyReadArguments(path);
            var process = await ExifToolInvoker.RunAsync(context.ProcessRunner, context.Environment, executablePath, args, cancellationToken).ConfigureAwait(false);
            if (process.IsFailure)
            {
                return Result<StepOutput>.Fail(process.Error);
            }

            var run = process.Value;
            if (run.ExitCode != 0)
            {
                return Result<StepOutput>.Fail(Error.NotSupported("ExifTool property read failed.", (run.StandardError ?? run.StandardOutput ?? string.Empty).Trim()));
            }

            var output = new StepOutput();
            var text = string.IsNullOrWhiteSpace(run.StandardOutput) ? run.StandardError : run.StandardOutput;
            var lines = (text ?? string.Empty).Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);
            for (var i = 0; i < lines.Length; i++)
            {
                var line = lines[i];
                var colonIndex = line.IndexOf(':');
                if (colonIndex <= 0)
                {
                    continue;
                }
                var left = line.Substring(0, colonIndex).Trim();
                var value = line.Substring(colonIndex + 1).Trim();
                var group = string.Empty;
                var name = left;
                if (left.StartsWith("[") && left.Contains("]"))
                {
                    var end = left.IndexOf(']');
                    group = left.Substring(1, end - 1).Trim();
                    name = left.Substring(end + 1).Trim();
                }
                output.Properties.Add(new PropertyEntry
                {
                    Group = string.IsNullOrWhiteSpace(group) ? "ExifTool" : group,
                    Name = name,
                    Value = value,
                    Source = "ExifTool"
                });
            }
            return Result<StepOutput>.Ok(output);
        }
    }
}
