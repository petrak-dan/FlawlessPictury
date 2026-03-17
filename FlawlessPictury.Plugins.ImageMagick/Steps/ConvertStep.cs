using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using FlawlessPictury.AppCore.Common;
using FlawlessPictury.AppCore.Plugins.Artifacts;
using FlawlessPictury.AppCore.Plugins.Execution;
using FlawlessPictury.AppCore.Plugins.Parameters;
using FlawlessPictury.Plugins.ImageMagick.Internal;

namespace FlawlessPictury.Plugins.ImageMagick.Steps
{
    /// <summary>
    /// Generic convert step backed by ImageMagick.
    ///
    /// Preset-driven behavior:
    /// - Output format comes from parameter 'format' and outputNamePattern (e.g., {name}.{format}).
    /// - Options are built from parameter list (strip/resize/quality/etc.) plus raw 'arguments'.
    /// </summary>
    public sealed class ConvertStep : IStep
    {
        private readonly ParameterValues _p;

        public ConvertStep(ParameterValues parameters)
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

            var inPath = input.Primary.Locator;
            if (string.IsNullOrWhiteSpace(inPath) || !File.Exists(inPath))
            {
                return Result<StepOutput>.Fail(Error.Validation("Input file does not exist."));
            }

            var exe = _p.GetString(ImageMagickPlugin.Keys.ExecutablePath, "magick");
            var format = _p.GetString(ImageMagickPlugin.Keys.Format, null);
            if (string.IsNullOrWhiteSpace(format))
            {
                return Result<StepOutput>.Fail(Error.Validation("Parameter 'format' is required."));
            }

            Directory.CreateDirectory(context.OutputDirectory);

            var baseName = Path.GetFileNameWithoutExtension(inPath);
            var outName = ExpandPattern(_p.GetString(ImageMagickPlugin.Keys.OutputNamePattern, "{name}.{format}"), baseName, format);
            var outPath = Path.Combine(context.OutputDirectory, outName);

            var options = StepOptionBuilder.BuildConvertOptions(_p);

            var conv = await ImageMagickInvoker.RunConvertAsync(
                context.ProcessRunner,
                context.Logger,
                context.Environment,
                exe,
                inPath,
                outPath,
                options,
                cancellationToken).ConfigureAwait(false);

            if (conv.IsFailure)
            {
                return Result<StepOutput>.Fail(conv.Error);
            }

            var art = Artifact.FromFilePath(outPath, GuessMediaTypeByExtension(format));
            art.Metadata.Set("im.op", "convert");
            art.Metadata.Set("im.format", format);

            var output = new StepOutput();
            output.OutputArtifacts.Add(art);
            return Result<StepOutput>.Ok(output);
        }

        private static string ExpandPattern(string pattern, string name, string format)
        {
            if (string.IsNullOrWhiteSpace(pattern))
            {
                pattern = "{name}.{format}";
            }

            var s = pattern.Replace("{name}", name ?? string.Empty);
            s = s.Replace("{format}", format ?? string.Empty);
            return s;
        }

        private static string GuessMediaTypeByExtension(string extOrFormat)
        {
            if (string.IsNullOrWhiteSpace(extOrFormat)) return "application/octet-stream";

            var f = extOrFormat.Trim().TrimStart('.').ToLowerInvariant();

            if (f == "jpg" || f == "jpeg") return "image/jpeg";
            if (f == "png") return "image/png";
            if (f == "webp") return "image/webp";
            if (f == "tif" || f == "tiff") return "image/tiff";
            if (f == "gif") return "image/gif";
            if (f == "bmp") return "image/bmp";

            return "image/*";
        }
    }
}
