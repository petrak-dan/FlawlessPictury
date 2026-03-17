using System;
using System.Collections.Generic;
using FlawlessPictury.AppCore.Plugins.Pipeline;

namespace FlawlessPictury.AppCore.Presets
{
    /// <summary>
    /// Represents a runnable preset/profile for the application.
    /// </summary>
    public sealed class PresetDefinition
    {
        /// <summary>
        /// Initializes a new preset.
        /// </summary>
        public PresetDefinition(string presetId, string displayName, PipelineDefinition pipeline)
        {
            if (string.IsNullOrWhiteSpace(presetId))
            {
                throw new ArgumentException("PresetId is required.", nameof(presetId));
            }

            PresetId = presetId;
            DisplayName = string.IsNullOrWhiteSpace(displayName) ? presetId : displayName;
            Pipeline = pipeline ?? throw new ArgumentNullException(nameof(pipeline));
            OutputPolicy = OutputPolicyDefinition.CreateDefault();
            PropertiesProviderRefs = new List<CapabilityReference>();
        }

        public string PresetId { get; }
        public string DisplayName { get; }
        public string Description { get; set; }
        public PipelineDefinition Pipeline { get; }
        public OutputPolicyDefinition OutputPolicy { get; set; }
        public CapabilityReference PreviewProviderRef { get; set; }
        public List<CapabilityReference> PropertiesProviderRefs { get; }
        public string SourcePath { get; set; }
        public bool IsReadOnly { get; set; }

        public override string ToString()
        {
            return DisplayName;
        }
    }
}
