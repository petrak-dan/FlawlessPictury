using System;

namespace FlawlessPictury.AppCore.Plugins
{
    /// <summary>
    /// Identifies and describes a plugin assembly.
    /// </summary>
    public sealed class PluginMetadata
    {
        /// <summary>
        /// Initializes metadata.
        /// </summary>
        /// <param name="pluginId">Stable plugin id (e.g., "imagemagick").</param>
        /// <param name="displayName">User-visible name.</param>
        /// <param name="version">Plugin version.</param>
        public PluginMetadata(string pluginId, string displayName, Version version)
        {
            if (string.IsNullOrWhiteSpace(pluginId)) throw new ArgumentNullException(nameof(pluginId));
            if (string.IsNullOrWhiteSpace(displayName)) throw new ArgumentNullException(nameof(displayName));
            if (version == null) throw new ArgumentNullException(nameof(version));

            PluginId = pluginId;
            DisplayName = displayName;
            Version = version;
        }

        /// <summary>Stable plugin id.</summary>
        public string PluginId { get; }

        /// <summary>User-visible name.</summary>
        public string DisplayName { get; }

        /// <summary>Plugin version.</summary>
        public Version Version { get; }

        /// <summary>Optional description for UI.</summary>
        public string Description { get; set; }

        /// <summary>Optional author/vendor.</summary>
        public string Author { get; set; }

        /// <summary>
        /// Optional host contract version range string (e.g., ">=1.0 <2.0").
        /// </summary>
        /// <remarks>
        /// Hosts may treat this as informational until version enforcement is implemented.
        /// </remarks>
        public string HostContractRange { get; set; }
    }
}
