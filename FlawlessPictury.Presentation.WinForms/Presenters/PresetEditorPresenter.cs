using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using FlawlessPictury.AppCore.CrossCutting;
using FlawlessPictury.AppCore.Plugins.Capabilities;
using FlawlessPictury.AppCore.Plugins.Parameters;
using FlawlessPictury.AppCore.Plugins.Pipeline;
using FlawlessPictury.AppCore.Presets;
using FlawlessPictury.Infrastructure.Plugins;
using FlawlessPictury.Infrastructure.Presets;
using FlawlessPictury.Presentation.WinForms.Parameters;
using FlawlessPictury.Presentation.WinForms.Views;

namespace FlawlessPictury.Presentation.WinForms.Presenters
{
    internal sealed class PresetEditorPresenter
    {
        private readonly IPresetEditorView _view;
        private readonly PresetWorkspace _workspace;
        private readonly PluginEnvironment _pluginEnvironment;
        private readonly ILogger _logger;
        private readonly List<PresetDefinition> _presets;

        private PresetDefinition _current;
        private int _currentPresetIndex;
        private int _currentStepIndex;
        private int _currentStructureIndex;
        private string _currentStructureKey;
        private readonly List<PresetEditorStructureItem> _structureItems;
        private bool _hasLoadedOnce;
        private bool _isDirty;

        public PresetEditorPresenter(IPresetEditorView view, PresetWorkspace workspace, PluginEnvironment pluginEnvironment, ILogger logger)
        {
            _view = view ?? throw new ArgumentNullException(nameof(view));
            _workspace = workspace ?? throw new ArgumentNullException(nameof(workspace));
            _pluginEnvironment = pluginEnvironment ?? throw new ArgumentNullException(nameof(pluginEnvironment));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            _presets = new List<PresetDefinition>();
            _structureItems = new List<PresetEditorStructureItem>();
            _currentPresetIndex = -1;
            _currentStepIndex = -1;
            _currentStructureIndex = -1;
            _currentStructureKey = null;

            _view.ViewReady += OnViewReady;
            _view.SelectedPresetChanged += OnSelectedPresetChanged;
            _view.NewPresetRequested += OnNewPresetRequested;
            _view.DuplicatePresetRequested += OnDuplicatePresetRequested;
            _view.ReloadRequested += delegate { Reload(); };
            _view.SaveRequested += delegate { Save(false); };
            _view.SaveAsRequested += delegate { Save(true); };
            _view.DeleteRequested += OnDeleteRequested;
            _view.OpenPresetLocationRequested += OnOpenPresetLocationRequested;
            _view.AddStepRequested += OnAddStepRequested;
            _view.RemoveStepRequested += OnRemoveStepRequested;
            _view.MoveStepUpRequested += delegate { MoveStep(-1); };
            _view.MoveStepDownRequested += delegate { MoveStep(1); };
            _view.SelectedStructureChanged += OnSelectedStructureChanged;
            _view.SelectedPluginChanged += OnSelectedPluginChanged;
            _view.SelectedCapabilityChanged += OnSelectedCapabilityChanged;
            _view.MetadataChanged += delegate
            {
                PullMetadataFromView();
                PersistCurrentIntoLocalCollection();
                MarkDirty();
                RefreshValidationSummary();
                UpdateActionState();
            };
            _view.ParameterValuesChanged += delegate
            {
                SyncLegacyChildBindings();
                PersistCurrentIntoLocalCollection();
                MarkDirty();
                var currentStep = GetSelectedStep();
                if (currentStep != null)
                {
                    RefreshChildEditors(currentStep, _pluginEnvironment.Catalog.FindCapability(currentStep.PluginId, currentStep.CapabilityId));
                }
                RefreshValidationSummary();
                UpdateActionState();
            };
            _view.EncoderPluginChanged += delegate { OnChildPluginChanged("encoder", CapabilityRole.OptimizationEncoder); };
            _view.EncoderCapabilityChanged += delegate { OnChildCapabilityChanged("encoder"); };
            _view.EncoderParameterValuesChanged += delegate
            {
                SyncLegacyChildBindings();
                PersistCurrentIntoLocalCollection();
                MarkDirty();
                RefreshValidationSummary();
                UpdateActionState();
            };
            _view.MetricPluginChanged += delegate { OnChildPluginChanged("metric", CapabilityRole.OptimizationMetric); };
            _view.MetricCapabilityChanged += delegate { OnChildCapabilityChanged("metric"); };
            _view.MetricParameterValuesChanged += delegate
            {
                SyncLegacyChildBindings();
                PersistCurrentIntoLocalCollection();
                MarkDirty();
                RefreshValidationSummary();
                UpdateActionState();
            };
            _view.OutputPolicyChanged += delegate
            {
                PersistCurrentIntoLocalCollection();
                MarkDirty();
                RefreshValidationSummary();
                UpdateActionState();
            };
            _view.OkRequested += delegate { CommitSessionToWorkspaceAndClose(); };
            _view.CancelRequested += delegate { HandleCloseRequest(); };
        }

        public void Show(object owner)
        {
            LoadFromWorkspace(false);
            _view.ShowDialogView(owner);
        }

        private void OnViewReady(object sender, EventArgs e)
        {
            if (!_hasLoadedOnce)
            {
                LoadFromWorkspace(false);
            }
        }

        private void Reload()
        {
            _pluginEnvironment.Reload();

            var result = _workspace.Reload(_logger);
            if (result.IsFailure)
            {
                _view.ShowError(result.Error == null ? "Preset reload failed." : result.Error.Message);
            }

            LoadFromWorkspace(true);
        }

        private void LoadFromWorkspace(bool updateStatus)
        {
            _hasLoadedOnce = true;
            RefreshFromWorkspacePreservingSelection();
            MarkClean();
            RefreshValidationSummary();
        }

        private void RefreshFromWorkspacePreservingSelection()
        {
            var selectedPresetId = _workspace.GetSelectedPresetId();
            if (string.IsNullOrWhiteSpace(selectedPresetId))
            {
                selectedPresetId = _current == null ? null : _current.PresetId;
            }

            if (string.IsNullOrWhiteSpace(selectedPresetId) && _currentPresetIndex >= 0 && _currentPresetIndex < _presets.Count)
            {
                selectedPresetId = _presets[_currentPresetIndex].PresetId;
            }

            _presets.Clear();
            var loaded = _workspace.GetSnapshot();
            if (loaded != null)
            {
                _presets.AddRange(loaded.OrderBy(p => p.DisplayName, StringComparer.OrdinalIgnoreCase));
            }

            _view.SetPresets(_presets);

            if (_presets.Count == 0)
            {
                _current = null;
                _currentPresetIndex = -1;
                _currentStepIndex = -1;
                ClearEditor();
                return;
            }

            var targetIndex = 0;
            if (!string.IsNullOrWhiteSpace(selectedPresetId))
            {
                var found = _presets.FindIndex(p => string.Equals(p.PresetId, selectedPresetId, StringComparison.OrdinalIgnoreCase));
                if (found >= 0)
                {
                    targetIndex = found;
                }
            }
            else if (_currentPresetIndex >= 0 && _currentPresetIndex < _presets.Count)
            {
                targetIndex = _currentPresetIndex;
            }

            SelectPreset(targetIndex);
        }

        private void OnSelectedPresetChanged(object sender, EventArgs e)
        {
            PersistCurrentIntoLocalCollection();
            SelectPreset(_view.SelectedPresetIndex);
        }

        private void SelectPreset(int index)
        {
            if (index < 0 || index >= _presets.Count)
            {
                _current = null;
                _currentPresetIndex = -1;
                _currentStepIndex = -1;
                ClearEditor();
                return;
            }

            _current = PresetWorkspace.ClonePreset(_presets[index]);
            _currentPresetIndex = index;
            MaterializeLegacyOptimizerChildSteps();
            _currentStepIndex = _current.Pipeline != null && _current.Pipeline.Steps.Count > 0 ? 0 : -1;

            _view.SetPresetSelection(index);
            PushCurrentPresetToView();
            var selected = _currentStepIndex >= 0 && _currentStepIndex < _current.Pipeline.Steps.Count ? _current.Pipeline.Steps[_currentStepIndex] : null;
            _currentStructureKey = selected == null ? null : BuildTopLevelStructureKey(selected, _currentStepIndex);
            RefreshStructureList();
            RefreshSelectionEditor();
            UpdateActionState();
        }

