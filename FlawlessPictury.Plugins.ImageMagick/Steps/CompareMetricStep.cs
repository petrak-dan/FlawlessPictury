using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using FlawlessPictury.AppCore.Common;
using FlawlessPictury.AppCore.Plugins.Execution;
using FlawlessPictury.AppCore.Plugins.Parameters;
using FlawlessPictury.Plugins.ImageMagick.Internal;

namespace FlawlessPictury.Plugins.ImageMagick.Steps
{
    /// <summary>
    /// Compares StepInput.Reference to StepInput.Primary using ImageMagick compare -metric <metric>.
    /// Outputs the numeric value into StepOutput.Metrics[metricKey].
    /// </summary>
    public sealed class CompareMetricStep : IStep
    {
        private readonly ParameterValues _p;

        public CompareMetricStep(ParameterValues parameters)
        {
            _p = parameters ?? new ParameterValues();
        }

        public async Task<Result<StepOutput>> ExecuteAsync(
            StepInput input,
            StepContext context,
            IProgress<StepProgress> progress,
            CancellationToken cancellationToken)
        {
            if (input == null) return Result<StepOutput>.Fail(Error.Validation("StepInput is null."));
            if (context == null) return Result<StepOutput>.Fail(Error.Validation("StepContext is null."));
            if (input.Primary == null) return Result<StepOutput>.Fail(Error.Validation("Primary artifact is null."));

            var cand = input.Primary.Locator;
            if (string.IsNullOrWhiteSpace(cand) || !File.Exists(cand))
            {
                return Result<StepOutput>.Fail(Error.Validation("Candidate file does not exist."));
            }

            var reference = (input.Reference ?? input.Primary).Locator;
            if (string.IsNullOrWhiteSpace(reference) || !File.Exists(reference))
            {
                reference = cand;
            }

            var exe = _p.GetString(ImageMagickPlugin.Keys.ExecutablePath, "magick");
            var metric = _p.GetString(ImageMagickPlugin.Keys.Metric, "SSIM");
            var key = _p.GetString(ImageMagickPlugin.Keys.MetricKey, "metric");

            var options = StepOptionBuilder.BuildCompareOptions(_p);

            var cmp = await ImageMagickInvoker.RunCompareMetricAsync(
                context.ProcessRunner,
                context.Logger,
                context.Environment,
                exe,
                metric,
                reference,
                cand,
                options,
                cancellationToken).ConfigureAwait(false);

            if (cmp.IsFailure)
            {
                return Result<StepOutput>.Fail(cmp.Error);
            }

            var output = new StepOutput();
            output.Metrics[key] = cmp.Value;
            output.Metrics["metricName"] = metric;

            return Result<StepOutput>.Ok(output);
        }
    }
}
