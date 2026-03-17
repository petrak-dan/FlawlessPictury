using System;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using FlawlessPictury.AppCore.Common;
using FlawlessPictury.AppCore.Plugins.Execution;
using FlawlessPictury.AppCore.Plugins.View;

namespace FlawlessPictury.Plugins.GdiPlus.Steps
{
    public sealed class ReadImagePropertiesStep : IStep
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
                    output.Properties.Add(new PropertyEntry { Name = "Width", Value = img.Width.ToString(CultureInfo.InvariantCulture), Group = "Image", Source = "GDI+" });
                    output.Properties.Add(new PropertyEntry { Name = "Height", Value = img.Height.ToString(CultureInfo.InvariantCulture), Group = "Image", Source = "GDI+" });
                    output.Properties.Add(new PropertyEntry { Name = "Horizontal Resolution", Value = img.HorizontalResolution.ToString("0.##", CultureInfo.InvariantCulture), Group = "Image", Source = "GDI+" });
                    output.Properties.Add(new PropertyEntry { Name = "Vertical Resolution", Value = img.VerticalResolution.ToString("0.##", CultureInfo.InvariantCulture), Group = "Image", Source = "GDI+" });
                    output.Properties.Add(new PropertyEntry { Name = "Pixel Format", Value = img.PixelFormat.ToString(), Group = "Image", Source = "GDI+" });
                    output.Properties.Add(new PropertyEntry { Name = "Raw Format", Value = img.RawFormat.ToString(), Group = "Image", Source = "GDI+" });
                    output.Properties.Add(new PropertyEntry { Name = "Frame Dimensions", Value = img.FrameDimensionsList == null ? "0" : img.FrameDimensionsList.Length.ToString(CultureInfo.InvariantCulture), Group = "Image", Source = "GDI+" });
                    return Task.FromResult(Result<StepOutput>.Ok(output));
                }
            }
            catch (Exception ex)
            {
                return Task.FromResult(Result<StepOutput>.Fail(new Error("inspect", "Failed to read GDI+ image properties.", ex.Message)));
            }
        }
    }
}
