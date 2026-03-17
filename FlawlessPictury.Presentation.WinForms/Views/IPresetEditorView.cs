using System;
using System.Collections.Generic;
using System.Windows.Forms;
using FlawlessPictury.AppCore.Presets;

namespace FlawlessPictury.Presentation.WinForms.Views
{
    internal interface IPresetEditorView
    {
        event EventHandler ViewReady;
        event EventHandler SelectedPresetChanged;
        event EventHandler NewPresetRequested;
        event EventHandler DuplicatePresetRequested;
        event EventHandler ReloadRequested;
        event EventHandler SaveRequested;
        event EventHandler SaveAsRequested;
        event EventHandler DeleteRequested;
        event EventHandler OpenPresetLocationRequested;
        event EventHandler AddStepRequested;
        event EventHandler RemoveStepRequested;
        event EventHandler MoveStepUpRequested;
        event EventHandler MoveStepDownRequested;
        event EventHandler SelectedStructureChanged;
        event EventHandler SelectedPluginChanged;
        event EventHandler SelectedCapabilityChanged;
        event EventHandler MetadataChanged;
        event EventHandler ParameterValuesChanged;
        event EventHandler EncoderPluginChanged;
        event EventHandler EncoderCapabilityChanged;
        event EventHandler EncoderParameterValuesChanged;
        event EventHandler MetricPluginChanged;
        event EventHandler MetricCapabilityChanged;
        event EventHandler MetricParameterValuesChanged;
        event EventHandler OutputPolicyChanged;
        event EventHandler OkRequested;
        event EventHandler CancelRequested;

        string PresetIdText { get; set; }
        string DisplayNameText { get; set; }
        string DescriptionText { get; set; }
        bool IsReadOnlyChecked { get; set; }
        string SelectedPluginId { get; }
        string SelectedCapabilityId { get; }
        string SelectedEncoderPluginId { get; }
        string SelectedEncoderCapabilityId { get; }
        string SelectedMetricPluginId { get; }
        string SelectedMetricCapabilityId { get; }
        int SelectedPresetIndex { get; }
        int SelectedStructureIndex { get; }

        void SetPresets(IReadOnlyList<PresetDefinition> presets);
        void SetPresetSelection(int index);
        void SetStructureItems(IReadOnlyList<PresetEditorStructureItem> items);
        void SetStructureSelection(int index);
        void SetPluginChoices(IReadOnlyList<PresetEditorChoiceItem> items, string selectedPluginId);
        void SetCapabilityChoices(IReadOnlyList<PresetEditorChoiceItem> items, string selectedCapabilityId);
        void SetParameterEditor(object selectedObject);
        void SetCapabilitySummary(string text);
        void SetValidationSummary(string text);
        void SetActionState(bool canSave, bool canSaveAs, bool canDelete, bool canDuplicate, bool canOpenLocation, bool canAddStep, bool canModifySelectedStep, bool canEditSelectors, bool canEditProperties);
        void SetOptimizerChildEditorsVisible(bool visible);
        void SetEncoderPluginChoices(IReadOnlyList<PresetEditorChoiceItem> items, string selectedPluginId);
        void SetEncoderCapabilityChoices(IReadOnlyList<PresetEditorChoiceItem> items, string selectedCapabilityId);
        void SetEncoderParameterEditor(object selectedObject);
        void SetEncoderSummary(string text);
        void SetMetricPluginChoices(IReadOnlyList<PresetEditorChoiceItem> items, string selectedPluginId);
        void SetMetricCapabilityChoices(IReadOnlyList<PresetEditorChoiceItem> items, string selectedCapabilityId);
        void SetMetricParameterEditor(object selectedObject);
        void SetMetricSummary(string text);
        void SetOutputPolicyEditor(object selectedObject);
        void SelectStepTab();
        void SelectEncoderTab();
        void SelectMetricTab();
        void SelectOutputTab();
        string PromptSaveAsPath(string initialDirectory, string initialFileName);
        bool ConfirmDelete(string presetName);
        DialogResult ConfirmKeepSessionChanges(string presetName);
        void ShowMessage(string message);
        void ShowError(string message);
        DialogResult ShowDialogView(object owner);
        void CloseView(DialogResult result);
    }
}