        private void OnNewPresetRequested(object sender, EventArgs e)
        {
            PersistCurrentIntoLocalCollection();

            var preset = new PresetDefinition("new.preset", "New Preset", new PipelineDefinition { Name = "New Preset" })
            {
                Description = string.Empty,
                IsReadOnly = false,
                SourcePath = null
            };

            preset.Pipeline.Steps.Add(CreateDefaultStep());
            _presets.Add(preset);
            SortLocalPresets();
            _view.SetPresets(_presets);

            var index = _presets.FindIndex(p => string.Equals(p.PresetId, preset.PresetId, StringComparison.OrdinalIgnoreCase));
            SelectPreset(index < 0 ? 0 : index);
            MarkDirty();
            RefreshValidationSummary();
            UpdateActionState();
        }

        private void OnDuplicatePresetRequested(object sender, EventArgs e)
        {
            if (_current == null)
            {
                return;
            }

            PullMetadataFromView();
            PersistCurrentIntoLocalCollection();

            var clonedSource = PresetWorkspace.ClonePreset(_current);
            clonedSource.SourcePath = null;

            var clone = new PresetDefinition(
                MakeUniquePresetId((_current.PresetId ?? "preset") + ".copy"),
                MakeUniqueDisplayName((_current.DisplayName ?? _current.PresetId ?? "Preset") + " Copy"),
                clonedSource.Pipeline)
            {
                Description = clonedSource.Description,
                IsReadOnly = clonedSource.IsReadOnly,
                SourcePath = null,
                OutputPolicy = OutputPolicyDefinition.Clone(clonedSource.OutputPolicy)
            };

            _presets.Add(clone);
            SortLocalPresets();
            _view.SetPresets(_presets);
            var index = _presets.FindIndex(p => string.Equals(p.PresetId, clone.PresetId, StringComparison.OrdinalIgnoreCase));
            SelectPreset(index < 0 ? 0 : index);
            MarkDirty();
            RefreshValidationSummary();
            UpdateActionState();
        }

        private void OnOpenPresetLocationRequested(object sender, EventArgs e)
        {
            if (_current == null || string.IsNullOrWhiteSpace(_current.SourcePath))
            {
                _view.ShowMessage("This preset has not been saved to disk yet.");
                return;
            }

            try
            {
                if (File.Exists(_current.SourcePath))
                {
                    Process.Start("explorer.exe", "/select,\"" + _current.SourcePath + "\"");
                    return;
                }

                var directory = Path.GetDirectoryName(_current.SourcePath);
                if (!string.IsNullOrWhiteSpace(directory) && Directory.Exists(directory))
                {
                    Process.Start("explorer.exe", "\"" + directory + "\"");
                    return;
                }

                _view.ShowMessage("Preset file location does not exist on disk.");
            }
            catch (Exception ex)
            {
                _view.ShowError("Could not open preset file location: " + ex.Message);
            }
        }

        private void OnDeleteRequested(object sender, EventArgs e)
        {
            if (_current == null)
            {
                return;
            }

            PullMetadataFromView();

            if (!_view.ConfirmDelete(_current.DisplayName))
            {
                return;
            }

            var result = _workspace.DeletePreset(_current, _logger);
            if (result.IsFailure)
            {
                _view.ShowError(result.Error == null ? "Delete failed." : result.Error.Message);
                return;
            }

            _presets.RemoveAll(p =>
                (!string.IsNullOrWhiteSpace(_current.SourcePath) && !string.IsNullOrWhiteSpace(p.SourcePath) && string.Equals(p.SourcePath, _current.SourcePath, StringComparison.OrdinalIgnoreCase)) ||
                string.Equals(p.PresetId, _current.PresetId, StringComparison.OrdinalIgnoreCase));

            _view.SetPresets(_presets);

            if (_presets.Count == 0)
            {
                _current = null;
                _currentPresetIndex = -1;
                _currentStepIndex = -1;
                _workspace.SetSelectedPresetId(null);
                ClearEditor();
            }
            else
            {
                SelectPreset(Math.Min(_currentPresetIndex, _presets.Count - 1));
                _workspace.SetSelectedPresetId(_current == null ? null : _current.PresetId);
            }

            MarkClean();
            RefreshValidationSummary();
        }

        private void OnAddStepRequested(object sender, EventArgs e)
        {
            if (_current == null)
            {
                return;
            }

            PullMetadataFromView();

            var step = CreateDefaultStep();
            _current.Pipeline.Steps.Add(step);
            _currentStepIndex = _current.Pipeline.Steps.Count - 1;
            _currentStructureKey = BuildTopLevelStructureKey(step, _currentStepIndex);
            RefreshStructureList();
            RefreshSelectionEditor();
            PersistCurrentIntoLocalCollection();
            MarkDirty();
            RefreshValidationSummary();
            UpdateActionState();
        }

        private void OnRemoveStepRequested(object sender, EventArgs e)
        {
            if (_current == null || _current.Pipeline == null)
            {
                return;
            }

            var index = GetSelectedTopLevelStepIndex();
            if (index < 0 || index >= _current.Pipeline.Steps.Count)
            {
                return;
            }

            _current.Pipeline.Steps.RemoveAt(index);
            if (_current.Pipeline.Steps.Count == 0)
            {
                _currentStepIndex = -1;
            }
            else if (index >= _current.Pipeline.Steps.Count)
            {
                _currentStepIndex = _current.Pipeline.Steps.Count - 1;
            }
            else
            {
                _currentStepIndex = index;
            }

            var selected = _currentStepIndex >= 0 && _currentStepIndex < _current.Pipeline.Steps.Count ? _current.Pipeline.Steps[_currentStepIndex] : null;
            _currentStructureKey = selected == null ? null : BuildTopLevelStructureKey(selected, _currentStepIndex);
            RefreshStructureList();
            RefreshSelectionEditor();
            PersistCurrentIntoLocalCollection();
            MarkDirty();
            RefreshValidationSummary();
            UpdateActionState();
        }

        private void MoveStep(int delta)
        {
            if (_current == null || _current.Pipeline == null)
            {
                return;
            }

            var index = GetSelectedTopLevelStepIndex();
            var newIndex = index + delta;
            if (index < 0 || index >= _current.Pipeline.Steps.Count || newIndex < 0 || newIndex >= _current.Pipeline.Steps.Count)
            {
                return;
            }

            var step = _current.Pipeline.Steps[index];
            _current.Pipeline.Steps.RemoveAt(index);
            _current.Pipeline.Steps.Insert(newIndex, step);
            _currentStepIndex = newIndex;
            _currentStructureKey = BuildTopLevelStructureKey(step, newIndex);
            RefreshStructureList();
            RefreshSelectionEditor();
            PersistCurrentIntoLocalCollection();
            MarkDirty();
            RefreshValidationSummary();
            UpdateActionState();
        }

        private void OnSelectedStructureChanged(object sender, EventArgs e)
        {
            _currentStructureIndex = _view.SelectedStructureIndex;
            var item = GetSelectedStructureItem();
            _currentStructureKey = item == null ? null : item.Key;
            if (item != null && (item.Kind == PresetEditorStructureItemKind.TopLevelStep || item.Kind == PresetEditorStructureItemKind.ChildStep))
            {
                _currentStepIndex = item.StepIndex;
            }

            RefreshSelectionEditor();
            UpdateActionState();
        }

