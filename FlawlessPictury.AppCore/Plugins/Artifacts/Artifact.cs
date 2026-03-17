using System;
using System.Collections.Generic;

namespace FlawlessPictury.AppCore.Plugins.Artifacts
{
    /// <summary>
    /// Represents a unit of content (often a file) that is processed by pipeline steps.
    /// </summary>
    public sealed class Artifact
    {
        private readonly HashSet<RepresentationKind> _availableRepresentations;

        /// <summary>
        /// Initializes a new <see cref="Artifact"/> instance.
        /// </summary>
        /// <param name="id">Stable id for tracing pipeline outputs.</param>
        /// <param name="mediaType">MIME/media type (e.g., "image/png").</param>
        /// <param name="locator">A locator for the content, typically a local file path.</param>
        /// <param name="createdUtc">Creation timestamp for diagnostics/provenance.</param>
        public Artifact(Guid id, string mediaType, string locator, DateTime createdUtc)
        {
            if (id == Guid.Empty) throw new ArgumentException("Artifact id must not be empty.", nameof(id));
            if (string.IsNullOrWhiteSpace(mediaType)) throw new ArgumentNullException(nameof(mediaType));
            if (string.IsNullOrWhiteSpace(locator)) throw new ArgumentNullException(nameof(locator));

            Id = id;
            MediaType = mediaType;
            Locator = locator;
            CreatedUtc = createdUtc;

            Metadata = new MetadataBag();
            _availableRepresentations = new HashSet<RepresentationKind>();

            // Artifacts created from a locator expose the file-path representation immediately.
            _availableRepresentations.Add(RepresentationKind.FilePath);
        }

        /// <summary>
        /// Gets the artifact identifier.
        /// </summary>
        public Guid Id { get; }

        /// <summary>
        /// Gets the MIME/media type (e.g., "image/jpeg", "audio/flac").
        /// </summary>
        public string MediaType { get; }

        /// <summary>
        /// Gets a locator string for the content, typically a file path.
        /// </summary>
        /// <remarks>
        /// This is intentionally a string to remain flexible (file path, URI, content-addressed store key, etc.).
        /// </remarks>
        public string Locator { get; }

        /// <summary>
        /// Gets the UTC creation time for this artifact instance.
        /// </summary>
        public DateTime CreatedUtc { get; }

        /// <summary>
        /// Gets the metadata bag associated with the artifact.
        /// </summary>
        public MetadataBag Metadata { get; }

        /// <summary>
        /// Gets the set of currently available representations for this artifact.
        /// </summary>
        /// <remarks>
        /// The runtime may add representations as they are materialized (e.g., decoded pixels).
        /// </remarks>
        public IReadOnlyCollection<RepresentationKind> AvailableRepresentations => _availableRepresentations;

        /// <summary>
        /// Adds a representation kind to the list of available representations.
        /// </summary>
        /// <param name="kind">Representation kind that is now available.</param>
        public void MarkRepresentationAvailable(RepresentationKind kind)
        {
            // Intent: Keep a simple in-memory set for planning/validation. The runtime stores the actual payload elsewhere.
            _availableRepresentations.Add(kind);
        }

        /// <summary>
        /// Checks whether a representation is available.
        /// </summary>
        public bool HasRepresentation(RepresentationKind kind)
        {
            return _availableRepresentations.Contains(kind);
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return $"{MediaType} ({Locator})";
        }

        /// <summary>
        /// Creates an artifact representing a local file path.
        /// </summary>
        /// <param name="filePath">Local file path.</param>
        /// <param name="mediaType">MIME/media type.</param>
        public static Artifact FromFilePath(string filePath, string mediaType)
        {
            // Convenience factory for common file-based workflows.
            return new Artifact(Guid.NewGuid(), mediaType, filePath, DateTime.UtcNow);
        }
    }
}
