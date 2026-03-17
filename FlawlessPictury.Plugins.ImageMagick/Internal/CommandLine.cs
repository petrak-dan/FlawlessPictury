using System;
using System.Text;

namespace FlawlessPictury.Plugins.ImageMagick.Internal
{
    /// <summary>
    /// Minimal command-line quoting helper (C# 7.3 safe).
    /// </summary>
    internal static class CommandLine
    {
        public static string Quote(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return "\"\"";
            }

            if (!NeedsQuotes(value))
            {
                return value;
            }

            var sb = new StringBuilder();
            sb.Append('"');

            for (int i = 0; i < value.Length; i++)
            {
                var ch = value[i];
                if (ch == '"')
                {
                    // Emit an escaped quote sequence: \"
                    sb.Append("\\\"");
                }
                else
                {
                    sb.Append(ch);
                }
            }

            sb.Append('"');
            return sb.ToString();
        }

        private static bool NeedsQuotes(string value)
        {
            for (int i = 0; i < value.Length; i++)
            {
                var ch = value[i];
                if (char.IsWhiteSpace(ch) || ch == '"')
                {
                    return true;
                }
            }

            return false;
        }
    }
}