        private void OnSelectedPluginChanged(object sender, EventArgs e)
        {
            var selected = GetSelectedStructureItem();
            if (selected != null && selected.Kind == PresetEditorStructureItemKind.ChildStep)
            {
                var pluginId = _view.SelectedPluginId;
                if (string.IsNullOrWhiteSpace(pluginId))
                {
                    return;
                }

                var role = string.Equals(selected.SlotKey, "encoder", StringComparison.OrdinalIgnoreCase)
                    ? CapabilityRole.OptimizationEncoder
                    : CapabilityRole.OptimizationMetric;
                var choices = BuildCapabilityChoices(pluginId, role);
                var chosen = choices.Count > 0 ? choices[0].Id : null;
                RebuildChildStep(selected.SlotKey, pluginId, chosen, role);
                return;
            }

            var step = GetSelectedStep();
            if (step == null)
            {
                return;
            }

            var pluginIdTop = _view.SelectedPluginId;
            if (string.IsNullOrWhiteSpace(pluginIdTop))
            {
                return;
            }

            var topChoices = BuildCapabilityChoices(pluginIdTop);
            var topChosen = topChoices.Count > 0 ? topChoices[0].Id : null;
            RebuildSelectedStep(pluginIdTop, topChosen);
        }

        private void OnSelectedCapabilityChanged(object sender, EventArgs e)
        {
            var selected = GetSelectedStructureItem();
            if (selected != null && selected.Kind == PresetEditorStructureItemKind.ChildStep)
            {
                RebuildChildStep(selected.SlotKey, _view.SelectedPluginId, _view.SelectedCapabilityId, CapabilityRole.None);
                return;
            }

            var step = GetSelectedStep();
            if (step == null)
            {
                return;
            }

            RebuildSelectedStep(step.PluginId, _view.SelectedCapabilityId);
        }

        private void RebuildSelectedStep(string pluginId, string capabilityId)
        {
            var oldStep = GetSelectedStep();
            if (oldStep == null || string.IsNullOrWhiteSpace(pluginId) || string.IsNullOrWhiteSpace(capabilityId))
            {
                return;
            }

            var newStep = new PipelineStepDefinition(pluginId, capabilityId)
            {
                StepId = oldStep.StepId,
                DisplayName = oldStep.DisplayName,
                ReferenceAnchor = oldStep.ReferenceAnchor,
                Parameters = BuildInitialParameterValues(pluginId, capabilityId, oldStep.Parameters)
            };

            _current.Pipeline.Steps[_currentStepIndex] = newStep;
            _currentStructureKey = BuildTopLevelStructureKey(newStep, _currentStepIndex);
            RefreshStructureList();
            RefreshSelectionEditor();
            PersistCurrentIntoLocalCollection();
            MarkDirty();
            RefreshValidationSummary();
            UpdateActionState();
        }

        private void RefreshSelectionEditor()
        {
            var selectedItem = GetSelectedStructureItem();
            if (selectedItem == null)
            {
                ShowNoSelectionEditor();
                return;
            }

            if (selectedItem.Kind == PresetEditorStructureItemKind.OutputPolicy)
            {
                ShowOutputPolicyEditor();
                return;
            }

            if (selectedItem.Kind == PresetEditorStructureItemKind.FinalCommit)
            {
                ShowFinalCommitSummary();
                return;
            }

            if (selectedItem.Kind == PresetEditorStructureItemKind.ChildStep)
            {
                ShowChildStepEditor(selectedItem);
                return;
            }

            ShowTopLevelStepEditor();
        }

        private void RefreshStructureList()
        {
            _structureItems.Clear();
            if (_current == null || _current.Pipeline == null)
            {
                _view.SetStructureItems(_structureItems);
                _currentStructureIndex = -1;
                return;
            }

            for (var i = 0; i < _current.Pipeline.Steps.Count; i++)
            {
                var step = _current.Pipeline.Steps[i];
                _structureItems.Add(new PresetEditorStructureItem(
                    PresetEditorStructureItemKind.TopLevelStep,
                    BuildTopLevelStructureKey(step, i),
                    BuildTopLevelStepLabel(step, i),
                    i,
                    null));

                foreach (var childItem in BuildChildStructureItems(step, i))
                {
                    _structureItems.Add(childItem);
                }
            }

            _structureItems.Add(new PresetEditorStructureItem(PresetEditorStructureItemKind.OutputPolicy, "common:output-policy", "Output Policy", -1, null));
            _structureItems.Add(new PresetEditorStructureItem(PresetEditorStructureItemKind.FinalCommit, "common:final-commit", "Final Commit (SafeOutput)", -1, null));

            _view.SetStructureItems(_structureItems);

            var selectionIndex = FindStructureSelectionIndex(_currentStructureKey);
            if (selectionIndex < 0)
            {
                selectionIndex = FindDefaultStructureSelectionIndex();
            }

            _currentStructureIndex = selectionIndex;
            _view.SetStructureSelection(selectionIndex);
        }

        private int FindStructureSelectionIndex(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                return -1;
            }

            for (var i = 0; i < _structureItems.Count; i++)
            {
                if (string.Equals(_structureItems[i].Key, key, StringComparison.OrdinalIgnoreCase))
                {
                    return i;
                }
            }

            return -1;
        }

        private int FindDefaultStructureSelectionIndex()
        {
            if (_currentStepIndex >= 0 && _currentStepIndex < _current.Pipeline.Steps.Count)
            {
                var step = _current.Pipeline.Steps[_currentStepIndex];
                var key = BuildTopLevelStructureKey(step, _currentStepIndex);
                return FindStructureSelectionIndex(key);
            }

            return _structureItems.Count > 0 ? 0 : -1;
        }

        private void ShowNoSelectionEditor()
        {
            _view.SetPluginChoices(BuildPluginChoices(), null);
            _view.SetCapabilityChoices(new List<PresetEditorChoiceItem>(), null);
            _view.SetParameterEditor(null);
            _view.SetCapabilitySummary(string.Empty);
            _view.SetOutputPolicyEditor(_current == null ? null : _current.OutputPolicy);
            RefreshValidationSummary();
            ClearChildEditors();
            _view.SelectStepTab();
        }

        private void ShowTopLevelStepEditor()
        {
            var step = GetSelectedStep();
            if (step == null)
            {
                ShowNoSelectionEditor();
                return;
            }

            var pluginChoices = BuildPluginChoices();
            _view.SetPluginChoices(pluginChoices, step.PluginId);
            _view.SetCapabilityChoices(BuildCapabilityChoices(step.PluginId), step.CapabilityId);

            var cap = _pluginEnvironment.Catalog.FindCapability(step.PluginId, step.CapabilityId);
            if (cap == null)
            {
                _view.SetParameterEditor(null);
                _view.SetCapabilitySummary("Missing capability: " + step.PluginId + "/" + step.CapabilityId);
                RefreshValidationSummary();
                return;
            }

            step.Parameters = BuildInitialParameterValues(step.PluginId, step.CapabilityId, step.Parameters);
            _view.SetParameterEditor(BuildMainParameterEditor(step, cap));
            _view.SetCapabilitySummary(BuildCapabilitySummary(cap));
            RefreshValidationSummary();
        }

        private void ShowChildStepEditor(PresetEditorStructureItem selectedItem)
        {
            var parent = GetSelectedStep();
            if (parent == null || selectedItem == null || string.IsNullOrWhiteSpace(selectedItem.SlotKey))
            {
                ShowNoSelectionEditor();
                return;
            }

            var role = string.Equals(selectedItem.SlotKey, "encoder", StringComparison.OrdinalIgnoreCase)
                ? CapabilityRole.OptimizationEncoder
                : CapabilityRole.OptimizationMetric;

            var child = EnsureChildStep(parent, selectedItem.SlotKey, role);
            SyncLegacyChildBindings();

            _view.SetPluginChoices(BuildPluginChoices(role), child.PluginId);
            _view.SetCapabilityChoices(BuildCapabilityChoices(child.PluginId, role), child.CapabilityId);

            var cap = _pluginEnvironment.Catalog.FindCapability(child.PluginId, child.CapabilityId);
            if (cap == null)
            {
                _view.SetParameterEditor(null);
                _view.SetCapabilitySummary("Missing capability: " + child.PluginId + "/" + child.CapabilityId);
                RefreshValidationSummary();
                return;
            }

            child.Parameters = BuildInitialParameterValues(child.PluginId, child.CapabilityId, child.Parameters);
            var readOnlyKeys = string.Equals(selectedItem.SlotKey, "encoder", StringComparison.OrdinalIgnoreCase) ? GetEncoderReadOnlyKeys(parent) : null;
            _view.SetParameterEditor(new DynamicParameterObject(cap.Parameters, child.Parameters, null, readOnlyKeys));
            _view.SetCapabilitySummary(BuildCapabilitySummary(cap));
            RefreshValidationSummary();
        }

