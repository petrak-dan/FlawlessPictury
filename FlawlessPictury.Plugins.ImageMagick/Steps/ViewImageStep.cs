using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using FlawlessPictury.AppCore.Common;
using FlawlessPictury.AppCore.Plugins.Execution;
using FlawlessPictury.AppCore.Plugins.View;
using FlawlessPictury.Plugins.ImageMagick.Internal;

namespace FlawlessPictury.Plugins.ImageMagick.Steps
{
    /// <summary>
    /// Renders an image preview using ImageMagick and returns encoded PNG bytes.
    /// </summary>
    public sealed class ViewImageStep : IStep
    {
        private readonly AppCore.Plugins.Parameters.ParameterValues _p;

        public ViewImageStep(AppCore.Plugins.Parameters.ParameterValues parameters)
        {
            _p = parameters ?? new AppCore.Plugins.Parameters.ParameterValues();
        }

        public async Task<Result<StepOutput>> ExecuteAsync(StepInput input, StepContext context, IProgress<StepProgress> progress, CancellationToken cancellationToken)
        {
            if (input == null || input.Primary == null)
            {
                return Result<StepOutput>.Fail(Error.Validation("Primary artifact is required."));
            }

            var path = input.Primary.Locator;
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            {
                return Result<StepOutput>.Fail(Error.Validation("Preview input file does not exist."));
            }

            progress?.Report(new StepProgress(10, "Rendering ImageMagick preview"));
            var exe = _p.GetString(ImageMagickPlugin.Keys.ExecutablePath, "magick");
            var bytesResult = await ImageMagickInvoker.RunPreviewPngAsync(
                context.ProcessRunner,
                context.Logger,
                context.Environment,
                exe,
                path,
                cancellationToken).ConfigureAwait(false);

            if (bytesResult.IsFailure)
            {
                return Result<StepOutput>.Fail(bytesResult.Error);
            }

            var bytes = bytesResult.Value;
            var output = new StepOutput();
            output.ImageView = new ImageViewData
            {
                EncodedBytes = bytes,
                MediaType = "image/png"
            };
            progress?.Report(new StepProgress(100, "Preview rendered"));
            return Result<StepOutput>.Ok(output);
        }
    }
}
