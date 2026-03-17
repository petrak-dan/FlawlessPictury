using System.Collections.Generic;
using FlawlessPictury.AppCore.CrossCutting;

namespace FlawlessPictury.AppCore.Presets
{
    /// <summary>
    /// Loads preset definitions from some storage (directory, embedded resources, database, ...).
    ///
    /// Important:
    /// - Repository should validate JSON structure and required fields.
    /// - Repository should NOT hide presets based on missing plugins/capabilities.
    ///   (Capability availability is checked by the host/presenter so presets remain visible and diagnostic messages are logged.)
    /// </summary>
    public interface IPresetRepository
    {
        /// <summary>
        /// Loads all preset definitions found in storage.
        /// Invalid preset files should be skipped and logged as warnings.
        /// </summary>
        IReadOnlyList<PresetDefinition> LoadPresets(ILogger logger);
    }
}
