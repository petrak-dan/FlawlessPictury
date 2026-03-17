using System;
using System.Collections.Generic;
using FlawlessPictury.AppCore.Plugins;
using FlawlessPictury.AppCore.Plugins.Capabilities;
using FlawlessPictury.AppCore.Plugins.Execution;
using FlawlessPictury.AppCore.Plugins.Parameters;
using FlawlessPictury.Plugins.Optimizer.Steps;

namespace FlawlessPictury.Plugins.Optimizer
{
    /// <summary>
    /// Provides orchestration capabilities that search or optimize parameters by invoking other capabilities.
    /// </summary>
    public sealed class OptimizerPlugin : IPlugin
    {
        /// <summary>
        /// Gets the stable plugin identifier.
        /// </summary>
        public const string PluginId = "optimizer";

        private static readonly PluginMetadata _meta = new PluginMetadata(
            pluginId: PluginId,
            displayName: "Optimizer Tools",
            version: new Version(0, 1, 0, 0))
        {
            Author = "FlawlessPictury",
            Description = "Orchestrator capabilities (search/optimize) that invoke other plugin capabilities."
        };

        /// <inheritdoc />
        public PluginMetadata GetMetadata()
        {
            return _meta;
        }

        /// <inheritdoc />
        public IEnumerable<CapabilityDescriptor> GetCapabilities()
        {
            yield return SearchParameterCapability.Build();
        }

        /// <inheritdoc />
        public IStep CreateStep(string capabilityId, ParameterValues parameters)
        {
            if (string.IsNullOrWhiteSpace(capabilityId))
            {
                throw new ArgumentNullException(nameof(capabilityId));
            }

            if (capabilityId == SearchParameterCapability.Id)
            {
                return new SearchParameterStep(parameters);
            }

            throw new NotSupportedException("Unknown capabilityId: " + capabilityId);
        }
    }
}
