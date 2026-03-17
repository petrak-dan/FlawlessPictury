using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using FlawlessPictury.AppCore.Common;
using FlawlessPictury.AppCore.CrossCutting;
using FlawlessPictury.AppCore.Plugins.Execution;
using FlawlessPictury.Plugins.GdiPlus.Internal;

namespace FlawlessPictury.Plugins.GdiPlus.Steps
{
    public sealed class TranscodeImageStep : IStep
    {
        private readonly AppCore.Plugins.Parameters.ParameterValues _parameters;

        public TranscodeImageStep(AppCore.Plugins.Parameters.ParameterValues parameters)
        {
            _parameters = parameters ?? new AppCore.Plugins.Parameters.ParameterValues();
        }

        public Task<Result<StepOutput>> ExecuteAsync(StepInput input, StepContext context, IProgress<StepProgress> progress, CancellationToken cancellationToken)
        {
            if (input == null || input.Primary == null)
            {
                return Task.FromResult(Result<StepOutput>.Fail(Error.Validation("No input artifact was provided.")));
            }

            var inputPath = input.Primary.Locator;
            if (string.IsNullOrWhiteSpace(inputPath) || !File.Exists(inputPath))
            {
                return Task.FromResult(Result<StepOutput>.Fail(Error.Validation("Input file does not exist.")));
            }

            var format = _parameters.GetString("format", "jpg");
            var codec = GdiImageHelpers.FindCodecByFormat(format);
            if (codec == null)
            {
                return Task.FromResult(Result<StepOutput>.Fail(Error.NotSupported("Requested output format is not supported by GDI+.")));
            }

            var suffix = _parameters.GetString("suffix", "_out") ?? "_out";
            var ext = GdiImageHelpers.ResolveDefaultExtension(format);
            var mediaType = GdiImageHelpers.ResolveMediaType(format);
            var quality = _parameters.GetInt32("quality", 85);
            if (quality < 1) quality = 1;
            if (quality > 100) quality = 100;

            Directory.CreateDirectory(context.OutputDirectory);
            var baseName = Path.GetFileNameWithoutExtension(inputPath) ?? "image";
            var outputPath = GdiImageHelpers.MakeUnique(Path.Combine(context.OutputDirectory, baseName + suffix + ext));
            progress?.Report(new StepProgress(5, "Transcoding image"));

            try
            {
                using (var fs = new FileStream(inputPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                using (var src = Image.FromStream(fs, false, false))
                using (var bmp = new Bitmap(src))
                {
                    TryCopyPropertyItems(src, bmp);

                    if (string.Equals(mediaType, "image/jpeg", StringComparison.OrdinalIgnoreCase))
                    {
                        using (var encParams = new EncoderParameters(1))
                        {
                            encParams.Param[0] = new EncoderParameter(Encoder.Quality, (long)quality);
                            bmp.Save(outputPath, codec, encParams);
                        }
                    }
                    else
                    {
                        bmp.Save(outputPath, codec, null);
                    }
                }

                var output = new StepOutput();
                var artifact = GdiImageHelpers.CloneArtifactWithPath(input.Primary, mediaType, outputPath, context.Clock == null ? DateTime.UtcNow : context.Clock.UtcNow);
                output.OutputArtifacts.Add(artifact);
                output.Metrics["output_bytes"] = new FileInfo(outputPath).Length;
                output.Metrics["format"] = GdiImageHelpers.NormalizeFormat(format);
                output.Metrics["quality"] = quality;
                progress?.Report(new StepProgress(100, "Image transcoded"));
                context.Logger?.Log(LogLevel.Info, "GDI+ transcoded image: " + outputPath);
                return Task.FromResult(Result<StepOutput>.Ok(output));
            }
            catch (Exception ex)
            {
                context.Logger?.Log(LogLevel.Error, "GDI+ image transcode failed.", ex);
                return Task.FromResult(Result<StepOutput>.Fail(new Error("encode", "GDI+ image transcode failed.", ex.Message)));
            }
        }

        private static void TryCopyPropertyItems(Image source, Image target)
        {
            try
            {
                foreach (var pi in source.PropertyItems)
                {
                    target.SetPropertyItem(pi);
                }
            }
            catch
            {
            }
        }
    }
}
