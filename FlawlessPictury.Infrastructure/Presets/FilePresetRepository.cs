using System;
using System.Collections.Generic;
using System.IO;
using FlawlessPictury.AppCore.Common;
using FlawlessPictury.AppCore.CrossCutting;
using FlawlessPictury.AppCore.Presets;

namespace FlawlessPictury.Infrastructure.Presets
{
    /// <summary>
    /// Loads and saves presets from JSON files in a directory.
    /// </summary>
    public sealed class FilePresetRepository : IPresetRepository
    {
        private readonly string _directory;

        public FilePresetRepository(string directory)
        {
            _directory = directory;
        }

        public string DirectoryPath => _directory;

        public IReadOnlyList<PresetDefinition> LoadPresets(ILogger logger)
        {
            var list = new List<PresetDefinition>();

            if (string.IsNullOrWhiteSpace(_directory))
            {
                return list;
            }

            if (!Directory.Exists(_directory))
            {
                return list;
            }

            string[] files;
            try
            {
                files = Directory.GetFiles(_directory, "*.json");
            }
            catch (Exception ex)
            {
                logger?.Log(LogLevel.Warn, "Failed to enumerate presets in: " + _directory, ex);
                return list;
            }

            Array.Sort(files, StringComparer.OrdinalIgnoreCase);

            var byId = new Dictionary<string, PresetDefinition>(StringComparer.OrdinalIgnoreCase);
            for (var i = 0; i < files.Length; i++)
            {
                var preset = TryLoadOne(files[i], logger);
                if (preset == null)
                {
                    continue;
                }

                if (byId.ContainsKey(preset.PresetId))
                {
                    logger?.Log(LogLevel.Warn, "Duplicate preset id '" + preset.PresetId + "' found. Later file overrides earlier one.");
                }

                byId[preset.PresetId] = preset;
            }

            foreach (var kv in byId)
            {
                list.Add(kv.Value);
            }

            return list;
        }

        public Result<string> SavePreset(PresetDefinition preset, string targetPath, ILogger logger)
        {
            if (preset == null)
            {
                return Result<string>.Fail(Error.Validation("Preset is required."));
            }

            if (string.IsNullOrWhiteSpace(targetPath))
            {
                return Result<string>.Fail(Error.Validation("Target path is required."));
            }

            try
            {
                PresetJsonSerializer.SaveToFile(preset, targetPath);
                preset.SourcePath = targetPath;
                return Result<string>.Ok(targetPath);
            }
            catch (Exception ex)
            {
                logger?.Log(LogLevel.Error, "Failed to save preset: " + targetPath, ex);
                return Result<string>.Fail(Error.NotSupported("Failed to save preset.", ex.Message));
            }
        }

        public Result DeletePreset(PresetDefinition preset, ILogger logger)
        {
            if (preset == null)
            {
                return Result.Fail(Error.Validation("Preset is required."));
            }

            if (string.IsNullOrWhiteSpace(preset.SourcePath))
            {
                return Result.Fail(Error.Validation("Preset has no source path."));
            }

            try
            {
                if (File.Exists(preset.SourcePath))
                {
                    File.Delete(preset.SourcePath);
                }

                return Result.Ok();
            }
            catch (Exception ex)
            {
                logger?.Log(LogLevel.Error, "Failed to delete preset: " + preset.SourcePath, ex);
                return Result.Fail(Error.NotSupported("Failed to delete preset.", ex.Message));
            }
        }

        private PresetDefinition TryLoadOne(string path, ILogger logger)
        {
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            {
                return null;
            }

            try
            {
                var preset = PresetJsonSerializer.LoadFromFile(path);
                if (preset == null)
                {
                    logger?.Log(LogLevel.Warn, "Preset JSON was invalid or incomplete (skipping): " + Path.GetFileName(path));
                    return null;
                }

                return preset;
            }
            catch (Exception ex)
            {
                logger?.Log(LogLevel.Warn, "Invalid preset JSON (skipping): " + Path.GetFileName(path), ex);
                return null;
            }
        }
    }
}
