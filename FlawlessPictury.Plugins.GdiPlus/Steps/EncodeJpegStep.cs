using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FlawlessPictury.AppCore.Common;
using FlawlessPictury.AppCore.Plugins.Artifacts;
using FlawlessPictury.AppCore.Plugins.Execution;
using FlawlessPictury.AppCore.Plugins.Parameters;

namespace FlawlessPictury.Plugins.GdiPlus.Steps
{
    /// <summary>
    /// JPEG encoder step based on System.Drawing codecs.
    /// </summary>
    public sealed class EncodeJpegStep : IStep
    {
        private readonly ParameterValues _parameters;

        public EncodeJpegStep(ParameterValues parameters)
        {
            _parameters = parameters ?? new ParameterValues();
        }

        public Task<Result<StepOutput>> ExecuteAsync(
            StepInput input,
            StepContext context,
            IProgress<StepProgress> progress,
            CancellationToken cancellationToken)
        {
            if (input == null || input.Primary == null)
            {
                return Task.FromResult(Result<StepOutput>.Fail(Error.Validation("No input artifact was provided.")));
            }

            if (context == null)
            {
                return Task.FromResult(Result<StepOutput>.Fail(Error.Validation("No step context was provided.")));
            }

            var inPath = input.Primary.Locator;

            if (string.IsNullOrWhiteSpace(inPath) || !File.Exists(inPath))
            {
                return Task.FromResult(Result<StepOutput>.Fail(Error.Validation("Input file does not exist.")));
            }

            if (string.IsNullOrWhiteSpace(context.OutputDirectory))
            {
                return Task.FromResult(Result<StepOutput>.Fail(Error.Validation("No output directory is configured.")));
            }

            // Read parameters
            var quality = _parameters.GetInt32("quality", 85);
            if (quality < 1) quality = 1;
            if (quality > 100) quality = 100;

            var suffix = _parameters.GetString("suffix", "_jpg") ?? "_jpg";

            try
            {
                Directory.CreateDirectory(context.OutputDirectory);
            }
            catch (Exception ex)
            {
                context.Logger.Log(AppCore.CrossCutting.LogLevel.Error, "Failed to create output directory.", ex);
                return Task.FromResult(Result<StepOutput>.Fail(new Error("io", "Failed to create output directory.")));
            }

            cancellationToken.ThrowIfCancellationRequested();

            progress?.Report(new StepProgress(0, "Encoding JPEG..."));

            // Build output path (unique within output directory)
            var baseName = Path.GetFileNameWithoutExtension(inPath);
            if (string.IsNullOrWhiteSpace(baseName))
            {
                baseName = "image";
            }

            var outName = baseName + suffix + ".jpg";
            var outPath = MakeUnique(Path.Combine(context.OutputDirectory, outName));

            try
            {
                using (var fs = new FileStream(inPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                using (var src = Image.FromStream(fs, useEmbeddedColorManagement: false, validateImageData: false))
                using (var bmp = new Bitmap(src))
                {
                    // Best-effort metadata copy
                    try
                    {
                        foreach (var pi in src.PropertyItems)
                        {
                            bmp.SetPropertyItem(pi);
                        }
                    }
                    catch
                    {
                        // Ignore property copy issues.
                    }

                    var jpegCodec = GetJpegCodec();
                    if (jpegCodec == null)
                    {
                        return Task.FromResult(Result<StepOutput>.Fail(Error.NotSupported("JPEG encoder not available on this system.")));
                    }

                    using (var encParams = new EncoderParameters(1))
                    {
                        encParams.Param[0] = new EncoderParameter(System.Drawing.Imaging.Encoder.Quality, (long)quality);
                        bmp.Save(outPath, jpegCodec, encParams);
                    }
                }

                progress?.Report(new StepProgress(100, "JPEG encoded."));

                var createdUtc = context.Clock != null ? context.Clock.UtcNow : DateTime.UtcNow;

                var produced = new Artifact(Guid.NewGuid(), "image/jpeg", outPath, createdUtc);

                var output = new StepOutput();
                output.OutputArtifacts.Add(produced);

                try
                {
                    output.Metrics["output_bytes"] = new FileInfo(outPath).Length;
                    output.Metrics["jpeg_quality"] = quality;
                }
                catch
                {
                }

                context.Logger.Log(AppCore.CrossCutting.LogLevel.Info, "Encoded JPEG: " + outPath);

                return Task.FromResult(Result<StepOutput>.Ok(output));
            }
            catch (OperationCanceledException)
            {
                return Task.FromResult(Result<StepOutput>.Fail(new Error("canceled", "Operation canceled.")));
            }
            catch (Exception ex)
            {
                context.Logger.Log(AppCore.CrossCutting.LogLevel.Error, "JPEG encoding failed.", ex);
                return Task.FromResult(Result<StepOutput>.Fail(new Error("encode", "JPEG encoding failed.", ex.Message)));
            }
        }

        private static ImageCodecInfo GetJpegCodec()
        {
            try
            {
                return ImageCodecInfo.GetImageEncoders()
                    .FirstOrDefault(c => string.Equals(c.MimeType, "image/jpeg", StringComparison.OrdinalIgnoreCase));
            }
            catch
            {
                return null;
            }
        }

        private static string MakeUnique(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return path;
            }

            if (!File.Exists(path))
            {
                return path;
            }

            var dir = Path.GetDirectoryName(path) ?? string.Empty;
            var name = Path.GetFileNameWithoutExtension(path) ?? "file";
            var ext = Path.GetExtension(path) ?? string.Empty;

            for (int i = 1; i <= 9999; i++)
            {
                var candidate = Path.Combine(dir, name + " (" + i + ")" + ext);
                if (!File.Exists(candidate))
                {
                    return candidate;
                }
            }

            return Path.Combine(dir, name + "_" + Guid.NewGuid().ToString("N") + ext);
        }
    }
}
