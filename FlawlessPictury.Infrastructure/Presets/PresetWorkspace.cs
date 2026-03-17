using System;
using System.Collections.Generic;
using System.Linq;
using FlawlessPictury.AppCore.Common;
using FlawlessPictury.AppCore.CrossCutting;
using FlawlessPictury.AppCore.Plugins.Parameters;
using FlawlessPictury.AppCore.Plugins.Pipeline;
using FlawlessPictury.AppCore.Presets;

namespace FlawlessPictury.Infrastructure.Presets
{
    /// <summary>
    /// In-memory preset workspace shared by the Runner and Preset Editor.
    ///
    /// Behavior:
    /// - Presets are loaded from JSON once into memory.
    /// - Unsaved editor changes live only in memory until Save is clicked.
    /// - Running batches uses a cloned snapshot of the selected preset, so on-disk edits/deletes do not affect an active run.
    /// </summary>
    public sealed class PresetWorkspace
    {
        private readonly FilePresetRepository _repository;
        private readonly List<PresetDefinition> _presets;
        private readonly object _sync;
        private string _selectedPresetId;

        public PresetWorkspace(FilePresetRepository repository)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _presets = new List<PresetDefinition>();
            _sync = new object();
            _selectedPresetId = null;
        }

        public event EventHandler Changed;

        public string DirectoryPath => _repository.DirectoryPath;

        public IReadOnlyList<PresetDefinition> GetSnapshot()
        {
            lock (_sync)
            {
                return _presets.Select(ClonePreset).ToList();
            }
        }

        public PresetDefinition FindById(string presetId)
        {
            if (string.IsNullOrWhiteSpace(presetId))
            {
                return null;
            }

            lock (_sync)
            {
                var found = _presets.FirstOrDefault(p => string.Equals(p.PresetId, presetId, StringComparison.OrdinalIgnoreCase));
                return found == null ? null : ClonePreset(found);
            }
        }

        public PresetDefinition FindBySourcePath(string sourcePath)
        {
            if (string.IsNullOrWhiteSpace(sourcePath))
            {
                return null;
            }

            lock (_sync)
            {
                var found = _presets.FirstOrDefault(p => !string.IsNullOrWhiteSpace(p.SourcePath) && string.Equals(p.SourcePath, sourcePath, StringComparison.OrdinalIgnoreCase));
                return found == null ? null : ClonePreset(found);
            }
        }


        public string GetSelectedPresetId()
        {
            lock (_sync)
            {
                return _selectedPresetId;
            }
        }

        public void SetSelectedPresetId(string presetId)
        {
            lock (_sync)
            {
                _selectedPresetId = string.IsNullOrWhiteSpace(presetId) ? null : presetId.Trim();
            }
        }

        public Result Reload(ILogger logger)
        {
            try
            {
                var loaded = _repository.LoadPresets(logger) ?? new List<PresetDefinition>();

                lock (_sync)
                {
                    _presets.Clear();
                    for (var i = 0; i < loaded.Count; i++)
                    {
                        _presets.Add(ClonePreset(loaded[i]));
                    }
                }

                RaiseChanged();
                return Result.Ok();
            }
            catch (Exception ex)
            {
                logger?.Log(LogLevel.Error, "Failed to reload presets into workspace.", ex);
                return Result.Fail(Error.NotSupported("Failed to reload presets.", ex.Message));
            }
        }

        public void UpsertDraft(PresetDefinition preset)
        {
            if (preset == null)
            {
                return;
            }

            lock (_sync)
            {
                var replacement = ClonePreset(preset);
                var index = FindIndex_NoLock(replacement);
                if (index >= 0)
                {
                    _presets[index] = replacement;
                }
                else
                {
                    _presets.Add(replacement);
                }

                Sort_NoLock();
            }

            RaiseChanged();
        }

        public void ReplaceSnapshot(IEnumerable<PresetDefinition> presets)
        {
            lock (_sync)
            {
                _presets.Clear();
                if (presets != null)
                {
                    foreach (var preset in presets)
                    {
                        if (preset != null)
                        {
                            _presets.Add(ClonePreset(preset));
                        }
                    }
                }

                Sort_NoLock();
            }

            RaiseChanged();
        }

        public Result<string> SavePreset(PresetDefinition preset, string targetPath, ILogger logger)
        {
            if (preset == null)
            {
                return Result<string>.Fail(Error.Validation("Preset is required."));
            }

            var clone = ClonePreset(preset);
            var result = _repository.SavePreset(clone, targetPath, logger);
            if (result.IsFailure)
            {
                return result;
            }

            lock (_sync)
            {
                var index = FindIndex_NoLock(clone);
                if (index >= 0)
                {
                    _presets[index] = clone;
                }
                else
                {
                    _presets.Add(clone);
                }

                Sort_NoLock();
            }

            RaiseChanged();
            return result;
        }

