using System.Collections.Generic;
using FlawlessPictury.AppCore.Plugins;

namespace FlawlessPictury.Infrastructure.Plugins
{
    /// <summary>
    /// Represents the outcome of loading plugins from disk.
    /// </summary>
    public sealed class PluginLoadResult
    {
        /// <summary>
        /// Initializes a new result.
        /// </summary>
        public PluginLoadResult()
        {
            LoadedPlugins = new List<PluginMetadata>();
            Errors = new List<PluginLoadError>();
            Warnings = new List<string>();
        }

        /// <summary>Metadata of successfully loaded plugins.</summary>
        public List<PluginMetadata> LoadedPlugins { get; }

        /// <summary>Non-fatal errors encountered while loading.</summary>
        public List<PluginLoadError> Errors { get; }

        /// <summary>Warnings for the user (duplicates, missing folder, etc.).</summary>
        public List<string> Warnings { get; }

        /// <summary>True if no errors were recorded.</summary>
        public bool IsClean => Errors.Count == 0;
    }
}
