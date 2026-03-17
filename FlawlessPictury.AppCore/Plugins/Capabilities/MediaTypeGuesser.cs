using System;
using System.Collections.Generic;
using System.IO;

namespace FlawlessPictury.AppCore.Plugins.Capabilities
{
    /// <summary>
    /// Guesses a file's media type based on its extension.
    /// </summary>
    public static class MediaTypeGuesser
    {
        private static readonly Dictionary<string, string> ExtensionToMediaType =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { ".jpg", "image/jpeg" },
                { ".jpeg", "image/jpeg" },
                { ".jpe", "image/jpeg" },
                { ".png", "image/png" },
                { ".gif", "image/gif" },
                { ".bmp", "image/bmp" },
                { ".tif", "image/tiff" },
                { ".tiff", "image/tiff" },
                { ".webp", "image/webp" },

                // Additional media types supported by the built-in guesser.
                { ".mp3", "audio/mpeg" },
                { ".wav", "audio/wav" },
                { ".flac", "audio/flac" },
                { ".ogg", "audio/ogg" },
                { ".m4a", "audio/mp4" },

                { ".mp4", "video/mp4" },
                { ".mkv", "video/x-matroska" },
                { ".mov", "video/quicktime" },

                { ".pdf", "application/pdf" },
                { ".txt", "text/plain" }
            };

        /// <summary>
        /// Returns a guessed media type based on file extension.
        /// </summary>
        /// <param name="filePath">File path used to extract extension.</param>
        public static string GuessFromFilePath(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                return "application/octet-stream";
            }

            string ext = null;
            try
            {
                ext = Path.GetExtension(filePath);
            }
            catch
            {
                return "application/octet-stream";
            }

            if (string.IsNullOrWhiteSpace(ext))
            {
                return "application/octet-stream";
            }

            string mt;
            return ExtensionToMediaType.TryGetValue(ext, out mt)
                ? mt
                : "application/octet-stream";
        }
    }
}
