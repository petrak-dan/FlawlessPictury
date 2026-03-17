using System;
using System.Globalization;
using System.Text;
using FlawlessPictury.AppCore.Plugins.Parameters;

namespace FlawlessPictury.Plugins.ImageMagick.Steps
{
    /// <summary>
    /// Builds ImageMagick option strings from structured parameters.
    ///
    /// Why:
    /// - Keep step execution generic while still exposing a visible list of supported IM options.
    /// - Provide an "arguments" escape hatch for options not yet modeled as parameters.
    /// </summary>
    internal static class StepOptionBuilder
    {
        public static string BuildConvertOptions(ParameterValues p)
        {
            var sb = new StringBuilder();

            var strip = p.GetBoolean(ImageMagickPlugin.Keys.Strip, true);
            if (strip)
            {
                sb.Append("-strip ");
            }

            var autoOrient = p.GetBoolean(ImageMagickPlugin.Keys.AutoOrient, true);
            if (autoOrient)
            {
                sb.Append("-auto-orient ");
            }

            var resize = p.GetString(ImageMagickPlugin.Keys.Resize, null);
            if (!string.IsNullOrWhiteSpace(resize))
            {
                sb.Append("-resize ").Append(resize.Trim()).Append(' ');
            }

            var colorspace = p.GetString(ImageMagickPlugin.Keys.ColorSpace, null);
            if (!string.IsNullOrWhiteSpace(colorspace))
            {
                sb.Append("-colorspace ").Append(colorspace.Trim()).Append(' ');
            }

            var interlace = p.GetString(ImageMagickPlugin.Keys.Interlace, null);
            if (!string.IsNullOrWhiteSpace(interlace) && !string.Equals(interlace, "None", StringComparison.OrdinalIgnoreCase))
            {
                sb.Append("-interlace ").Append(interlace.Trim()).Append(' ');
            }

            // Quality is format-dependent; if present, we pass it through.
            var q = p.GetInt32(ImageMagickPlugin.Keys.Quality, 0);
            if (q > 0)
            {
                sb.Append("-quality ").Append(q.ToString(CultureInfo.InvariantCulture)).Append(' ');
            }

            var define = p.GetString(ImageMagickPlugin.Keys.Define, null);
            if (!string.IsNullOrWhiteSpace(define))
            {
                sb.Append("-define ").Append(define.Trim()).Append(' ');
            }

            // Raw escape hatch
            var raw = p.GetString(ImageMagickPlugin.Keys.Arguments, null);
            if (!string.IsNullOrWhiteSpace(raw))
            {
                sb.Append(raw.Trim()).Append(' ');
            }

            return sb.ToString().Trim();
        }

        public static string BuildCompareOptions(ParameterValues p)
        {
            // For now, only the raw 'arguments' is used for compare options, but we keep the same escape hatch.
            var raw = p.GetString(ImageMagickPlugin.Keys.Arguments, null);
            return string.IsNullOrWhiteSpace(raw) ? string.Empty : raw.Trim();
        }
    }
}
