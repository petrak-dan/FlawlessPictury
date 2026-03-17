using System;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using FlawlessPictury.AppCore.Common;
using FlawlessPictury.AppCore.Plugins.Execution;
using FlawlessPictury.Plugins.GdiPlus.Internal;

namespace FlawlessPictury.Plugins.GdiPlus.Steps
{
    public sealed class ViewImageStep : IStep
    {
        public Task<Result<StepOutput>> ExecuteAsync(StepInput input, StepContext context, IProgress<StepProgress> progress, CancellationToken cancellationToken)
        {
            if (input == null || input.Primary == null)
            {
                return Task.FromResult(Result<StepOutput>.Fail(Error.Validation("Primary artifact is required.")));
            }

            var path = input.Primary.Locator;
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            {
                return Task.FromResult(Result<StepOutput>.Fail(Error.Validation("Image file does not exist.")));
            }

            try
            {
                using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                using (var img = Image.FromStream(fs, false, false))
                {
                    var output = new StepOutput();
                    output.ImageView = GdiImageHelpers.BuildPngViewData(img);
                    output.Metrics["width"] = img.Width;
                    output.Metrics["height"] = img.Height;
                    return Task.FromResult(Result<StepOutput>.Ok(output));
                }
            }
            catch (Exception ex)
            {
                return Task.FromResult(Result<StepOutput>.Fail(new Error("preview", "Failed to build GDI+ preview.", ex.Message)));
            }
        }
    }
}
