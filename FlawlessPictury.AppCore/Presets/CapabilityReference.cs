using System;

namespace FlawlessPictury.AppCore.Presets
{
    /// <summary>
    /// Simple plugin/capability reference used by preset-owned provider selections.
    /// </summary>
    public sealed class CapabilityReference
    {
        public CapabilityReference()
        {
        }

        public CapabilityReference(string pluginId, string capabilityId)
        {
            PluginId = pluginId;
            CapabilityId = capabilityId;
        }

        public string PluginId { get; set; }
        public string CapabilityId { get; set; }

        public bool IsEmpty => string.IsNullOrWhiteSpace(PluginId) || string.IsNullOrWhiteSpace(CapabilityId);

        public CapabilityReference Clone()
        {
            return new CapabilityReference(PluginId, CapabilityId);
        }

        public override string ToString()
        {
            return string.IsNullOrWhiteSpace(PluginId) || string.IsNullOrWhiteSpace(CapabilityId)
                ? string.Empty
                : (PluginId + "/" + CapabilityId);
        }
    }
}
