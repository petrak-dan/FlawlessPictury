using System;
using System.IO;

namespace FlawlessPictury.Plugins.ImageMagick.Internal
{
    internal static class TempName
    {
        public static string NewFile(string directory, string prefix, string extension)
        {
            if (string.IsNullOrWhiteSpace(directory))
            {
                directory = Path.GetTempPath();
            }

            if (string.IsNullOrWhiteSpace(prefix))
            {
                prefix = "fp";
            }

            if (string.IsNullOrWhiteSpace(extension))
            {
                extension = ".tmp";
            }

            if (!extension.StartsWith("."))
            {
                extension = "." + extension;
            }

            var name = prefix + "_" + Guid.NewGuid().ToString("N") + extension;
            return Path.Combine(directory, name);
        }
    }
}
