using System;
using System.Collections.Generic;
using System.Globalization;
using FlawlessPictury.AppCore.Plugins.Parameters;

namespace FlawlessPictury.AppCore.Plugins.Pipeline
{
    /// <summary>
    /// Represents a single pipeline step definition referencing a plugin capability.
    /// </summary>
    public sealed class PipelineStepDefinition
    {
        /// <summary>
        /// Initializes a step definition.
        /// </summary>
        /// <param name="pluginId">Plugin id providing the capability.</param>
        /// <param name="capabilityId">Capability id within the plugin.</param>
        public PipelineStepDefinition(string pluginId, string capabilityId)
        {
            if (string.IsNullOrWhiteSpace(pluginId)) throw new ArgumentNullException(nameof(pluginId));
            if (string.IsNullOrWhiteSpace(capabilityId)) throw new ArgumentNullException(nameof(capabilityId));

            PluginId = pluginId;
            CapabilityId = capabilityId;

            Parameters = new ParameterValues();
            ChildSteps = new List<PipelineStepDefinition>();
            ReferenceAnchor = ReferenceAnchorKind.None;
            StepId = Guid.NewGuid().ToString("N", CultureInfo.InvariantCulture);
        }

        /// <summary>Stable step identifier used by editors/UI selection tracking.</summary>
        public string StepId { get; set; }

        /// <summary>Plugin id providing the capability.</summary>
        public string PluginId { get; }

        /// <summary>Capability id within the plugin.</summary>
        public string CapabilityId { get; }

        /// <summary>
        /// Optional semantic slot used by orchestrators to bind mandatory child steps
        /// (for example "encoder" or "metric").
        /// </summary>
        public string SlotKey { get; set; }

        /// <summary>
        /// Optional user-visible name (useful when the same capability appears multiple times).
        /// </summary>
        public string DisplayName { get; set; }

        /// <summary>Parameters for this step instance.</summary>
        public ParameterValues Parameters { get; set; }

        /// <summary>
        /// Optional mandatory child steps owned by this step definition.
        /// These are not part of the normal top-level linear pipeline order.
        /// </summary>
        public List<PipelineStepDefinition> ChildSteps { get; }

        /// <summary>
        /// Which artifact should be passed as Reference to the step.
        /// </summary>
        public ReferenceAnchorKind ReferenceAnchor { get; set; }

        public override string ToString()
        {
            if (!string.IsNullOrWhiteSpace(DisplayName))
            {
                return DisplayName;
            }

            return PluginId + "/" + CapabilityId;
        }
    }
}
