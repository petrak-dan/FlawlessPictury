using System.Collections.Generic;
using FlawlessPictury.AppCore.Plugins.Capabilities;

namespace FlawlessPictury.AppCore.Plugins
{
    /// <summary>
    /// Provides access to loaded plugins and their declared capabilities.
    /// </summary>
    public interface IPluginCatalog
    {
        /// <summary>
        /// Gets all loaded plugins.
        /// </summary>
        IEnumerable<IPlugin> GetPlugins();

        /// <summary>
        /// Finds a plugin by its stable plugin id.
        /// </summary>
        /// <param name="pluginId">Stable plugin id (see <see cref="PluginMetadata.PluginId"/>).</param>
        /// <returns>The plugin instance, or null if not found.</returns>
        IPlugin FindPlugin(string pluginId);

        /// <summary>
        /// Finds a capability descriptor by plugin id and capability id.
        /// </summary>
        /// <param name="pluginId">Plugin id.</param>
        /// <param name="capabilityId">Capability id (see <see cref="CapabilityDescriptor.Id"/>).</param>
        /// <returns>The capability descriptor, or null if not found.</returns>
        CapabilityDescriptor FindCapability(string pluginId, string capabilityId);
    }
}
