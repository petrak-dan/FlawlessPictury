using System.Collections.Generic;
using FlawlessPictury.AppCore.Plugins.Capabilities;
using FlawlessPictury.AppCore.Plugins.Execution;
using FlawlessPictury.AppCore.Plugins.Parameters;

namespace FlawlessPictury.AppCore.Plugins
{
    /// <summary>
    /// Represents a plugin that can provide one or more pipeline capabilities.
    /// </summary>
    public interface IPlugin
    {
        /// <summary>
        /// Gets plugin identity and display metadata.
        /// </summary>
        PluginMetadata GetMetadata();

        /// <summary>
        /// Enumerates capabilities available in this plugin.
        /// </summary>
        IEnumerable<CapabilityDescriptor> GetCapabilities();

        /// <summary>
        /// Creates a runnable step instance for a selected capability id.
        /// </summary>
        /// <param name="capabilityId">The capability id from <see cref="CapabilityDescriptor.Id"/>.</param>
        /// <param name="parameters">Configured parameter values (may be empty).</param>
        IStep CreateStep(string capabilityId, ParameterValues parameters);
    }
}