        private void ShowOutputPolicyEditor()
        {
            _view.SetPluginChoices(new List<PresetEditorChoiceItem>(), null);
            _view.SetCapabilityChoices(new List<PresetEditorChoiceItem>(), null);
            _view.SetParameterEditor(_current == null ? null : _current.OutputPolicy);
            _view.SetCapabilitySummary("Host-owned output policy. This is not a removable pipeline step.");
            _view.SetOutputPolicyEditor(_current == null ? null : _current.OutputPolicy);
            RefreshValidationSummary();
            ClearChildEditors();
            _view.SelectOutputTab();
        }

        private void ShowFinalCommitSummary()
        {
            _view.SetPluginChoices(new List<PresetEditorChoiceItem>(), null);
            _view.SetCapabilityChoices(new List<PresetEditorChoiceItem>(), null);
            _view.SetParameterEditor(null);
            _view.SetCapabilitySummary("Host-owned final commit stage. Current builds use SafeOutput commit, so originals are not modified.");
            _view.SetOutputPolicyEditor(_current == null ? null : _current.OutputPolicy);
            RefreshValidationSummary();
            ClearChildEditors();
            _view.SelectStepTab();
        }

        private void PushCurrentPresetToView()
        {
            if (_current == null)
            {
                ClearEditor();
                return;
            }

            _view.PresetIdText = _current.PresetId;
            _view.DisplayNameText = _current.DisplayName;
            _view.DescriptionText = _current.Description;
            _view.IsReadOnlyChecked = false;
            if (_current.OutputPolicy == null)
            {
                _current.OutputPolicy = OutputPolicyDefinition.CreateDefault();
            }
            _view.SetOutputPolicyEditor(_current.OutputPolicy);
        }

        private void PullMetadataFromView()
        {
            if (_current == null)
            {
                return;
            }

            var presetId = (_view.PresetIdText ?? string.Empty).Trim();
            var displayName = (_view.DisplayNameText ?? string.Empty).Trim();

            if (!string.IsNullOrWhiteSpace(presetId))
            {
                if (!string.Equals(_current.PresetId, presetId, StringComparison.OrdinalIgnoreCase))
                {
                    _current = new PresetDefinition(presetId, string.IsNullOrWhiteSpace(displayName) ? presetId : displayName, _current.Pipeline)
                    {
                        Description = _view.DescriptionText,
                        IsReadOnly = false,
                        SourcePath = _current.SourcePath,
                        OutputPolicy = OutputPolicyDefinition.Clone(_current.OutputPolicy)
                    };
                }
            }

            _current.Description = _view.DescriptionText;
            _current.IsReadOnly = false;

            if (!string.IsNullOrWhiteSpace(displayName) && !string.Equals(_current.DisplayName, displayName, StringComparison.Ordinal))
            {
                _current = new PresetDefinition(_current.PresetId, displayName, _current.Pipeline)
                {
                    Description = _current.Description,
                    IsReadOnly = _current.IsReadOnly,
                    SourcePath = _current.SourcePath,
                    OutputPolicy = OutputPolicyDefinition.Clone(_current.OutputPolicy)
                };
            }
        }

        private void Save(bool forceSaveAs)
        {
            if (_current == null)
            {
                return;
            }

            PullMetadataFromView();

            var validationErrors = BuildValidationMessages(true);
            if (validationErrors.Count > 0)
            {
                _view.ShowError(string.Join(Environment.NewLine, validationErrors));
                RefreshValidationSummary();
                return;
            }

            var savePath = _current.SourcePath;
            if (forceSaveAs || string.IsNullOrWhiteSpace(savePath))
            {
                var fileName = MakeSafeFileName(_current.PresetId) + ".json";
                savePath = _view.PromptSaveAsPath(_workspace.DirectoryPath, fileName);
                if (string.IsNullOrWhiteSpace(savePath))
                {
                    return;
                }
            }

            var result = _workspace.SavePreset(_current, savePath, _logger);
            if (result.IsFailure)
            {
                _view.ShowError(result.Error == null ? "Save failed." : result.Error.Message);
                return;
            }

            _current.SourcePath = savePath;
            PersistCurrentIntoLocalCollection();
            _workspace.SetSelectedPresetId(_current.PresetId);
            _workspace.ReplaceSnapshot(_presets);
            _view.SetPresets(_presets);
            var savedIndex = _presets.FindIndex(p => string.Equals(p.PresetId, _current.PresetId, StringComparison.OrdinalIgnoreCase));
            if (savedIndex >= 0)
            {
                SelectPreset(savedIndex);
            }

            MarkClean();
            RefreshValidationSummary();
            UpdateActionState();
        }

        private void PersistCurrentIntoLocalCollection()
        {
            if (_current == null)
            {
                return;
            }

            var clone = PresetWorkspace.ClonePreset(_current);
            var index = _presets.FindIndex(p =>
                (!string.IsNullOrWhiteSpace(clone.SourcePath) && !string.IsNullOrWhiteSpace(p.SourcePath) && string.Equals(p.SourcePath, clone.SourcePath, StringComparison.OrdinalIgnoreCase)) ||
                string.Equals(p.PresetId, clone.PresetId, StringComparison.OrdinalIgnoreCase));

            if (index >= 0)
            {
                _presets[index] = clone;
                _currentPresetIndex = index;
            }
            else
            {
                _presets.Add(clone);
                SortLocalPresets();
                _currentPresetIndex = _presets.FindIndex(p => string.Equals(p.PresetId, clone.PresetId, StringComparison.OrdinalIgnoreCase));
            }
        }

        private void CommitSessionToWorkspaceAndClose()
        {
            PersistCurrentIntoLocalCollection();
            _workspace.SetSelectedPresetId(_current == null ? null : _current.PresetId);
            _workspace.ReplaceSnapshot(_presets);
            MarkClean();
            _view.CloseView(System.Windows.Forms.DialogResult.OK);
        }

        private void SortLocalPresets()
        {
            _presets.Sort((a, b) => StringComparer.OrdinalIgnoreCase.Compare(a.DisplayName, b.DisplayName));
        }

        private void UpdateActionState()
        {
            var hasPreset = _current != null;
            var selected = GetSelectedStructureItem();
            var canModifySelectedStep = hasPreset && selected != null && selected.Kind == PresetEditorStructureItemKind.TopLevelStep;
            var canEditSelectors = hasPreset && selected != null && (selected.Kind == PresetEditorStructureItemKind.TopLevelStep || selected.Kind == PresetEditorStructureItemKind.ChildStep);
            var canEditProperties = hasPreset && selected != null && selected.Kind != PresetEditorStructureItemKind.FinalCommit;
            var canOpenLocation = hasPreset && !string.IsNullOrWhiteSpace(_current.SourcePath);
            _view.SetActionState(hasPreset, hasPreset, hasPreset, hasPreset, canOpenLocation, hasPreset, canModifySelectedStep, canEditSelectors, canEditProperties);
        }

        private void ClearEditor()
        {
            _currentStructureIndex = -1;
            _currentStructureKey = null;
            _structureItems.Clear();
            _view.PresetIdText = string.Empty;
            _view.DisplayNameText = string.Empty;
            _view.DescriptionText = string.Empty;
            _view.IsReadOnlyChecked = false;
            _view.SetStructureItems(new List<PresetEditorStructureItem>());
            _view.SetPluginChoices(BuildPluginChoices(), null);
            _view.SetCapabilityChoices(new List<PresetEditorChoiceItem>(), null);
            _view.SetParameterEditor(null);
            _view.SetCapabilitySummary(string.Empty);
            _view.SetOutputPolicyEditor(null);
            _view.SetValidationSummary(string.Empty);
            ClearChildEditors();
            _view.SelectStepTab();
            UpdateActionState();
        }