        public Result DeletePreset(PresetDefinition preset, ILogger logger)
        {
            if (preset == null)
            {
                return Result.Fail(Error.Validation("Preset is required."));
            }

            var clone = ClonePreset(preset);
            if (!string.IsNullOrWhiteSpace(clone.SourcePath))
            {
                var deleteResult = _repository.DeletePreset(clone, logger);
                if (deleteResult.IsFailure)
                {
                    return deleteResult;
                }
            }

            lock (_sync)
            {
                var index = FindIndex_NoLock(clone);
                if (index >= 0)
                {
                    _presets.RemoveAt(index);
                }
            }

            RaiseChanged();
            return Result.Ok();
        }

        private int FindIndex_NoLock(PresetDefinition preset)
        {
            if (preset == null)
            {
                return -1;
            }

            if (!string.IsNullOrWhiteSpace(preset.SourcePath))
            {
                for (var i = 0; i < _presets.Count; i++)
                {
                    var candidate = _presets[i];
                    if (!string.IsNullOrWhiteSpace(candidate.SourcePath) && string.Equals(candidate.SourcePath, preset.SourcePath, StringComparison.OrdinalIgnoreCase))
                    {
                        return i;
                    }
                }
            }

            for (var i = 0; i < _presets.Count; i++)
            {
                var candidate = _presets[i];
                if (string.Equals(candidate.PresetId, preset.PresetId, StringComparison.OrdinalIgnoreCase))
                {
                    return i;
                }
            }

            return -1;
        }

        private void Sort_NoLock()
        {
            _presets.Sort((a, b) => StringComparer.OrdinalIgnoreCase.Compare(a.DisplayName, b.DisplayName));
        }

        private void RaiseChanged()
        {
            var handler = Changed;
            if (handler != null)
            {
                handler(this, EventArgs.Empty);
            }
        }

        public static PresetDefinition ClonePreset(PresetDefinition source)
        {
            if (source == null)
            {
                return null;
            }

            var pipeline = ClonePipeline(source.Pipeline, null);

            var clone = new PresetDefinition(source.PresetId, source.DisplayName, pipeline)
            {
                Description = source.Description,
                IsReadOnly = source.IsReadOnly,
                SourcePath = source.SourcePath,
                OutputPolicy = OutputPolicyDefinition.Clone(source.OutputPolicy),
                PreviewProviderRef = source.PreviewProviderRef == null ? null : source.PreviewProviderRef.Clone()
            };

            if (source.PropertiesProviderRefs != null)
            {
                foreach (var provider in source.PropertiesProviderRefs)
                {
                    if (provider != null && !provider.IsEmpty)
                    {
                        clone.PropertiesProviderRefs.Add(provider.Clone());
                    }
                }
            }

            return clone;
        }

        private static PipelineDefinition ClonePipeline(PipelineDefinition source, string defaultName)
        {
            var clone = new PipelineDefinition
            {
                Name = source == null ? defaultName : (string.IsNullOrWhiteSpace(source.Name) ? defaultName : source.Name)
            };

            if (source != null && source.Steps != null)
            {
                for (var i = 0; i < source.Steps.Count; i++)
                {
                    var step = source.Steps[i];
                    if (step != null)
                    {
                        clone.Steps.Add(CloneStep(step));
                    }
                }
            }

            return clone;
        }

        private static PipelineStepDefinition CloneStep(PipelineStepDefinition source)
        {
            var clone = new PipelineStepDefinition(source.PluginId, source.CapabilityId)
            {
                StepId = source.StepId,
                SlotKey = source.SlotKey,
                DisplayName = source.DisplayName,
                ReferenceAnchor = source.ReferenceAnchor,
                Parameters = CloneParameters(source.Parameters)
            };

            if (source.ChildSteps != null)
            {
                for (var i = 0; i < source.ChildSteps.Count; i++)
                {
                    var child = source.ChildSteps[i];
                    if (child != null)
                    {
                        clone.ChildSteps.Add(CloneStep(child));
                    }
                }
            }

            return clone;
        }

        private static ParameterValues CloneParameters(ParameterValues source)
        {
            var values = new ParameterValues();
            if (source == null)
            {
                return values;
            }

            foreach (var kv in source.ToDictionary())
            {
                values.Set(kv.Key, kv.Value);
            }

            return values;
        }
    }
}
