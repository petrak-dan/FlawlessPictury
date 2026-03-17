namespace FlawlessPictury.AppCore.Plugins.Artifacts
{
    /// <summary>
    /// Identifies a representation of an <see cref="Artifact"/> that a step can require or produce.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The host/runtime is responsible for "materializing" a required representation if possible
    /// (e.g., decoding an image into pixels, generating a preview, etc.).
    /// </para>
    /// <para>
    /// Most steps operate on <see cref="FilePath"/> (external tools) while
    /// some steps may operate on decoded representations.
    /// </para>
    /// </remarks>
    public enum RepresentationKind
    {
        /// <summary>
        /// A file path on local storage. This is the most interoperable representation for external tools.
        /// </summary>
        FilePath = 0,

        /// <summary>
        /// Raw bytes in memory. Prefer <see cref="FilePath"/> for large assets unless a step truly needs bytes.
        /// </summary>
        ByteArray = 1,

        /// <summary>
        /// A decoded raster image representation (pixels + color metadata). Host-defined DTO.
        /// </summary>
        DecodedImage = 2,

        /// <summary>
        /// A decoded audio representation (PCM + sample metadata). Host-defined DTO.
        /// </summary>
        DecodedAudio = 3,

        /// <summary>
        /// A lightweight preview image (e.g., downscaled bitmap). Used for interactive workflows.
        /// </summary>
        PreviewImage = 4,

        /// <summary>
        /// A lightweight preview audio representation (e.g., waveform segment).
        /// </summary>
        PreviewAudio = 5,

        /// <summary>
        /// Only metadata is available / required (EXIF, tags, file attributes). No heavy content materialization.
        /// </summary>
        MetadataOnly = 6
    }
}
