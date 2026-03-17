using System;
using System.Collections.Generic;
using System.Drawing;

namespace FlawlessPictury.Presentation.WinForms.Views
{
    /// <summary>
    /// Passive View contract for the Runner window.
    /// </summary>
    public interface IRunnerView
    {
        event EventHandler ViewReady;

        event EventHandler AddFilesRequested;
        event EventHandler AddFolderRequested;
        event EventHandler RemoveSelectedRequested;
        event EventHandler ClearRequested;
        event EventHandler FilesDropped;

        event EventHandler PresetSelectionChanged;
        event EventHandler BrowseOutputRequested;
        event EventHandler OutputFolderTextChanged;
        event EventHandler RunRequested;

        /// <summary>
        /// Requests reloading plugins and presets. Bound to F5 in the default UI.
        /// </summary>
        event EventHandler ReloadRequested;

        event EventHandler OpenPluginExplorerRequested;
        event EventHandler OpenLogWindowRequested;

        event EventHandler SelectedInputChanged;
        event EventHandler PreviewPaneSelectionChanged;

        IReadOnlyList<string> DroppedPaths { get; }

        string SelectedPresetId { get; }

        void SetPresets(IReadOnlyList<PresetListItem> presets);
        void SetSelectedPresetId(string presetId);
        void SetCanRun(bool canRun);

        bool UseAutoOutputFolder { get; }
        string OutputFolderText { get; }

        void SetOutputFolderText(string text);
        void SetOutputChooserEnabled(bool enabled);

        void SetInputItems(IReadOnlyList<RunnerInputListItem> items);

        IReadOnlyList<string> GetSelectedInputPaths();
        string SelectedSingleInputPath { get; }
        bool IsNewPreviewSelected { get; }

        void SetStatus(string text);
        void SetProgress(int percent);
        void SetBusy(bool isBusy);

        void DisplayLogLine(string line);

        void ShowCriticalError(string message);

        // Compatibility methods used by the older single-preview presenter path.
        void SetPreviewImage(Image image);
        void SetProperties(IReadOnlyDictionary<string, string> props);

        // Dual-source preview API used by the presenter.
        void SetOriginalPreviewImage(Image image);
        void SetOriginalPreviewStatus(string text);
        void SetOriginalProperties(IReadOnlyDictionary<string, string> props);

        void SetNewPreviewImage(Image image);
        void SetNewPreviewStatus(string text);
        void SetNewProperties(IReadOnlyDictionary<string, string> props);
    }
}