        private PipelineStepDefinition CreateDefaultStep()
        {
            var pluginChoices = BuildPluginChoices();
            if (pluginChoices.Count == 0)
            {
                return new PipelineStepDefinition("sample", "copy_file");
            }

            var pluginId = pluginChoices[0].Id;
            var capabilityChoices = BuildCapabilityChoices(pluginId);
            var capabilityId = capabilityChoices.Count > 0 ? capabilityChoices[0].Id : string.Empty;

            return new PipelineStepDefinition(pluginId, capabilityId)
            {
                Parameters = BuildInitialParameterValues(pluginId, capabilityId, null)
            };
        }

        private PipelineStepDefinition GetSelectedStep()
        {
            if (_current == null || _current.Pipeline == null)
            {
                return null;
            }

            if (_currentStepIndex < 0 || _currentStepIndex >= _current.Pipeline.Steps.Count)
            {
                return null;
            }

            return _current.Pipeline.Steps[_currentStepIndex];
        }

        private PresetEditorStructureItem GetSelectedStructureItem()
        {
            if (_currentStructureIndex < 0 || _currentStructureIndex >= _structureItems.Count)
            {
                return null;
            }

            return _structureItems[_currentStructureIndex];
        }

        private int GetSelectedTopLevelStepIndex()
        {
            var selected = GetSelectedStructureItem();
            return selected != null && selected.Kind == PresetEditorStructureItemKind.TopLevelStep ? selected.StepIndex : -1;
        }

        private string BuildTopLevelStructureKey(PipelineStepDefinition step, int index)
        {
            if (step == null)
            {
                return string.Empty;
            }

            var stable = string.IsNullOrWhiteSpace(step.StepId) ? index.ToString(CultureInfo.InvariantCulture) : step.StepId;
            return "step:" + stable;
        }

        private string BuildChildStructureKey(PipelineStepDefinition step, int index, string slotKey)
        {
            return BuildTopLevelStructureKey(step, index) + ":child:" + (slotKey ?? string.Empty);
        }

        private string BuildTopLevelStepLabel(PipelineStepDefinition step, int index)
        {
            var name = ResolveStepDisplayName(step);
            return string.Format(CultureInfo.InvariantCulture, "{0}. {1}", index + 1, name);
        }

        private IEnumerable<PresetEditorStructureItem> BuildChildStructureItems(PipelineStepDefinition step, int stepIndex)
        {
            var capability = step == null ? null : _pluginEnvironment.Catalog.FindCapability(step.PluginId, step.CapabilityId);
            if (!IsOptimizerLikeStep(step, capability))
            {
                yield break;
            }

            yield return new PresetEditorStructureItem(
                PresetEditorStructureItemKind.ChildStep,
                BuildChildStructureKey(step, stepIndex, "encoder"),
                "    Encoder Child: " + ResolveChildDisplayName(step, "encoder", "Not configured"),
                stepIndex,
                "encoder");

            yield return new PresetEditorStructureItem(
                PresetEditorStructureItemKind.ChildStep,
                BuildChildStructureKey(step, stepIndex, "metric"),
                "    Metric Child: " + ResolveChildDisplayName(step, "metric", "Not configured"),
                stepIndex,
                "metric");
        }

        private string ResolveStepDisplayName(PipelineStepDefinition step)
        {
            if (step == null)
            {
                return string.Empty;
            }

            var fallbackName = string.IsNullOrWhiteSpace(step.DisplayName) ? step.CapabilityId : step.DisplayName;
            return FormatCapabilityDisplayName(step.PluginId, step.CapabilityId, fallbackName);
        }

        private string ResolveChildDisplayName(PipelineStepDefinition parentStep, string slotKey, string fallbackText)
        {
            var child = GetChildStep(parentStep, slotKey);
            if (child != null)
            {
                return ResolveStepDisplayName(child);
            }

            var pluginId = parentStep == null || parentStep.Parameters == null ? null : parentStep.Parameters.GetString(slotKey + ".pluginId", null);
            var capabilityId = parentStep == null || parentStep.Parameters == null ? null : parentStep.Parameters.GetString(slotKey + ".capabilityId", null);
            if (!string.IsNullOrWhiteSpace(pluginId) && !string.IsNullOrWhiteSpace(capabilityId))
            {
                return FormatCapabilityDisplayName(pluginId, capabilityId, capabilityId);
            }

            return fallbackText;
        }

        private string FormatCapabilityDisplayName(string pluginId, string capabilityId, string fallbackName)
        {
            var cap = _pluginEnvironment.Catalog.FindCapability(pluginId, capabilityId);
            var name = cap != null && !string.IsNullOrWhiteSpace(cap.DisplayName) ? cap.DisplayName : fallbackName;
            if (string.IsNullOrWhiteSpace(name))
            {
                name = capabilityId ?? string.Empty;
            }

            if (string.IsNullOrWhiteSpace(capabilityId))
            {
                return name;
            }

            return name + " [" + capabilityId + "]";
        }

        private List<PresetEditorChoiceItem> BuildPluginChoices()
        {
            return BuildPluginChoices(CapabilityRole.None);
        }

        private List<PresetEditorChoiceItem> BuildPluginChoices(CapabilityRole requiredRole)
        {
            var items = new List<PresetEditorChoiceItem>();
            foreach (var plugin in _pluginEnvironment.Catalog.GetPlugins().OrderBy(p => p.GetMetadata().DisplayName, StringComparer.OrdinalIgnoreCase))
            {
                var meta = plugin.GetMetadata();
                if (requiredRole != CapabilityRole.None && !plugin.GetCapabilities().Any(c => c.HasRole(requiredRole)))
                {
                    continue;
                }

                items.Add(new PresetEditorChoiceItem(meta.PluginId, meta.DisplayName + " [" + meta.PluginId + "]"));
            }

            return items;
        }

        private List<PresetEditorChoiceItem> BuildCapabilityChoices(string pluginId)
        {
            return BuildCapabilityChoices(pluginId, CapabilityRole.None);
        }

        private List<PresetEditorChoiceItem> BuildCapabilityChoices(string pluginId, CapabilityRole requiredRole)
        {
            var items = new List<PresetEditorChoiceItem>();
            var plugin = _pluginEnvironment.Catalog.FindPlugin(pluginId);
            if (plugin == null)
            {
                return items;
            }

            foreach (var cap in plugin.GetCapabilities().OrderBy(c => c.DisplayName, StringComparer.OrdinalIgnoreCase))
            {
                if (requiredRole != CapabilityRole.None && !cap.HasRole(requiredRole))
                {
                    continue;
                }

                items.Add(new PresetEditorChoiceItem(cap.Id, cap.DisplayName + " [" + cap.Id + "]"));
            }

            return items;
        }

        private static bool IsOptimizerLikeStep(PipelineStepDefinition step, CapabilityDescriptor cap)
        {
            if (step == null || cap == null || cap.Kind != OperationKind.Orchestrator)
            {
                return false;
            }

            if (step.ChildSteps.Any(c => string.Equals(c.SlotKey, "encoder", StringComparison.OrdinalIgnoreCase) || string.Equals(c.SlotKey, "metric", StringComparison.OrdinalIgnoreCase)))
            {
                return true;
            }

            return cap.Parameters.Parameters.Any(p => p != null &&
                (p.Key.StartsWith("encoder.", StringComparison.OrdinalIgnoreCase) ||
                 p.Key.StartsWith("metric.", StringComparison.OrdinalIgnoreCase) ||
                 string.Equals(p.Key, "search.parameterKey", StringComparison.OrdinalIgnoreCase)));
        }

