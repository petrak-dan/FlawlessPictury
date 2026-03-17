using System;

namespace FlawlessPictury.AppCore.Plugins.View
{
    /// <summary>
    /// UI-agnostic encoded image payload that a host can render in its native UI layer.
    /// </summary>
    public sealed class ImageViewData
    {
        public byte[] EncodedBytes { get; set; }
        public string MediaType { get; set; }
        public int? Width { get; set; }
        public int? Height { get; set; }

        public bool HasImage => EncodedBytes != null && EncodedBytes.Length > 0;
    }
}
