using System;

namespace FlawlessPictury.AppCore.Plugins.Capabilities
{
    /// <summary>
    /// Optional semantic roles advertised by a capability.
    /// </summary>
    [Flags]
    public enum CapabilityRole
    {
        /// <summary>No special role is declared.</summary>
        None = 0,

        /// <summary>
        /// Capability can be used by generic optimization/search orchestrators as an encoder/transformer.
        /// </summary>
        OptimizationEncoder = 1,

        /// <summary>
        /// Capability can be used by generic optimization/search orchestrators as a metric/analyzer.
        /// </summary>
        OptimizationMetric = 2,

        /// <summary>
        /// Capability can produce a host-renderable image view directly.
        /// </summary>
        PreviewProvider = 4,

        /// <summary>
        /// Capability can produce user-visible property entries for the selected artifact.
        /// </summary>
        PropertiesProvider = 8,
    }
}