        private object BuildMainParameterEditor(PipelineStepDefinition step, CapabilityDescriptor cap)
        {
            Func<ParameterDefinition, bool> filter = null;
            if (IsOptimizerLikeStep(step, cap))
            {
                filter = def => def != null &&
                    !def.Key.StartsWith("encoder.", StringComparison.OrdinalIgnoreCase) &&
                    !def.Key.StartsWith("metric.", StringComparison.OrdinalIgnoreCase);
            }

            return new DynamicParameterObject(cap.Parameters, step.Parameters, filter, null);
        }

        private void RefreshChildEditors(PipelineStepDefinition step, CapabilityDescriptor cap)
        {
            var visible = IsOptimizerLikeStep(step, cap);
            _view.SetOptimizerChildEditorsVisible(visible);
            if (!visible)
            {
                ClearChildEditors();
                return;
            }

            var encoderStep = EnsureChildStep(step, "encoder", CapabilityRole.OptimizationEncoder);
            var metricStep = EnsureChildStep(step, "metric", CapabilityRole.OptimizationMetric);
            SyncLegacyChildBindings();

            BindChildEditor(encoderStep, CapabilityRole.OptimizationEncoder, _view.SetEncoderPluginChoices, _view.SetEncoderCapabilityChoices, _view.SetEncoderParameterEditor, _view.SetEncoderSummary, GetEncoderReadOnlyKeys(step));
            BindChildEditor(metricStep, CapabilityRole.OptimizationMetric, _view.SetMetricPluginChoices, _view.SetMetricCapabilityChoices, _view.SetMetricParameterEditor, _view.SetMetricSummary, null);
        }

        private void ClearChildEditors()
        {
            _view.SetOptimizerChildEditorsVisible(false);
            _view.SetEncoderPluginChoices(new List<PresetEditorChoiceItem>(), null);
            _view.SetEncoderCapabilityChoices(new List<PresetEditorChoiceItem>(), null);
            _view.SetEncoderParameterEditor(null);
            _view.SetEncoderSummary(string.Empty);
            _view.SetMetricPluginChoices(new List<PresetEditorChoiceItem>(), null);
            _view.SetMetricCapabilityChoices(new List<PresetEditorChoiceItem>(), null);
            _view.SetMetricParameterEditor(null);
            _view.SetMetricSummary(string.Empty);
        }

        private void BindChildEditor(
            PipelineStepDefinition childStep,
            CapabilityRole role,
            Action<IReadOnlyList<PresetEditorChoiceItem>, string> setPluginChoices,
            Action<IReadOnlyList<PresetEditorChoiceItem>, string> setCapabilityChoices,
            Action<object> setParameterEditor,
            Action<string> setSummary,
            IEnumerable<string> readOnlyKeys)
        {
            if (childStep == null)
            {
                setPluginChoices(new List<PresetEditorChoiceItem>(), null);
                setCapabilityChoices(new List<PresetEditorChoiceItem>(), null);
                setParameterEditor(null);
                setSummary(string.Empty);
                return;
            }

            setPluginChoices(BuildPluginChoices(role), childStep.PluginId);
            setCapabilityChoices(BuildCapabilityChoices(childStep.PluginId, role), childStep.CapabilityId);

            var cap = _pluginEnvironment.Catalog.FindCapability(childStep.PluginId, childStep.CapabilityId);
            if (cap == null)
            {
                setParameterEditor(null);
                setSummary("Missing capability: " + childStep.PluginId + "/" + childStep.CapabilityId);
                return;
            }

            childStep.Parameters = BuildInitialParameterValues(childStep.PluginId, childStep.CapabilityId, childStep.Parameters);
            setParameterEditor(new DynamicParameterObject(cap.Parameters, childStep.Parameters, null, readOnlyKeys));
            setSummary(BuildCapabilitySummary(cap));
        }

        private IEnumerable<string> GetEncoderReadOnlyKeys(PipelineStepDefinition parentStep)
        {
            if (parentStep == null || parentStep.Parameters == null)
            {
                return null;
            }

            var key = parentStep.Parameters.GetString("search.parameterKey", null);
            if (string.IsNullOrWhiteSpace(key))
            {
                return null;
            }

            return new[] { key };
        }

        private void OnChildPluginChanged(string slotKey, CapabilityRole role)
        {
            var pluginId = string.Equals(slotKey, "encoder", StringComparison.OrdinalIgnoreCase)
                ? _view.SelectedEncoderPluginId
                : _view.SelectedMetricPluginId;
            if (string.IsNullOrWhiteSpace(pluginId))
            {
                return;
            }

            var choices = BuildCapabilityChoices(pluginId, role);
            var chosen = choices.Count > 0 ? choices[0].Id : null;
            RebuildChildStep(slotKey, pluginId, chosen, role);
        }

        private void OnChildCapabilityChanged(string slotKey)
        {
            var pluginId = string.Equals(slotKey, "encoder", StringComparison.OrdinalIgnoreCase)
                ? _view.SelectedEncoderPluginId
                : _view.SelectedMetricPluginId;
            var capabilityId = string.Equals(slotKey, "encoder", StringComparison.OrdinalIgnoreCase)
                ? _view.SelectedEncoderCapabilityId
                : _view.SelectedMetricCapabilityId;
            RebuildChildStep(slotKey, pluginId, capabilityId, CapabilityRole.None);
        }

        private void RebuildChildStep(string slotKey, string pluginId, string capabilityId, CapabilityRole role)
        {
            var parent = GetSelectedStep();
            if (parent == null || string.IsNullOrWhiteSpace(slotKey) || string.IsNullOrWhiteSpace(pluginId) || string.IsNullOrWhiteSpace(capabilityId))
            {
                return;
            }

            var oldStep = GetChildStep(parent, slotKey);
            var newStep = new PipelineStepDefinition(pluginId, capabilityId)
            {
                SlotKey = slotKey,
                DisplayName = oldStep == null ? null : oldStep.DisplayName,
                ReferenceAnchor = oldStep == null ? ReferenceAnchorKind.None : oldStep.ReferenceAnchor,
                Parameters = BuildInitialParameterValues(pluginId, capabilityId, oldStep == null ? null : oldStep.Parameters)
            };

            if (oldStep != null)
            {
                newStep.StepId = oldStep.StepId;
                parent.ChildSteps.Remove(oldStep);
            }

            parent.ChildSteps.Add(newStep);
            if (string.Equals(slotKey, "encoder", StringComparison.OrdinalIgnoreCase))
            {
                TryDefaultSearchParameter(parent, newStep);
            }

            SyncLegacyChildBindings();
            _currentStructureKey = BuildChildStructureKey(parent, _currentStepIndex, slotKey);
            RefreshStructureList();
            RefreshSelectionEditor();
            PersistCurrentIntoLocalCollection();
            MarkDirty();
            RefreshValidationSummary();
            UpdateActionState();
        }

        private PipelineStepDefinition EnsureChildStep(PipelineStepDefinition parent, string slotKey, CapabilityRole role)
        {
            var existing = GetChildStep(parent, slotKey);
            if (existing != null)
            {
                return existing;
            }

            var legacyPluginKey = slotKey + ".pluginId";
            var legacyCapabilityKey = slotKey + ".capabilityId";
            var pluginId = parent.Parameters.GetString(legacyPluginKey, null);
            var capabilityId = parent.Parameters.GetString(legacyCapabilityKey, null);

            if (string.IsNullOrWhiteSpace(pluginId) || string.IsNullOrWhiteSpace(capabilityId))
            {
                var first = FindFirstCapability(role);
                pluginId = first.Item1;
                capabilityId = first.Item2;
            }

            if (string.IsNullOrWhiteSpace(pluginId) || string.IsNullOrWhiteSpace(capabilityId))
            {
                return null;
            }

            var child = new PipelineStepDefinition(pluginId, capabilityId)
            {
                SlotKey = slotKey,
                Parameters = BuildInitialParameterValues(pluginId, capabilityId, ExtractLegacyChildParameters(parent, slotKey))
            };

            parent.ChildSteps.Add(child);
            if (string.Equals(slotKey, "encoder", StringComparison.OrdinalIgnoreCase))
            {
                TryDefaultSearchParameter(parent, child);
            }

            return child;
        }

