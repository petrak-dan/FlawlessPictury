using System;
using System.Globalization;
using System.Text.RegularExpressions;

namespace FlawlessPictury.Plugins.ImageMagick.Internal
{
    /// <summary>
    /// Parses a numeric metric value from ImageMagick output (stdout/stderr).
    ///
    /// For ImageMagick compare -metric <metric>, the metric is often emitted as a number on stderr.
    /// We conservatively take the last floating number found.
    /// </summary>
    internal static class MetricParser
    {
        private static readonly Regex NumberRegex = new Regex(
            @"([-+]?[0-9]*\.?[0-9]+([eE][-+]?[0-9]+)?)",
            RegexOptions.Compiled);

        public static double? TryParse(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return null;
            }

            try
            {
                var matches = NumberRegex.Matches(text);
                if (matches == null || matches.Count == 0)
                {
                    return null;
                }

                var token = matches[matches.Count - 1].Groups[1].Value;

                double value;
                if (double.TryParse(token, NumberStyles.Float, CultureInfo.InvariantCulture, out value))
                {
                    return value;
                }

                return null;
            }
            catch
            {
                return null;
            }
        }
    }
}
