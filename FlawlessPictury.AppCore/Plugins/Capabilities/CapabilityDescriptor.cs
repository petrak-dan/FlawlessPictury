using System;
using System.Collections.Generic;
using FlawlessPictury.AppCore.Plugins.Artifacts;
using FlawlessPictury.AppCore.Plugins.Parameters;

namespace FlawlessPictury.AppCore.Plugins.Capabilities
{
    /// <summary>
    /// Declares a plugin-provided capability, including formats supported and required representations.
    /// </summary>
    public sealed class CapabilityDescriptor
    {
        /// <summary>
        /// Initializes a new <see cref="CapabilityDescriptor"/> instance.
        /// </summary>
        /// <param name="id">Stable capability id unique within the plugin (e.g., "imagemagick.convert").</param>
        /// <param name="displayName">User-visible name.</param>
        /// <param name="kind">High-level operation kind.</param>
        public CapabilityDescriptor(string id, string displayName, OperationKind kind)
        {
            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentNullException(nameof(id));
            if (string.IsNullOrWhiteSpace(displayName)) throw new ArgumentNullException(nameof(displayName));

            Id = id;
            DisplayName = displayName;
            Kind = kind;

            InputMediaTypes = new List<string>();
            OutputMediaTypes = new List<string>();

            RequiredRepresentation = RepresentationKind.FilePath;
            ProducedRepresentation = RepresentationKind.FilePath;

            Effect = OperationEffect.ProducesNewArtifact;
            Parameters = new ParameterSchema();
            Roles = CapabilityRole.None;
        }

        /// <summary>Stable id unique within a plugin.</summary>
        public string Id { get; }

        /// <summary>User-visible name shown in UI.</summary>
        public string DisplayName { get; }

        /// <summary>Optional user-visible description for tooltips/help.</summary>
        public string Description { get; set; }

        /// <summary>High-level kind classification for grouping/policy.</summary>
        public OperationKind Kind { get; }

        /// <summary>
        /// Optional semantic role hints that help editors and orchestrators filter compatible capabilities.
        /// </summary>
        public CapabilityRole Roles { get; set; }

        /// <summary>
        /// Declared input media types accepted by this capability (e.g., "image/*", "image/png").
        /// </summary>
        /// <remarks>
        /// Media type strings are treated as patterns and matched conservatively.
        /// </remarks>
        public List<string> InputMediaTypes { get; }

        /// <summary>
        /// Declared output media types produced by this capability.
        /// </summary>
        public List<string> OutputMediaTypes { get; }

        /// <summary>
        /// Representation required by this capability.
        /// </summary>
        public RepresentationKind RequiredRepresentation { get; set; }

        /// <summary>
        /// Representation produced by this capability.
        /// </summary>
        public RepresentationKind ProducedRepresentation { get; set; }

        /// <summary>
        /// Declares whether the capability mutates content in-place or produces a new artifact.
        /// </summary>
        public OperationEffect Effect { get; set; }

        /// <summary>
        /// Declares parameter schema used for host-rendered configuration UI.
        /// </summary>
        public ParameterSchema Parameters { get; set; }

        /// <summary>
        /// Adds an input media type pattern.
        /// </summary>
        public CapabilityDescriptor AddInput(string mediaTypePattern)
        {
            if (!string.IsNullOrWhiteSpace(mediaTypePattern))
            {
                InputMediaTypes.Add(mediaTypePattern);
            }

            return this;
        }

        /// <summary>
        /// Adds an output media type pattern.
        /// </summary>
        public CapabilityDescriptor AddOutput(string mediaTypePattern)
        {
            if (!string.IsNullOrWhiteSpace(mediaTypePattern))
            {
                OutputMediaTypes.Add(mediaTypePattern);
            }

            return this;
        }

        /// <summary>
        /// Adds one or more semantic roles.
        /// </summary>
        public CapabilityDescriptor AddRole(CapabilityRole role)
        {
            Roles |= role;
            return this;
        }

        /// <summary>
        /// Returns true when the capability declares the supplied role.
        /// </summary>
        public bool HasRole(CapabilityRole role)
        {
            return (Roles & role) == role;
        }
    }
}
