using System;
using System.Collections.Generic;
using System.Linq;

namespace FlawlessPictury.Presentation.WinForms.Views
{
    /// <summary>
    /// Lightweight view model for displaying a preset in the runner preset list.
    /// </summary>
    /// <remarks>
    /// This type is separate from <c>PresetDefinition</c> and includes availability information so presets can be shown even when dependencies are missing.
    /// </remarks>
    public sealed class PresetListItem
    {
        /// <summary>
        /// Initializes a new <see cref="PresetListItem"/> instance.
        /// </summary>
        public PresetListItem(string presetId, string displayName, string description, bool isAvailable, IReadOnlyList<string> missingCapabilities)
        {
            PresetId = presetId ?? string.Empty;
            DisplayName = string.IsNullOrWhiteSpace(displayName) ? PresetId : displayName;
            Description = description ?? string.Empty;

            IsAvailable = isAvailable;
            MissingCapabilities = missingCapabilities == null ? new List<string>() : new List<string>(missingCapabilities);
        }

        /// <summary>Stable preset identifier (used internally to locate the PresetDefinition).</summary>
        public string PresetId { get; }

        /// <summary>Human-friendly name shown in the UI.</summary>
        public string DisplayName { get; }

        /// <summary>Optional description shown by the UI.</summary>
        public string Description { get; }

        /// <summary>True if all plugin capabilities referenced by the preset are currently available.</summary>
        public bool IsAvailable { get; }

        /// <summary>Missing capability identifiers (plugin/capability) that prevent running this preset.</summary>
        public IReadOnlyList<string> MissingCapabilities { get; }

        /// <inheritdoc />
        public override string ToString()
        {
            if (IsAvailable)
            {
                return DisplayName;
            }

            if (MissingCapabilities == null || MissingCapabilities.Count == 0)
            {
                return DisplayName + " (missing plugins)";
            }

            // Keep short for the ComboBox display.
            var first = MissingCapabilities[0];
            if (MissingCapabilities.Count == 1)
            {
                return DisplayName + " (missing: " + first + ")";
            }

            return DisplayName + " (missing: " + first + " …)";
        }
    }
}
