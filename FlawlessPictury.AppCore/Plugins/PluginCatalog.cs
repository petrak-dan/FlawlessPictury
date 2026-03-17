using System;
using System.Collections.Generic;
using FlawlessPictury.AppCore.Plugins.Capabilities;

namespace FlawlessPictury.AppCore.Plugins
{
    /// <summary>
    /// Simple in-memory catalog that stores plugins and their capability descriptors.
    /// </summary>
    public sealed class PluginCatalog : IPluginCatalog
    {
        private readonly Dictionary<string, IPlugin> _pluginsById;
        private readonly Dictionary<string, CapabilityDescriptor> _capabilitiesByKey;

        /// <summary>
        /// Initializes an empty plugin catalog.
        /// </summary>
        public PluginCatalog()
        {
            _pluginsById = new Dictionary<string, IPlugin>(StringComparer.OrdinalIgnoreCase);
            _capabilitiesByKey = new Dictionary<string, CapabilityDescriptor>(StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Registers a plugin instance and indexes its capabilities.
        /// </summary>
        /// <param name="plugin">Plugin to register.</param>
        public void Register(IPlugin plugin)
        {
            if (plugin == null) throw new ArgumentNullException(nameof(plugin));

            var meta = plugin.GetMetadata();
            if (meta == null) throw new InvalidOperationException("Plugin.GetMetadata() returned null.");

            // Replace existing registrations by plugin identifier.
            _pluginsById[meta.PluginId] = plugin;

            // Re-index capabilities for this plugin identifier.
            foreach (var cap in plugin.GetCapabilities())
            {
                if (cap == null)
                {
                    continue;
                }

                var key = MakeKey(meta.PluginId, cap.Id);
                _capabilitiesByKey[key] = cap;
            }
        }

        /// <inheritdoc />
        public IEnumerable<IPlugin> GetPlugins()
        {
            return _pluginsById.Values;
        }

        /// <inheritdoc />
        public IPlugin FindPlugin(string pluginId)
        {
            if (string.IsNullOrWhiteSpace(pluginId))
            {
                return null;
            }

            IPlugin plugin;
            return _pluginsById.TryGetValue(pluginId, out plugin) ? plugin : null;
        }

        /// <inheritdoc />
        public CapabilityDescriptor FindCapability(string pluginId, string capabilityId)
        {
            if (string.IsNullOrWhiteSpace(pluginId) || string.IsNullOrWhiteSpace(capabilityId))
            {
                return null;
            }

            CapabilityDescriptor cap;
            return _capabilitiesByKey.TryGetValue(MakeKey(pluginId, capabilityId), out cap) ? cap : null;
        }

        private static string MakeKey(string pluginId, string capabilityId)
        {
            // A single string key keeps indexing simple while avoiding nested dictionaries.
            return $"{pluginId}::{capabilityId}";
        }
    }
}