        private static PipelineStepDefinition GetChildStep(PipelineStepDefinition parent, string slotKey)
        {
            if (parent == null || string.IsNullOrWhiteSpace(slotKey))
            {
                return null;
            }

            return parent.ChildSteps.FirstOrDefault(c => string.Equals(c.SlotKey, slotKey, StringComparison.OrdinalIgnoreCase));
        }

        private Tuple<string, string> FindFirstCapability(CapabilityRole role)
        {
            foreach (var plugin in _pluginEnvironment.Catalog.GetPlugins().OrderBy(p => p.GetMetadata().DisplayName, StringComparer.OrdinalIgnoreCase))
            {
                var capability = plugin.GetCapabilities().OrderBy(c => c.DisplayName, StringComparer.OrdinalIgnoreCase).FirstOrDefault(c => role == CapabilityRole.None || c.HasRole(role));
                if (capability != null)
                {
                    return Tuple.Create(plugin.GetMetadata().PluginId, capability.Id);
                }
            }

            return Tuple.Create<string, string>(null, null);
        }

        private static ParameterValues ExtractLegacyChildParameters(PipelineStepDefinition parent, string slotKey)
        {
            var values = new ParameterValues();
            if (parent == null || parent.Parameters == null)
            {
                return values;
            }

            var prefix = slotKey + ".param.";
            foreach (var kv in parent.Parameters.ToDictionary())
            {
                if (!kv.Key.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                values.Set(kv.Key.Substring(prefix.Length), kv.Value);
            }

            return values;
        }

        private void SyncLegacyChildBindings()
        {
            var parent = GetSelectedStep();
            if (parent == null || parent.Parameters == null)
            {
                return;
            }

            SyncLegacyChildBinding(parent, "encoder");
            SyncLegacyChildBinding(parent, "metric");
        }

        private static void SyncLegacyChildBinding(PipelineStepDefinition parent, string slotKey)
        {
            var child = GetChildStep(parent, slotKey);
            parent.Parameters.Set(slotKey + ".pluginId", child == null ? null : (object)child.PluginId);
            parent.Parameters.Set(slotKey + ".capabilityId", child == null ? null : (object)child.CapabilityId);

            var prefix = slotKey + ".param.";
            foreach (var key in parent.Parameters.ToDictionary().Keys.Where(k => k.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)).ToList())
            {
                parent.Parameters.Set(key, null);
            }

            if (child == null || child.Parameters == null)
            {
                return;
            }

            foreach (var kv in child.Parameters.ToDictionary())
            {
                parent.Parameters.Set(prefix + kv.Key, kv.Value);
            }
        }

        private void TryDefaultSearchParameter(PipelineStepDefinition parent, PipelineStepDefinition encoderStep)
        {
            if (parent == null || parent.Parameters == null || encoderStep == null)
            {
                return;
            }

            var currentKey = parent.Parameters.GetString("search.parameterKey", null);
            if (!string.IsNullOrWhiteSpace(currentKey))
            {
                return;
            }

            var encoderCap = _pluginEnvironment.Catalog.FindCapability(encoderStep.PluginId, encoderStep.CapabilityId);
            if (encoderCap == null || encoderCap.Parameters == null)
            {
                return;
            }

            var searchable = encoderCap.Parameters.Parameters.Where(d => d != null && d.IsSearchable).Select(d => d.Key).ToList();
            if (searchable.Count == 1)
            {
                parent.Parameters.Set("search.parameterKey", searchable[0]);
            }
        }

        private ParameterValues BuildInitialParameterValues(string pluginId, string capabilityId, ParameterValues existing)
        {
            var values = new ParameterValues();
            var cap = _pluginEnvironment.Catalog.FindCapability(pluginId, capabilityId);
            if (cap != null && cap.Parameters != null)
            {
                for (var i = 0; i < cap.Parameters.Parameters.Count; i++)
                {
                    var def = cap.Parameters.Parameters[i];
                    if (def == null || def.DefaultValue == null)
                    {
                        continue;
                    }

                    values.Set(def.Key, ConvertDefaultValue(def));
                }
            }

            if (existing != null)
            {
                foreach (var kv in existing.ToDictionary())
                {
                    values.Set(kv.Key, kv.Value);
                }
            }

            return values;
        }

        private static object ConvertDefaultValue(ParameterDefinition def)
        {
            if (def == null || def.DefaultValue == null)
            {
                return null;
            }

            if (def.Type == ParameterType.Boolean)
            {
                bool b;
                return bool.TryParse(def.DefaultValue, out b) ? (object)b : false;
            }

            if (def.Type == ParameterType.Int32)
            {
                int i;
                return int.TryParse(def.DefaultValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out i) ? (object)i : 0;
            }

            if (def.Type == ParameterType.Decimal)
            {
                decimal d;
                return decimal.TryParse(def.DefaultValue, NumberStyles.Number, CultureInfo.InvariantCulture, out d) ? (object)d : 0m;
            }

            return def.DefaultValue;
        }

        private static string BuildCapabilitySummary(CapabilityDescriptor cap)
        {
            if (cap == null)
            {
                return string.Empty;
            }

            return cap.DisplayName + Environment.NewLine +
                   (string.IsNullOrWhiteSpace(cap.Description) ? string.Empty : cap.Description + Environment.NewLine) +
                   "Kind: " + cap.Kind + Environment.NewLine +
                   "Input: " + string.Join(", ", cap.InputMediaTypes) + Environment.NewLine +
                   "Output: " + string.Join(", ", cap.OutputMediaTypes);
        }

        private void MaterializeLegacyOptimizerChildSteps()
        {
            if (_current == null || _current.Pipeline == null)
            {
                return;
            }

            for (var i = 0; i < _current.Pipeline.Steps.Count; i++)
            {
                var step = _current.Pipeline.Steps[i];
                var capability = step == null ? null : _pluginEnvironment.Catalog.FindCapability(step.PluginId, step.CapabilityId);
                if (!IsOptimizerLikeStep(step, capability))
                {
                    continue;
                }

                MaterializeLegacyChildStep(step, "encoder");
                MaterializeLegacyChildStep(step, "metric");
            }
        }

        private static void MaterializeLegacyChildStep(PipelineStepDefinition parent, string slotKey)
        {
            if (parent == null || string.IsNullOrWhiteSpace(slotKey) || GetChildStep(parent, slotKey) != null)
            {
                return;
            }

            var pluginId = parent.Parameters == null ? null : parent.Parameters.GetString(slotKey + ".pluginId", null);
            var capabilityId = parent.Parameters == null ? null : parent.Parameters.GetString(slotKey + ".capabilityId", null);
            if (string.IsNullOrWhiteSpace(pluginId) || string.IsNullOrWhiteSpace(capabilityId))
            {
                return;
            }

            parent.ChildSteps.Add(new PipelineStepDefinition(pluginId, capabilityId)
            {
                SlotKey = slotKey,
                Parameters = ExtractLegacyChildParameters(parent, slotKey)
            });
        }

        private void HandleCloseRequest()
        {
            if (!_isDirty)
            {
                _view.CloseView(System.Windows.Forms.DialogResult.Cancel);
                return;
            }

            var decision = _view.ConfirmKeepSessionChanges(_current == null ? null : _current.DisplayName);
            if (decision == System.Windows.Forms.DialogResult.Yes)
            {
                CommitSessionToWorkspaceAndClose();
                return;
            }

            if (decision == System.Windows.Forms.DialogResult.No)
            {
                _view.CloseView(System.Windows.Forms.DialogResult.Cancel);
                return;
            }

            if (decision == System.Windows.Forms.DialogResult.Cancel)
            {
                return;
            }
        }

        private void MarkDirty()
        {
            _isDirty = true;
        }

        private void MarkClean()
        {
            _isDirty = false;
        }

        private void RefreshValidationSummary()
        {
            _view.SetValidationSummary(BuildValidationSummaryText());
        }

