using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Linq;
using FlawlessPictury.AppCore.Plugins.Artifacts;
using FlawlessPictury.AppCore.Plugins.View;

namespace FlawlessPictury.Plugins.GdiPlus.Internal
{
    internal static class GdiImageHelpers
    {
        public static ImageCodecInfo FindCodecByMimeType(string mimeType)
        {
            try
            {
                return ImageCodecInfo.GetImageEncoders().FirstOrDefault(c => string.Equals(c.MimeType, mimeType, StringComparison.OrdinalIgnoreCase));
            }
            catch
            {
                return null;
            }
        }

        public static ImageCodecInfo FindCodecByFormat(string format)
        {
            var normalized = NormalizeFormat(format);
            switch (normalized)
            {
                case "jpg":
                case "jpeg": return FindCodecByMimeType("image/jpeg");
                case "png": return FindCodecByMimeType("image/png");
                case "bmp": return FindCodecByMimeType("image/bmp");
                case "gif": return FindCodecByMimeType("image/gif");
                case "tif":
                case "tiff": return FindCodecByMimeType("image/tiff");
                default: return null;
            }
        }

        public static string NormalizeFormat(string format)
        {
            return (format ?? string.Empty).Trim().TrimStart('.').ToLowerInvariant();
        }

        public static string ResolveMediaType(string format)
        {
            switch (NormalizeFormat(format))
            {
                case "jpg":
                case "jpeg": return "image/jpeg";
                case "png": return "image/png";
                case "bmp": return "image/bmp";
                case "gif": return "image/gif";
                case "tif":
                case "tiff": return "image/tiff";
                default: return "application/octet-stream";
            }
        }

        public static string ResolveDefaultExtension(string format)
        {
            switch (NormalizeFormat(format))
            {
                case "jpeg": return ".jpg";
                case "jpg": return ".jpg";
                case "png": return ".png";
                case "bmp": return ".bmp";
                case "gif": return ".gif";
                case "tif":
                case "tiff": return ".tif";
                default: return ".bin";
            }
        }

        public static string MakeUnique(string path)
        {
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            {
                return path;
            }

            var dir = Path.GetDirectoryName(path) ?? string.Empty;
            var name = Path.GetFileNameWithoutExtension(path) ?? "file";
            var ext = Path.GetExtension(path) ?? string.Empty;
            for (var i = 1; i <= 9999; i++)
            {
                var candidate = Path.Combine(dir, name + " (" + i.ToString(CultureInfo.InvariantCulture) + ")" + ext);
                if (!File.Exists(candidate))
                {
                    return candidate;
                }
            }

            return Path.Combine(dir, name + "_" + Guid.NewGuid().ToString("N") + ext);
        }

        public static Artifact CloneArtifactWithPath(Artifact source, string mediaType, string path, DateTime createdUtc)
        {
            var artifact = new Artifact(Guid.NewGuid(), mediaType, path, createdUtc);
            if (source != null)
            {
                foreach (var kv in source.Metadata.ToDictionary())
                {
                    artifact.Metadata.Set(kv.Key, kv.Value);
                }
            }
            return artifact;
        }

        public static ImageViewData BuildPngViewData(Image image)
        {
            if (image == null)
            {
                return null;
            }

            using (var ms = new MemoryStream())
            {
                image.Save(ms, ImageFormat.Png);
                return new ImageViewData
                {
                    EncodedBytes = ms.ToArray(),
                    MediaType = "image/png",
                    Width = image.Width,
                    Height = image.Height
                };
            }
        }
    }
}