        private string BuildValidationSummaryText()
        {
            var messages = BuildValidationMessages(false);
            if (messages.Count == 0)
            {
                return _isDirty ? "No validation issues. There are session changes that are not yet applied to the workspace." : "No validation issues.";
            }

            return string.Join(Environment.NewLine, messages);
        }

        private List<string> BuildValidationMessages(bool errorsOnly)
        {
            var messages = new List<string>();
            if (_current == null)
            {
                return messages;
            }

            if (string.IsNullOrWhiteSpace(_view.PresetIdText))
            {
                messages.Add("Error: Preset Id is required.");
            }

            if (string.IsNullOrWhiteSpace(_view.DisplayNameText) && !errorsOnly)
            {
                messages.Add("Warning: Display Name is empty.");
            }

            var duplicateCount = _presets.Count(p => p != null && string.Equals(p.PresetId, _view.PresetIdText, StringComparison.OrdinalIgnoreCase));
            if (!string.IsNullOrWhiteSpace(_view.PresetIdText) && duplicateCount > 1)
            {
                messages.Add("Error: Another preset in the editor session already uses this Preset Id.");
            }

            if (_current.Pipeline == null || _current.Pipeline.Steps.Count == 0)
            {
                messages.Add("Error: At least one pipeline step is required.");
            }

            if (_current.OutputPolicy == null)
            {
                messages.Add("Error: Output Policy is missing.");
            }
            else if (string.IsNullOrWhiteSpace(_current.OutputPolicy.DirectoryPath))
            {
                messages.Add("Error: Output Policy directory path is required.");
            }

            if (_current.Pipeline != null)
            {
                for (var i = 0; i < _current.Pipeline.Steps.Count; i++)
                {
                    ValidateStep(_current.Pipeline.Steps[i], i + 1, messages);
                }
            }

            if (!errorsOnly && !string.IsNullOrWhiteSpace(_current.OutputPolicy?.NamingPattern))
            {
                messages.Add("Info: Naming pattern is saved in the preset but is not yet applied by the runtime.");
            }

            if (_isDirty && !errorsOnly)
            {
                messages.Add("Info: Session changes are not yet applied to the workspace until you click OK.");
            }

            return errorsOnly ? messages.Where(m => m.StartsWith("Error:", StringComparison.Ordinal)).ToList() : messages;
        }

        private PipelineStepDefinition GetEffectiveChildStepForValidation(PipelineStepDefinition parent, string slotKey)
        {
            var child = GetChildStep(parent, slotKey);
            if (child != null)
            {
                return child;
            }

            if (parent == null || parent.Parameters == null || string.IsNullOrWhiteSpace(slotKey))
            {
                return null;
            }

            var pluginId = parent.Parameters.GetString(slotKey + ".pluginId", null);
            var capabilityId = parent.Parameters.GetString(slotKey + ".capabilityId", null);
            if (string.IsNullOrWhiteSpace(pluginId) || string.IsNullOrWhiteSpace(capabilityId))
            {
                return null;
            }

            return new PipelineStepDefinition(pluginId, capabilityId)
            {
                SlotKey = slotKey,
                Parameters = ExtractLegacyChildParameters(parent, slotKey)
            };
        }

        private void ValidateStep(PipelineStepDefinition step, int stepNumber, List<string> messages)
        {
            if (step == null)
            {
                messages.Add("Error: Step " + stepNumber.ToString(CultureInfo.InvariantCulture) + " is missing.");
                return;
            }

            var cap = _pluginEnvironment.Catalog.FindCapability(step.PluginId, step.CapabilityId);
            if (cap == null)
            {
                messages.Add("Error: Step " + stepNumber.ToString(CultureInfo.InvariantCulture) + " capability was not found: " + step.PluginId + "/" + step.CapabilityId);
                return;
            }

            var isOptimizerLike = IsOptimizerLikeStep(step, cap);
            foreach (var def in cap.Parameters.Parameters.Where(d => d != null && d.IsRequired))
            {
                if (isOptimizerLike && (def.Key.StartsWith("encoder.", StringComparison.OrdinalIgnoreCase) || def.Key.StartsWith("metric.", StringComparison.OrdinalIgnoreCase)))
                {
                    continue;
                }

                var value = step.Parameters == null ? null : step.Parameters.GetString(def.Key, null);
                if (string.IsNullOrWhiteSpace(value))
                {
                    messages.Add("Error: Step " + stepNumber.ToString(CultureInfo.InvariantCulture) + " is missing required parameter '" + def.Key + "'.");
                }
            }

            if (!isOptimizerLike)
            {
                return;
            }

            var encoder = GetEffectiveChildStepForValidation(step, "encoder");
            var metric = GetEffectiveChildStepForValidation(step, "metric");
            if (encoder == null)
            {
                messages.Add("Error: Optimizer step " + stepNumber.ToString(CultureInfo.InvariantCulture) + " is missing its encoder child step.");
            }
            else if (_pluginEnvironment.Catalog.FindCapability(encoder.PluginId, encoder.CapabilityId) == null)
            {
                messages.Add("Error: Optimizer step " + stepNumber.ToString(CultureInfo.InvariantCulture) + " encoder child capability was not found: " + encoder.PluginId + "/" + encoder.CapabilityId);
            }

            if (metric == null)
            {
                messages.Add("Error: Optimizer step " + stepNumber.ToString(CultureInfo.InvariantCulture) + " is missing its metric child step.");
            }
            else if (_pluginEnvironment.Catalog.FindCapability(metric.PluginId, metric.CapabilityId) == null)
            {
                messages.Add("Error: Optimizer step " + stepNumber.ToString(CultureInfo.InvariantCulture) + " metric child capability was not found: " + metric.PluginId + "/" + metric.CapabilityId);
            }

            var searchKey = step.Parameters == null ? null : step.Parameters.GetString("search.parameterKey", null);
            if (string.IsNullOrWhiteSpace(searchKey))
            {
                messages.Add("Error: Optimizer step " + stepNumber.ToString(CultureInfo.InvariantCulture) + " is missing search.parameterKey.");
            }

            var min = step.Parameters == null ? 0 : step.Parameters.GetInt32("search.min", 0);
            var max = step.Parameters == null ? 0 : step.Parameters.GetInt32("search.max", 0);
            if (min > max)
            {
                messages.Add("Error: Optimizer step " + stepNumber.ToString(CultureInfo.InvariantCulture) + " has search.min greater than search.max.");
            }

            var maxTries = step.Parameters == null ? 0 : step.Parameters.GetInt32("search.maxTries", 0);
            if (maxTries < 1)
            {
                messages.Add("Error: Optimizer step " + stepNumber.ToString(CultureInfo.InvariantCulture) + " must have search.maxTries >= 1.");
            }
        }

        private string MakeUniquePresetId(string baseId)
        {
            var root = string.IsNullOrWhiteSpace(baseId) ? "preset.copy" : baseId.Trim();
            var candidate = root;
            var counter = 2;
            while (_presets.Any(p => p != null && string.Equals(p.PresetId, candidate, StringComparison.OrdinalIgnoreCase)))
            {
                candidate = root + "." + counter.ToString(CultureInfo.InvariantCulture);
                counter++;
            }

            return candidate;
        }

        private string MakeUniqueDisplayName(string baseName)
        {
            var root = string.IsNullOrWhiteSpace(baseName) ? "Preset Copy" : baseName.Trim();
            var candidate = root;
            var counter = 2;
            while (_presets.Any(p => p != null && string.Equals(p.DisplayName, candidate, StringComparison.OrdinalIgnoreCase)))
            {
                candidate = root + " " + counter.ToString(CultureInfo.InvariantCulture);
                counter++;
            }

            return candidate;
        }

        private static string MakeSafeFileName(string presetId)
        {
            var invalid = Path.GetInvalidFileNameChars();
            var chars = (presetId ?? "preset").ToCharArray();
            for (var i = 0; i < chars.Length; i++)
            {
                if (invalid.Contains(chars[i]))
                {
                    chars[i] = '_';
                }
            }

            var result = new string(chars).Trim();
            return string.IsNullOrWhiteSpace(result) ? "preset" : result;
        }
    }
}
