using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using FlawlessPictury.AppCore.CrossCutting;
using FlawlessPictury.AppCore.Plugins;
using FlawlessPictury.AppCore.Plugins.Artifacts;
using FlawlessPictury.AppCore.Plugins.Capabilities;
using FlawlessPictury.AppCore.Plugins.Execution;
using FlawlessPictury.AppCore.Plugins.Pipeline;
using FlawlessPictury.AppCore.Stats;
using FlawlessPictury.AppCore.Plugins.Parameters;
using FlawlessPictury.AppCore.Plugins.View;
using FlawlessPictury.AppCore.Presets;
using FlawlessPictury.Infrastructure.Plugins;
using FlawlessPictury.Infrastructure.Presets;
using FlawlessPictury.Infrastructure.SafeOutput;
using FlawlessPictury.Presentation.WinForms.CrossCutting;
using FlawlessPictury.Presentation.WinForms.Views;

namespace FlawlessPictury.Presentation.WinForms.Presenters
{
    /// <summary>
    /// Presenter for the main Runner screen (Passive View / MVP).
    ///
    /// Responsibilities:
    /// - Load plugins + presets on startup and on reload.
    /// - Keep presets visible even if dependencies are missing; report missing capabilities in log.
    /// - Disable Convert when the selected preset is unavailable, but allow user to select presets freely.
    /// - Execute the selected preset pipeline over the current input file list.
    /// </summary>
    public sealed class RunnerPresenter
    {
        private readonly IRunnerView _view;
        private readonly IUiDispatcher _dispatcher;
        private readonly ILogger _logger;
        private readonly ILogEventSource _logSource;
        private readonly IClock _clock;
        private readonly IProcessRunner _processRunner;
        private readonly PluginEnvironment _pluginEnv;
        private readonly PipelineExecutor _executor;
        private readonly IFileStager _stager;
        private readonly IOutputCommitter _committer;
        private readonly PresetWorkspace _presetWorkspace;
        private readonly int _maxConcurrentFiles;
        private readonly IRunStatsSink _statsSink;

        private readonly List<RunnerFileState> _files;
        private readonly object _filesSync = new object();
        private readonly List<PresetDefinition> _presets;
        private readonly Dictionary<string, string> _outputFolderOverrides;
        private readonly HashSet<int> _activeProcessingIndices;
        private int _activeProcessingFileCount;
        private readonly object _processingSlotSync = new object();
        private readonly HashSet<int> _leasedProcessingSlots = new HashSet<int>();

        private bool _isBusy;
        private readonly object _previewSync = new object();
        private readonly SemaphoreSlim _previewLoadGate = new SemaphoreSlim(1, 1);
        private readonly object _previewCacheSync = new object();
        private CancellationTokenSource _previewCts;
        private int _previewRequestId;
        private PreviewCacheEntry _previewCache;

        private const int PreviewSelectionDebounceMilliseconds = 180;

        public RunnerPresenter(
            IRunnerView view,
            IUiDispatcher dispatcher,
            ILogger logger,
            ILogEventSource logSource,
            IClock clock,
            PluginEnvironment pluginEnv,
            PipelineExecutor executor,
            IFileStager stager,
            IOutputCommitter committer,
            PresetWorkspace presetWorkspace,
            IProcessRunner processRunner,
            int maxConcurrentFiles,
            IRunStatsSink statsSink)
        {
            _view = view ?? throw new ArgumentNullException(nameof(view));
            _dispatcher = dispatcher ?? throw new ArgumentNullException(nameof(dispatcher));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _logSource = logSource ?? throw new ArgumentNullException(nameof(logSource));
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
            _processRunner = processRunner;
            _pluginEnv = pluginEnv ?? throw new ArgumentNullException(nameof(pluginEnv));
            _executor = executor ?? throw new ArgumentNullException(nameof(executor));
            _stager = stager ?? throw new ArgumentNullException(nameof(stager));
            _committer = committer ?? throw new ArgumentNullException(nameof(committer));
            _presetWorkspace = presetWorkspace ?? throw new ArgumentNullException(nameof(presetWorkspace));
            _maxConcurrentFiles = Math.Max(1, maxConcurrentFiles);
            _statsSink = statsSink;

            _files = new List<RunnerFileState>();
            _presets = new List<PresetDefinition>();
            _outputFolderOverrides = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            _activeProcessingIndices = new HashSet<int>();

            _view.ViewReady += OnViewReady;

            _view.AddFilesRequested += (s, e) => AddFilesViaDialog();
            _view.AddFolderRequested += (s, e) => AddFolderViaDialog();
            _view.RemoveSelectedRequested += (s, e) => RemoveSelected();
            _view.ClearRequested += (s, e) => ClearAll();
            _view.FilesDropped += (s, e) => AddPaths(_view.DroppedPaths);

            _view.PresetSelectionChanged += (s, e) => OnPresetSelectionChanged();
            _view.ReloadRequested += (s, e) => ReloadPluginsAndPresets();

            _view.BrowseOutputRequested += (s, e) => HandleOutputBrowseRequested();
            _view.OutputFolderTextChanged += (s, e) => OnOutputFolderTextChanged();
            _view.SelectedInputChanged += (s, e) => QueueUpdatePreviewAndProperties();

            _view.RunRequested += async (s, e) => await RunAsync().ConfigureAwait(false);

            // Reserved for UI features that may need structured runner completion notifications.
            _view.OpenPluginExplorerRequested += (s, e) => OpenPluginExplorerRequested?.Invoke(this, EventArgs.Empty);
            _view.OpenLogWindowRequested += (s, e) => OpenLogWindowRequested?.Invoke(this, EventArgs.Empty);

            _logSource.LogEmitted += (s, evt) =>
            {
                // Status bar during runs is intentionally driven by the currently processed file,
                // not by raw log lines. The subscription remains available for view-level coordination.
            };

            _presetWorkspace.Changed += (s, e) => Ui(() => LoadPresetsFromWorkspace());
        }


        public event EventHandler OpenPluginExplorerRequested;
        public event EventHandler OpenLogWindowRequested;

        private void OnViewReady(object sender, EventArgs e)
        {
            ReloadPluginsAndPresets();
            RefreshList();
            QueueUpdatePreviewAndProperties();
            UpdateRunEnabledForSelectedPreset();
            UpdateOutputFolderTextForSelection();
            UpdateIdleStatusText();

            Ui(() =>
            {
                _view.SetProgress(0);
            });
        }

        private void ReloadPluginsAndPresets()
        {
            var load = _pluginEnv.Reload();

            foreach (var w in load.Warnings)
            {
                _logger.Log(LogLevel.Warn, w);
            }

            foreach (var err in load.Errors)
            {
                _logger.Log(LogLevel.Error, err.ToString(), err.Exception);
            }

            var presetResult = _presetWorkspace.Reload(_logger);
            if (presetResult.IsFailure)
            {
                _logger.Log(LogLevel.Error, "Preset workspace reload failed: " + presetResult.Error.Message);
            }
        }

        private void LoadPresetsFromWorkspace()
        {
            _presets.Clear();

            IReadOnlyList<PresetDefinition> loaded = null;
            try
            {
                loaded = _presetWorkspace.GetSnapshot();
            }
            catch (Exception ex)
            {
                _logger.Log(LogLevel.Error, "Preset workspace error.", ex);
            }

            if (loaded != null)
            {
                _presets.AddRange(loaded);
            }

            var presetItems = new List<PresetListItem>();

            for (int i = 0; i < _presets.Count; i++)
            {
                var p = _presets[i];

                var missing = FindMissingCapabilities(p, _pluginEnv.Catalog);
                var available = missing.Count == 0;

                if (!available)
                {
                    _logger.Log(LogLevel.Warn, "Preset '" + p.PresetId + "' is unavailable (missing capabilities): " + string.Join(", ", missing));
                }

                presetItems.Add(new PresetListItem(p.PresetId, p.DisplayName, p.Description, available, missing));
            }

            var selectedPresetId = _presetWorkspace.GetSelectedPresetId();
            if (string.IsNullOrWhiteSpace(selectedPresetId))
            {
                selectedPresetId = _view.SelectedPresetId;
            }

            Ui(() =>
            {
                _view.SetPresets(presetItems);

                if (string.IsNullOrWhiteSpace(selectedPresetId))
                {
                    selectedPresetId = _view.SelectedPresetId;
                }

                if (!string.IsNullOrWhiteSpace(selectedPresetId))
                {
                    _view.SetSelectedPresetId(selectedPresetId);
                }

                if (presetItems.Count == 0)
                {
                    _view.SetCanRun(false);
                }
            });

            if (!string.IsNullOrWhiteSpace(selectedPresetId) && string.IsNullOrWhiteSpace(_presetWorkspace.GetSelectedPresetId()))
            {
                _presetWorkspace.SetSelectedPresetId(selectedPresetId);
            }

            UpdateRunEnabledForSelectedPreset();
            UpdateOutputFolderTextForSelection();
            UpdateIdleStatusText();
        }

        private PresetDefinition FindSelectedPreset()
        {
            var id = _view.SelectedPresetId;
            if (string.IsNullOrWhiteSpace(id)) return null;

            var found = _presets.FirstOrDefault(p => string.Equals(p.PresetId, id, StringComparison.OrdinalIgnoreCase));
            return found == null ? null : PresetWorkspace.ClonePreset(found);
        }

        private List<string> FindMissingCapabilities(PresetDefinition preset, PluginCatalog catalog)
        {
            var missing = new List<string>();

            if (preset == null || preset.Pipeline == null || preset.Pipeline.Steps == null)
            {
                return missing;
            }

            if (catalog == null)
            {
                missing.Add("catalog-unavailable");
                return missing;
            }

            for (int i = 0; i < preset.Pipeline.Steps.Count; i++)
            {
                var s = preset.Pipeline.Steps[i];
                if (s == null)
                {
                    continue;
                }

                var cap = catalog.FindCapability(s.PluginId, s.CapabilityId);
                if (cap == null)
                {
                    missing.Add(s.PluginId + "/" + s.CapabilityId);
                }
            }

            return missing;
        }

        
private int GetFileCount()
{
    lock (_filesSync)
    {
        return _files.Count;
    }
}

private bool HasAnyFiles()
{
    return GetFileCount() > 0;
}



/// <summary>
/// Handles preset selection changes. Keeps UI state updated and logs the user-visible selection.
/// </summary>
private void OnPresetSelectionChanged()
{
    try
    {
        _presetWorkspace.SetSelectedPresetId(_view.SelectedPresetId);
    }
    catch
    {
    }

    UpdateRunEnabledForSelectedPreset();
    UpdateOutputFolderTextForSelection();
    UpdateIdleStatusText();

    try
    {
        var selected = FindSelectedPreset();
        if (selected != null)
        {
            _logger.Log(LogLevel.Info, "Preset selected: " + selected.PresetId + " (" + selected.DisplayName + ")");
        }
    }
    catch
    {
        // Never allow logging to break the UI.
    }
}

private void UpdateRunEnabledForSelectedPreset()
{
    try
    {
        Ui(() => _view.SetCanRun(!_isBusy));
    }
    catch
    {
    }
}

private void UpdateIdleStatusText()
{
    if (_isBusy)
    {
        return;
    }

    string status;
    try
    {
        var selected = FindSelectedPreset();
        status = BuildIdleStatusText(selected);
    }
    catch
    {
        status = "Ready.";
    }

    Ui(() => _view.SetStatus(status));
}

private void SetFileProcessingState(int fileIndex, int fileCount, bool isProcessing)
{
    string status = null;

    lock (_filesSync)
    {
        if (isProcessing)
        {
            _activeProcessingFileCount = Math.Max(_activeProcessingFileCount, fileCount);
            _activeProcessingIndices.Add(fileIndex + 1);
        }
        else
        {
            _activeProcessingIndices.Remove(fileIndex + 1);
            if (_activeProcessingIndices.Count == 0)
            {
                _activeProcessingFileCount = 0;
            }
        }

        if (_activeProcessingIndices.Count > 0)
        {
            var ordered = _activeProcessingIndices.OrderBy(i => i).ToArray();
            status = "Processing file " + string.Join(", ", ordered.Select(i => i.ToString(CultureInfo.InvariantCulture)).ToArray()) +
                " of " + Math.Max(fileCount, _activeProcessingFileCount).ToString(CultureInfo.InvariantCulture) + ".";
        }
    }

    if (!string.IsNullOrWhiteSpace(status))
    {
        Ui(() => _view.SetStatus(status));
    }
}

private string BuildIdleStatusText(PresetDefinition preset)
{
    if (!HasAnyFiles())
    {
        return "1. Add Files   2. Choose Preset   3. Click Convert";
    }

    if (preset != null && !string.IsNullOrWhiteSpace(preset.Description))
    {
        return preset.Description;
    }

    if (preset != null && !string.IsNullOrWhiteSpace(preset.DisplayName))
    {
        return preset.DisplayName;
    }

    return "Choose Preset, then click Convert.";
}
private string BuildRunCompletionStatusText(int totalCount)
{
    int doneCount = 0;
    int warningCount = 0;
    int failedCount = 0;

    lock (_filesSync)
    {
        foreach (var file in _files)
        {
            var status = file == null ? string.Empty : (file.StatusText ?? string.Empty);
            if (status.StartsWith("Failed", StringComparison.OrdinalIgnoreCase))
            {
                failedCount++;
            }
            else if (status.StartsWith("Warning", StringComparison.OrdinalIgnoreCase))
            {
                warningCount++;
            }
            else if (status.StartsWith("Done", StringComparison.OrdinalIgnoreCase))
            {
                doneCount++;
            }
        }
    }

    return "Completed. Done " + doneCount.ToString(CultureInfo.InvariantCulture) +
        ", Warning " + warningCount.ToString(CultureInfo.InvariantCulture) +
        ", Failed " + failedCount.ToString(CultureInfo.InvariantCulture) +
        " of " + Math.Max(0, totalCount).ToString(CultureInfo.InvariantCulture) + ".";
}

private int AcquireProcessingSlot()
{
    lock (_processingSlotSync)
    {
        for (var slot = 1; slot <= Math.Max(1, _maxConcurrentFiles); slot++)
        {
            if (_leasedProcessingSlots.Add(slot))
            {
                return slot;
            }
        }

        var fallbackSlot = 1;
        while (_leasedProcessingSlots.Contains(fallbackSlot))
        {
            fallbackSlot++;
        }

        _leasedProcessingSlots.Add(fallbackSlot);
        return fallbackSlot;
    }
}

private void ReleaseProcessingSlot(int slotNumber)
{
    if (slotNumber <= 0)
    {
        return;
    }

    lock (_processingSlotSync)
    {
        _leasedProcessingSlots.Remove(slotNumber);
    }
}

        private void AddFilesViaDialog()
        {
            using (var dlg = new OpenFileDialog())
            {
                dlg.Title = "Add input files";
                dlg.Filter = "All files (*.*)|*.*";
                dlg.Multiselect = true;

                if (dlg.ShowDialog() == DialogResult.OK)
                {
                    AddPaths(dlg.FileNames);
                }
            }
        }

        private void AddFolderViaDialog()
        {
            using (var dlg = new FolderBrowserDialog())
            {
                dlg.Description = "Select a folder to add files from.";
                if (dlg.ShowDialog() == DialogResult.OK)
                {
                    AddPaths(new[] { dlg.SelectedPath });
                }
            }
        }

        private void AddPaths(IEnumerable<string> paths)
        {
            if (paths == null) return;

            var filesToAdd = new List<string>();
            var added = 0;

            foreach (var p in paths)
            {
                if (string.IsNullOrWhiteSpace(p)) continue;

                try
                {
                    if (File.Exists(p))
                    {
                        filesToAdd.Add(p);
                    }
                    else if (Directory.Exists(p))
                    {
                        filesToAdd.AddRange(CollectFilesFromDirectory(p));
                    }
                }
                catch (Exception ex)
                {
                    _logger.Log(LogLevel.Warn, "Failed to add path: " + p, ex);
                }
            }

            lock (_filesSync)
            {
                foreach (var filePath in filesToAdd)
                {
                    if (TryAddFile_NoLock(filePath))
                    {
                        added++;
                        _logger.Log(LogLevel.Debug, "Added file: " + filePath);
                    }
                }
            }

            if (added > 0)
            {
                _logger.Log(LogLevel.Info, "Added " + added + " file(s).");
            }

            RefreshList();
            QueueUpdatePreviewAndProperties();
            UpdateRunEnabledForSelectedPreset();
        }

        private List<string> CollectFilesFromDirectory(string directoryPath)
        {
            var files = new List<string>();
            if (string.IsNullOrWhiteSpace(directoryPath) || !Directory.Exists(directoryPath))
            {
                return files;
            }

            var recursive = false;
            if (DirectoryContainsSubdirectories(directoryPath))
            {
                var decision = PromptForRecursiveFolderAdd(directoryPath);
                if (decision == DialogResult.Cancel)
                {
                    _logger.Log(LogLevel.Info, "Skipped folder add: " + directoryPath);
                    return files;
                }

                recursive = decision == DialogResult.Yes;
            }

            _logger.Log(LogLevel.Debug, "Adding files from folder" + (recursive ? " recursively: " : ": ") + directoryPath);

            var pending = new Stack<string>();
            pending.Push(directoryPath);

            while (pending.Count > 0)
            {
                var current = pending.Pop();

                try
                {
                    var currentFiles = Directory.GetFiles(current);
                    Array.Sort(currentFiles, StringComparer.OrdinalIgnoreCase);
                    files.AddRange(currentFiles);
                }
                catch (Exception ex)
                {
                    _logger.Log(LogLevel.Warn, "Failed to enumerate files in folder: " + current, ex);
                }

                if (!recursive)
                {
                    continue;
                }

                try
                {
                    var subdirectories = Directory.GetDirectories(current);
                    Array.Sort(subdirectories, StringComparer.OrdinalIgnoreCase);
                    for (var i = subdirectories.Length - 1; i >= 0; i--)
                    {
                        pending.Push(subdirectories[i]);
                    }
                }
                catch (Exception ex)
                {
                    _logger.Log(LogLevel.Warn, "Failed to enumerate subfolders in folder: " + current, ex);
                }
            }

            return files;
        }

        private bool DirectoryContainsSubdirectories(string directoryPath)
        {
            try
            {
                return Directory.GetDirectories(directoryPath).Length > 0;
            }
            catch (Exception ex)
            {
                _logger.Log(LogLevel.Warn, "Failed to inspect subfolders in folder: " + directoryPath, ex);
                return false;
            }
        }

        private DialogResult PromptForRecursiveFolderAdd(string directoryPath)
        {
            try
            {
                return MessageBox.Show(
                    "The selected folder contains subfolders." + Environment.NewLine + Environment.NewLine +
                    directoryPath + Environment.NewLine + Environment.NewLine +
                    "Yes = add files recursively" + Environment.NewLine +
                    "No = add only files from the selected folder" + Environment.NewLine +
                    "Cancel = do not add this folder",
                    "Flawless Pictury",
                    MessageBoxButtons.YesNoCancel,
                    MessageBoxIcon.Question);
            }
            catch
            {
                return DialogResult.No;
            }
        }

        private bool TryAddFile_NoLock(string path)
        {
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path)) return false;

            if (_files.Any(f => string.Equals(f.FilePath, path, StringComparison.OrdinalIgnoreCase)))
            {
                return false;
            }

            long size = 0;
            try { size = new FileInfo(path).Length; } catch { size = 0; }

            _files.Add(new RunnerFileState(path, size));
            return true;
        }

        private void RemoveSelected()
        {
            var selected = _view.GetSelectedInputPaths();
            if (selected == null || selected.Count == 0) return;

            foreach (var path in selected)
            {
                if (!string.IsNullOrWhiteSpace(path))
                {
                    _logger.Log(LogLevel.Debug, "Removed file from list: " + path);
                }
            }

            lock (_filesSync)
            {
                _files.RemoveAll(f => selected.Any(s => string.Equals(s, f.FilePath, StringComparison.OrdinalIgnoreCase)));
            }

            RefreshList();
            QueueUpdatePreviewAndProperties();
            UpdateRunEnabledForSelectedPreset();
        }

        private void ClearAll()
        {
            var count = GetFileCount();
            if (count > 0)
            {
                _logger.Log(LogLevel.Debug, "Cleared input list. Removed " + count.ToString(CultureInfo.InvariantCulture) + " file(s).");
            }

            lock (_filesSync)
            {
                _files.Clear();
            }

            RefreshList();
            QueueUpdatePreviewAndProperties();
            UpdateRunEnabledForSelectedPreset();
        }

        private void RefreshList()
        {
            RunnerFileState[] snapshot;

            lock (_filesSync)
            {
                snapshot = _files.ToArray();
            }

            var items = snapshot.Select(f =>
                new RunnerInputListItem(
                    f.FilePath,
                    f.StatusText,
                    FormatBytes(f.OriginalSizeBytes),
                    f.NewSizeBytes.HasValue ? FormatBytes(f.NewSizeBytes.Value) : string.Empty,
                    GetNewFileDisplayText(f),
                    f.NewFilePath))
                .ToList();

            Ui(() => _view.SetInputItems(items));
        }

        private static string GetNewFileDisplayText(RunnerFileState fileState)
        {
            if (fileState == null || string.IsNullOrWhiteSpace(fileState.NewFilePath))
            {
                return string.Empty;
            }

            var displayPath = fileState.NewFilePath;
            if (fileState.ProducedOutputCount > 1)
            {
                return displayPath + " (+" + (fileState.ProducedOutputCount - 1).ToString(CultureInfo.InvariantCulture) + ")";
            }

            return displayPath;
        }

        private void HandleOutputBrowseRequested()
        {
            using (var dlg = new FolderBrowserDialog())
            {
                dlg.Description = "Select output folder for converted files.";

                var current = GetEffectiveOutputDirectorySetting(FindSelectedPreset());
                if (!string.IsNullOrWhiteSpace(current) && Path.IsPathRooted(current) && Directory.Exists(current))
                {
                    dlg.SelectedPath = current;
                }

                if (dlg.ShowDialog() == DialogResult.OK)
                {
                    var selectedPresetId = _view.SelectedPresetId;
                    if (!string.IsNullOrWhiteSpace(selectedPresetId))
                    {
                        _outputFolderOverrides[selectedPresetId] = dlg.SelectedPath;
                    }

                    _view.SetOutputFolderText(dlg.SelectedPath);
                }
            }

            _view.SetOutputChooserEnabled(true);
        }

        private void OnOutputFolderTextChanged()
        {
            var selectedPresetId = _view.SelectedPresetId;
            if (string.IsNullOrWhiteSpace(selectedPresetId))
            {
                return;
            }

            var text = (_view.OutputFolderText ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(text))
            {
                _outputFolderOverrides.Remove(selectedPresetId);
                return;
            }

            _outputFolderOverrides[selectedPresetId] = text;
        }

        private void UpdateOutputFolderTextForSelection()
        {
            var selected = FindSelectedPreset();
            var effective = GetEffectiveOutputDirectorySetting(selected);
            Ui(() =>
            {
                _view.SetOutputFolderText(effective);
                _view.SetOutputChooserEnabled(true);
            });
        }

        private string GetEffectiveOutputDirectorySetting(PresetDefinition preset)
        {
            var selectedPresetId = preset == null ? _view.SelectedPresetId : preset.PresetId;
            string overrideText;
            if (!string.IsNullOrWhiteSpace(selectedPresetId) && _outputFolderOverrides.TryGetValue(selectedPresetId, out overrideText) && !string.IsNullOrWhiteSpace(overrideText))
            {
                return overrideText;
            }

            var policy = preset == null ? null : preset.OutputPolicy;
            if (policy == null)
            {
                return @".\Flawless";
            }

            return string.IsNullOrWhiteSpace(policy.DirectoryPath) ? @".\Flawless" : policy.DirectoryPath;
        }


private bool PreflightForRun(out PresetDefinition preset, out List<string> missingCaps, out int fileCount, out string userMessage, out bool openLog)
{
    preset = null;
    missingCaps = new List<string>();
    fileCount = 0;
    userMessage = null;
    openLog = false;

    lock (_filesSync)
    {
        fileCount = _files.Count;
    }

    if (_presets.Count == 0)
    {
        userMessage = "No presets are available. Put JSON presets into the Presets folder and click Reload (F5).";
        return false;
    }

    if (fileCount == 0)
    {
        userMessage = "Add at least one input file first (drag && drop or File → Add Files).";
        return false;
    }

    preset = FindSelectedPreset();
    if (preset == null)
    {
        userMessage = "Select a preset first.";
        return false;
    }

    missingCaps = FindMissingCapabilities(preset, _pluginEnv.Catalog);
    if (missingCaps.Count > 0)
    {
        userMessage = "This preset can't run because required plugins/capabilities are missing. Open the Log window for details.";
        openLog = true;

        // Log details for advanced troubleshooting.
        for (int i = 0; i < missingCaps.Count; i++)
        {
            _logger.Log(LogLevel.Warn, "Preset '" + preset.PresetId + "' missing capability: " + missingCaps[i]);
        }

        return false;
    }

    return true;
}

private void RaiseOpenLogWindow()
{
    try
    {
        var handler = OpenLogWindowRequested;
        if (handler != null)
        {
            handler(this, EventArgs.Empty);
        }
    }
    catch
    {
    }
}

        private async Task RunAsync()
        {
            PresetDefinition preset;
            List<string> missingCaps;
            int fileCount;
            string userMessage;
            bool openLog;

            if (!PreflightForRun(out preset, out missingCaps, out fileCount, out userMessage, out openLog))
            {
                if (!string.IsNullOrWhiteSpace(userMessage))
                {
                    Ui(() =>
                    {
                        try
                        {
                            MessageBox.Show(
                                userMessage,
                                "Flawless Pictury",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Information);
                        }
                        catch
                        {
                        }
                    });
                }

                if (openLog)
                {
                    RaiseOpenLogWindow();
                }

                UpdateRunEnabledForSelectedPreset();
                return;
            }

            var filesSnapshot = GetFileSnapshot();
            var runId = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss_fff", CultureInfo.InvariantCulture) + "_" + Guid.NewGuid().ToString("N").Substring(0, 8);
            var failures = 0;
            var progressSync = new object();
            var fileProgress = new int[filesSnapshot.Length];

            try
            {
                var outputPath = GetEffectiveOutputDirectorySetting(preset);
                var outputMode = Path.IsPathRooted(outputPath) ? "Absolute" : "Relative";
                _logger.Log(LogLevel.Info,
                    "Convert clicked: preset=" + preset.PresetId +
                    " files=" + fileCount.ToString(CultureInfo.InvariantCulture) +
                    " outputMode=" + outputMode +
                    " outputPath=" + outputPath +
                    " maxParallelFiles=" + _maxConcurrentFiles.ToString(CultureInfo.InvariantCulture));
            }
            catch
            {
            }

            EmitStats(new StatsEvent(StatsEventKind.RunStarted)
            {
                RunId = runId,
                PresetId = preset.PresetId,
                Message = "Batch run started"
            }
            .Add("fileCount", filesSnapshot.Length.ToString(CultureInfo.InvariantCulture))
            .Add("maxConcurrentFiles", _maxConcurrentFiles.ToString(CultureInfo.InvariantCulture))
            .Add("outputDirectory", GetEffectiveOutputDirectorySetting(preset)));

            Ui(() =>
            {
                _isBusy = true;
                lock (_filesSync)
                {
                    _activeProcessingIndices.Clear();
                    _activeProcessingFileCount = filesSnapshot.Length;
                }
                CancelPreviewRequest();
                _view.SetBusy(true);
                _view.SetProgress(0);
                _view.SetStatus("Preparing batch...");
            });

            try
            {
                using (var throttle = new SemaphoreSlim(_maxConcurrentFiles))
                {
                    var tasks = new List<Task>();

                    for (var i = 0; i < filesSnapshot.Length; i++)
                    {
                        var fileIndex = i;
                        var fileState = filesSnapshot[fileIndex];
                        await throttle.WaitAsync().ConfigureAwait(false);

                        tasks.Add(Task.Run(async () =>
                        {
                            try
                            {
                                var ok = await ProcessSingleFileAsync(fileState, fileIndex, filesSnapshot.Length, preset, runId, fileProgress, progressSync).ConfigureAwait(false);
                                if (!ok)
                                {
                                    Interlocked.Increment(ref failures);
                                }
                            }
                            finally
                            {
                                throttle.Release();
                            }
                        }));
                    }

                    await Task.WhenAll(tasks).ConfigureAwait(false);
                }
            }
            finally
            {
                var ok = filesSnapshot.Length - failures;

                Ui(() =>
                {
                    _isBusy = false;
                    lock (_filesSync)
                    {
                        _activeProcessingIndices.Clear();
                        _activeProcessingFileCount = 0;
                    }
                    _view.SetBusy(false);
                    QueueUpdatePreviewAndProperties();
                    _view.SetProgress(100);
                });

                _logger.Log(LogLevel.Info, "Done. " + ok + " ok, " + failures + " failed.");
                EmitStats(new StatsEvent(StatsEventKind.RunCompleted)
                {
                    RunId = runId,
                    PresetId = preset.PresetId,
                    Message = "Batch run completed"
                }
                .Add("fileCount", filesSnapshot.Length.ToString(CultureInfo.InvariantCulture))
                .Add("okCount", ok.ToString(CultureInfo.InvariantCulture))
                .Add("failedCount", failures.ToString(CultureInfo.InvariantCulture)));
                RefreshList();
                UpdateRunEnabledForSelectedPreset();
                var completionStatus = BuildRunCompletionStatusText(filesSnapshot.Length);
                Ui(() => _view.SetStatus(completionStatus));
            }
        }

        private async Task<bool> ProcessSingleFileAsync(
            RunnerFileState fileState,
            int fileIndex,
            int fileCount,
            PresetDefinition preset,
            string runId,
            int[] fileProgress,
            object progressSync)
        {
            var fileId = (fileIndex + 1).ToString("0000", CultureInfo.InvariantCulture);
            var fileSw = System.Diagnostics.Stopwatch.StartNew();
            var processingSlot = AcquireProcessingSlot();
            try
            {
                using (ProcessingLogScope.Push(processingSlot, fileIndex + 1, fileCount, fileState.FilePath))
                {
                    _logger.Log(LogLevel.Info,
                        "Processing file " + (fileIndex + 1).ToString(CultureInfo.InvariantCulture) +
                        "/" + fileCount.ToString(CultureInfo.InvariantCulture) +
                        ": " + Path.GetFileName(fileState.FilePath));

            fileState.SetRunning();
            fileState.NewSizeBytes = null;
            fileState.NewFilePath = null;
            fileState.ProducedOutputCount = 0;
            RefreshList();
            UpdateOverallProgress(fileProgress, progressSync, fileIndex, 1);
            SetFileProcessingState(fileIndex, fileCount, true);

            EmitStats(new StatsEvent(StatsEventKind.FileStarted)
            {
                RunId = runId,
                PresetId = preset.PresetId,
                FileId = fileId,
                FilePath = fileState.FilePath,
                Message = "File processing started"
            }
            .Add("fileIndex", (fileIndex + 1).ToString(CultureInfo.InvariantCulture))
            .Add("fileCount", fileCount.ToString(CultureInfo.InvariantCulture))
            .Add("originalBytes", fileState.OriginalSizeBytes.ToString(CultureInfo.InvariantCulture)));

            var finalOutputDir = ResolveFinalOutputDirectoryForFile(fileState.FilePath, preset);
            if (string.IsNullOrWhiteSpace(finalOutputDir))
            {
                fileState.SetFailed("No output folder.");
                RefreshList();
                EmitStats(new StatsEvent(StatsEventKind.FileFailed)
                {
                    RunId = runId,
                    PresetId = preset.PresetId,
                    FileId = fileId,
                    FilePath = fileState.FilePath,
                    Message = "No output folder resolved"
                });
                UpdateOverallProgress(fileProgress, progressSync, fileIndex, 100);
                SetFileProcessingState(fileIndex, fileCount, false);
                return false;
            }

            try
            {
                Directory.CreateDirectory(finalOutputDir);
            }
            catch (Exception ex)
            {
                _logger.Log(LogLevel.Error, "Failed to create output folder.", ex);
                fileState.SetFailed("Cannot create output folder.");
                RefreshList();
                EmitStats(new StatsEvent(StatsEventKind.FileFailed)
                {
                    RunId = runId,
                    PresetId = preset.PresetId,
                    FileId = fileId,
                    FilePath = fileState.FilePath,
                    Message = "Cannot create output folder"
                }
                .Add("outputDirectory", finalOutputDir)
                .Add("error", ex.Message));
                UpdateOverallProgress(fileProgress, progressSync, fileIndex, 100);
                SetFileProcessingState(fileIndex, fileCount, false);
                return false;
            }

            var workspace = SafeOutputWorkspace.Create(Path.Combine(Path.GetTempPath(), "FlawlessPictury"));
            try
            {
                _logger.Log(LogLevel.Info, "Starting file " + (fileIndex + 1).ToString(CultureInfo.InvariantCulture) + "/" + fileCount.ToString(CultureInfo.InvariantCulture) + ": " + fileState.FilePath);

                var stagedPath = await _stager.StageAsync(fileState.FilePath, workspace.StageDirectory, CancellationToken.None).ConfigureAwait(false);
                _logger.Log(LogLevel.Debug, "Staged input: " + stagedPath);

                var ctx = new StepContext(
                    WorkflowMode.SafeOutput,
                    workingDirectory: workspace.RunRoot,
                    tempDirectory: workspace.TempDirectory,
                    outputDirectory: workspace.InternalOutputDirectory,
                    logger: _logger,
                    clock: _clock,
                    processRunner: _processRunner);

                ctx.StatsSink = _statsSink;
                ctx.Environment["PresetId"] = preset.PresetId ?? string.Empty;
                ctx.Environment["PresetName"] = preset.DisplayName ?? string.Empty;
                ctx.Environment["RunId"] = runId;
                ctx.Environment["FileId"] = fileId;
                ctx.Environment["InputSourcePath"] = fileState.FilePath ?? string.Empty;
                ctx.Environment["InputFileName"] = Path.GetFileName(fileState.FilePath) ?? string.Empty;
                ctx.Environment["OutputDirectory"] = finalOutputDir ?? string.Empty;

                var stagedMediaType = FlawlessPictury.AppCore.Plugins.Capabilities.MediaTypeGuesser.GuessFromFilePath(stagedPath);
                var primaryArtifact = Artifact.FromFilePath(stagedPath, stagedMediaType);
                var originalArtifact = CreateOriginalArtifact(fileState.FilePath);


                var pipelineProgress = new Progress<PipelineProgress>(p =>
                {
                    if (p == null)
                    {
                        return;
                    }

                    var filePercent = 1;
                    if (p.StepCount > 0 && p.StepProgress != null && p.StepProgress.Percent.HasValue)
                    {
                        var fraction = ((double)p.StepIndex + (p.StepProgress.Percent.Value / 100.0)) / Math.Max(1, p.StepCount);
                        filePercent = 1 + (int)Math.Round(fraction * 98.0, MidpointRounding.AwayFromZero);
                    }

                    UpdateOverallProgress(fileProgress, progressSync, fileIndex, Math.Max(1, Math.Min(99, filePercent)));
                });

                var execResult = await _executor.ExecutePipelineAsync(
                        preset.Pipeline,
                        new StepInput(primaryArtifact, originalArtifact, null, new ParameterValues()),
                        ctx,
                        progress: pipelineProgress,
                        cancellationToken: CancellationToken.None)
                    .ConfigureAwait(false);

                if (execResult.IsFailure)
                {
                    fileState.SetFailed(execResult.Error.Message);
                    _logger.Log(LogLevel.Error, "Pipeline failed for file: " + fileState.FilePath);
                    RefreshList();
                    EmitStats(new StatsEvent(StatsEventKind.FileFailed)
                    {
                        RunId = runId,
                        PresetId = preset.PresetId,
                        FileId = fileId,
                        FilePath = fileState.FilePath,
                        Message = "Pipeline execution failed"
                    }
                    .Add("error", execResult.Error == null ? null : execResult.Error.Message));
                    UpdateOverallProgress(fileProgress, progressSync, fileIndex, 100);
                    return false;
                }

                var produced = execResult.Value.ProducedArtifacts
                    .Where(a => a != null && !string.IsNullOrWhiteSpace(a.Locator))
                    .Where(a => IsUnderDirectory(a.Locator, workspace.RunRoot))
                    .ToList();

                if (produced.Count == 0)
                {
                    _logger.Log(LogLevel.Info, "No final output artifact was produced for file: " + fileState.FilePath + " (preset may have kept original input or executed analyzer-only steps).");
                    fileState.SetDoneNoOutput();
                    RefreshList();
                    EmitStats(new StatsEvent(StatsEventKind.FileCompleted)
                    {
                        RunId = runId,
                        PresetId = preset.PresetId,
                        FileId = fileId,
                        FilePath = fileState.FilePath,
                        Message = "File completed with no output"
                    }
                    .Add("durationMs", ((int)fileSw.ElapsedMilliseconds).ToString(CultureInfo.InvariantCulture))
                    .Add("status", "CompletedNoOutput")
                    .Add("outputFileName", null)
                    .Add("usedOriginalFallback", "false")
                    .Add("warningCount", "0")
                    .Add("tryCount", "0")
                    .Add("outputBytes", "0")
                    .Add("savedBytes", "0")
                    .Add("producedArtifacts", "0"));
                    UpdateOverallProgress(fileProgress, progressSync, fileIndex, 100);
                    SetFileProcessingState(fileIndex, fileCount, false);
                    return true;
                }

                var fallbackToInput = WasChosenFromInput(execResult.Value);
                var tryCount = TryGetTryCount(execResult.Value);
                var winningParameterKey = TryGetWinningParameterKey(execResult.Value);
                var winningParameterValue = TryGetWinningParameterValue(execResult.Value);
                var artifactToCommit = SelectArtifactToCommit(execResult.Value, produced, fileState.FilePath, stagedPath);
                if (artifactToCommit == null || string.IsNullOrWhiteSpace(artifactToCommit.Locator) || !File.Exists(artifactToCommit.Locator))
                {
                    fileState.SetFailed("Final output artifact does not exist.");
                    _logger.Log(LogLevel.Error, "Final output artifact does not exist for file: " + fileState.FilePath);
                    RefreshList();
                    EmitStats(new StatsEvent(StatsEventKind.FileFailed)
                    {
                        RunId = runId,
                        PresetId = preset.PresetId,
                        FileId = fileId,
                        FilePath = fileState.FilePath,
                        Message = "Final output artifact missing"
                    }
                    .Add("error", "Final output artifact does not exist."));
                    UpdateOverallProgress(fileProgress, progressSync, fileIndex, 100);
                    SetFileProcessingState(fileIndex, fileCount, false);
                    return false;
                }

                var preferredName = Path.GetFileName(artifactToCommit.Locator);
                var commit = await _committer.CommitFileAsync(artifactToCommit.Locator, finalOutputDir, preferredName, CancellationToken.None).ConfigureAwait(false);
                if (preset.OutputPolicy != null && preset.OutputPolicy.PreserveSourceFileTimes)
                {
                    TryApplySourceFileTimes(fileState.FilePath, commit.DestinationFilePath);
                }

                long totalBytes = 0;
                try
                {
                    totalBytes = new FileInfo(commit.DestinationFilePath).Length;
                }
                catch
                {
                }

                fileState.NewFilePath = commit.DestinationFilePath;
                fileState.ProducedOutputCount = 1;
                fileState.NewSizeBytes = totalBytes;

                _logger.Log(LogLevel.Info, "Committed output: " + commit.DestinationFilePath + (totalBytes > 0 ? " bytes=" + totalBytes.ToString(CultureInfo.InvariantCulture) : string.Empty));

                var nonFatalWarningText = fallbackToInput ? null : TryGetNonFatalWarningText(execResult.Value);
                var warningCount = fallbackToInput ? 0 : GetWarningCount(execResult.Value);
                if (!string.IsNullOrWhiteSpace(nonFatalWarningText))
                {
                    fileState.SetWarning(nonFatalWarningText);
                    _logger.Log(LogLevel.Warn, "Completed with warning: " + fileState.FilePath + " - " + nonFatalWarningText);
                }
                else
                {
                    fileState.SetDone();
                }

                _logger.Log(LogLevel.Info,
                    "Finished file: " + fileState.FilePath +
                    " outputBytes=" + (fileState.NewSizeBytes.HasValue ? fileState.NewSizeBytes.Value.ToString(CultureInfo.InvariantCulture) : "n/a"));
                RefreshList();

                EmitStats(new StatsEvent(StatsEventKind.FileCompleted)
                {
                    RunId = runId,
                    PresetId = preset.PresetId,
                    FileId = fileId,
                    FilePath = fileState.FilePath,
                    Message = "File completed"
                }
                .Add("durationMs", ((int)fileSw.ElapsedMilliseconds).ToString(CultureInfo.InvariantCulture))
                .Add("status", warningCount > 0 ? "CompletedWithWarning" : "Completed")
                .Add("outputFileName", Path.GetFileName(commit.DestinationFilePath))
                .Add("usedOriginalFallback", fallbackToInput ? "true" : "false")
                .Add("warningCount", warningCount.ToString(CultureInfo.InvariantCulture))
                .Add("tryCount", tryCount.ToString(CultureInfo.InvariantCulture))
                .Add("originalBytes", fileState.OriginalSizeBytes.ToString(CultureInfo.InvariantCulture))
                .Add("outputBytes", totalBytes.ToString(CultureInfo.InvariantCulture))
                .Add("savedBytes", Math.Max(0, fileState.OriginalSizeBytes - totalBytes).ToString(CultureInfo.InvariantCulture))
                .Add("producedArtifacts", produced.Count.ToString(CultureInfo.InvariantCulture))
                .Add("winningParameterKey", winningParameterKey)
                .Add("winningParameterValue", winningParameterValue));

                UpdateOverallProgress(fileProgress, progressSync, fileIndex, 100);
                SetFileProcessingState(fileIndex, fileCount, false);
                return true;
            }
            catch (Exception ex)
            {
                _logger.Log(LogLevel.Error, "Unexpected error while processing file: " + fileState.FilePath, ex);
                fileState.SetFailed("Unexpected error (see log).");
                RefreshList();
                EmitStats(new StatsEvent(StatsEventKind.FileFailed)
                {
                    RunId = runId,
                    PresetId = preset.PresetId,
                    FileId = fileId,
                    FilePath = fileState.FilePath,
                    Message = "Unexpected file processing error"
                }
                .Add("error", ex.Message));
                UpdateOverallProgress(fileProgress, progressSync, fileIndex, 100);
                SetFileProcessingState(fileIndex, fileCount, false);
                return false;
            }
            finally
            {
                workspace.TryCleanup();
                SetFileProcessingState(fileIndex, fileCount, false);
            }
                }
            }
            finally
            {
                ReleaseProcessingSlot(processingSlot);
            }
        }

        private RunnerFileState[] GetFileSnapshot()
        {
            lock (_filesSync)
            {
                return _files.ToArray();
            }
        }


        private Artifact CreateOriginalArtifact(string sourcePath)
        {
            var mediaType = FlawlessPictury.AppCore.Plugins.Capabilities.MediaTypeGuesser.GuessFromFilePath(sourcePath);
            var artifact = Artifact.FromFilePath(sourcePath, mediaType);
            PopulateOriginalArtifactMetadata(artifact, sourcePath);
            return artifact;
        }

        private static void PopulateOriginalArtifactMetadata(Artifact artifact, string sourcePath)
        {
            if (artifact == null || string.IsNullOrWhiteSpace(sourcePath) || !File.Exists(sourcePath))
            {
                return;
            }

            try
            {
                var info = new FileInfo(sourcePath);
                artifact.Metadata.Set("source.path", sourcePath);
                artifact.Metadata.Set("source.fileName", info.Name);
                artifact.Metadata.Set("source.extension", info.Extension);
                artifact.Metadata.Set("source.directory", info.DirectoryName ?? string.Empty);
                artifact.Metadata.Set("source.length", info.Length.ToString(CultureInfo.InvariantCulture));
                artifact.Metadata.Set("source.creationTimeUtc", info.CreationTimeUtc.ToString("o", CultureInfo.InvariantCulture));
                artifact.Metadata.Set("source.lastWriteTimeUtc", info.LastWriteTimeUtc.ToString("o", CultureInfo.InvariantCulture));
                artifact.Metadata.Set("source.lastAccessTimeUtc", info.LastAccessTimeUtc.ToString("o", CultureInfo.InvariantCulture));
                artifact.Metadata.Set("source.attributes", info.Attributes.ToString());
            }
            catch
            {
            }
        }

        private void UpdateOverallProgress(int[] fileProgress, object progressSync, int fileIndex, int filePercent)
        {
            if (fileProgress == null || fileIndex < 0 || fileIndex >= fileProgress.Length)
            {
                return;
            }

            int overall;
            lock (progressSync)
            {
                fileProgress[fileIndex] = Math.Max(0, Math.Min(100, filePercent));
                overall = (int)Math.Round(fileProgress.Average(), MidpointRounding.AwayFromZero);
            }

            Ui(() => _view.SetProgress(overall));
        }

        private void EmitStats(StatsEvent statsEvent)
        {
            if (_statsSink == null || statsEvent == null)
            {
                return;
            }

            try
            {
                _statsSink.Emit(statsEvent);
            }
            catch
            {
            }
        }

        private void TryApplySourceFileTimes(string sourcePath, string destinationPath)
        {
            if (string.IsNullOrWhiteSpace(sourcePath) || string.IsNullOrWhiteSpace(destinationPath) || !File.Exists(sourcePath) || !File.Exists(destinationPath))
            {
                return;
            }

            try
            {
                var sourceInfo = new FileInfo(sourcePath);
                File.SetCreationTimeUtc(destinationPath, sourceInfo.CreationTimeUtc);
                File.SetLastWriteTimeUtc(destinationPath, sourceInfo.LastWriteTimeUtc);
                File.SetLastAccessTimeUtc(destinationPath, sourceInfo.LastAccessTimeUtc);
                _logger.Log(LogLevel.Debug, "Preserved source file times on committed output: " + destinationPath);
            }
            catch (Exception ex)
            {
                _logger.Log(LogLevel.Warn, "Failed to preserve source file times on committed output.", ex);
            }
        }

        private string ResolveFinalOutputDirectoryForFile(string inputFilePath, PresetDefinition preset)
        {
            if (string.IsNullOrWhiteSpace(inputFilePath)) return null;

            try
            {
                var setting = GetEffectiveOutputDirectorySetting(preset);
                if (string.IsNullOrWhiteSpace(setting))
                {
                    setting = @".\Flawless";
                }

                if (Path.IsPathRooted(setting))
                {
                    return setting;
                }

                var inputDir = Path.GetDirectoryName(inputFilePath);
                if (string.IsNullOrWhiteSpace(inputDir))
                {
                    return null;
                }

                return Path.GetFullPath(Path.Combine(inputDir, setting));
            }
            catch
            {
                return null;
            }
        }

        private string GetNewFilePathForSelectedInput(string originalPath)
        {
            if (string.IsNullOrWhiteSpace(originalPath))
            {
                return null;
            }

            lock (_filesSync)
            {
                var match = _files.FirstOrDefault(f => string.Equals(f.FilePath, originalPath, StringComparison.OrdinalIgnoreCase));
                return match == null ? null : match.NewFilePath;
            }
        }

        private void QueueUpdatePreviewAndProperties()
        {
            var originalPath = _view.SelectedSingleInputPath;
            var preset = FindSelectedPreset();
            var newFilePath = GetNewFilePathForSelectedInput(originalPath);

            CancelPreviewRequest();

            if (_isBusy || string.IsNullOrWhiteSpace(originalPath) || !File.Exists(originalPath))
            {
                ClearPreviewPanels();
                return;
            }

            var cached = TryCreateCachedPreviewSnapshot(originalPath, newFilePath, preset);
            if (cached != null)
            {
                ApplyPreviewSnapshot(cached);
                return;
            }

            _view.SetOriginalPreviewImage(null);
            _view.SetOriginalPreviewStatus("Loading...");
            _view.SetOriginalProperties(new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) { { "Status", "Loading..." } });

            if (!string.IsNullOrWhiteSpace(newFilePath) && File.Exists(newFilePath))
            {
                _view.SetNewPreviewImage(null);
                _view.SetNewPreviewStatus("Loading...");
                _view.SetNewProperties(new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) { { "Status", "Loading..." } });
            }
            else
            {
                _view.SetNewPreviewImage(null);
                _view.SetNewPreviewStatus("No converted file yet.");
                _view.SetNewProperties(new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) { { "Status", "No converted file yet." } });
            }

            var requestId = Interlocked.Increment(ref _previewRequestId);
            var cts = new CancellationTokenSource();
            lock (_previewSync)
            {
                _previewCts = cts;
            }

            var presetSnapshot = preset == null ? null : PresetWorkspace.ClonePreset(preset);
            Task.Run(() => LoadAndApplyPreviewSnapshotAsync(originalPath, newFilePath, presetSnapshot, requestId, cts.Token), cts.Token);
        }

        private void ClearPreviewPanels()
        {
            _view.SetOriginalPreviewStatus(string.Empty);
            _view.SetOriginalPreviewImage(null);
            _view.SetOriginalProperties(new Dictionary<string, string>());
            _view.SetNewPreviewStatus(string.Empty);
            _view.SetNewPreviewImage(null);
            _view.SetNewProperties(new Dictionary<string, string>());
        }

        private void CancelPreviewRequest()
        {
            CancellationTokenSource toCancel = null;
            lock (_previewSync)
            {
                toCancel = _previewCts;
                _previewCts = null;
            }

            if (toCancel != null)
            {
                try { toCancel.Cancel(); } catch { }
                try { toCancel.Dispose(); } catch { }
            }
        }

        private async Task LoadAndApplyPreviewSnapshotAsync(string originalPath, string newFilePath, PresetDefinition preset, int requestId, CancellationToken cancellationToken)
        {
            PreviewBundleSnapshot snapshot = null;
            try
            {
                await Task.Delay(PreviewSelectionDebounceMilliseconds, cancellationToken).ConfigureAwait(false);
                await _previewLoadGate.WaitAsync(cancellationToken).ConfigureAwait(false);
                try
                {
                    if (cancellationToken.IsCancellationRequested || requestId != _previewRequestId)
                    {
                        return;
                    }

                    snapshot = BuildPreviewBundleSnapshot(originalPath, newFilePath, preset, cancellationToken);
                }
                finally
                {
                    _previewLoadGate.Release();
                }

                if (snapshot == null || cancellationToken.IsCancellationRequested || requestId != _previewRequestId)
                {
                    snapshot?.Dispose();
                    return;
                }

                StorePreviewCache(originalPath, newFilePath, preset, snapshot);

                Ui(() =>
                {
                    if (requestId != _previewRequestId)
                    {
                        snapshot?.Dispose();
                        return;
                    }

                    ApplyPreviewSnapshot(snapshot);
                    snapshot = null;
                });
            }
            catch (OperationCanceledException)
            {
                snapshot?.Dispose();
            }
            catch (Exception ex)
            {
                snapshot?.Dispose();
                if (!cancellationToken.IsCancellationRequested && requestId == _previewRequestId)
                {
                    _logger.Log(LogLevel.Warn, "Failed to load preview/properties.", ex);
                    Ui(() =>
                    {
                        if (requestId != _previewRequestId)
                        {
                            return;
                        }

                        _view.SetOriginalPreviewStatus("Preview unavailable.");
                        _view.SetOriginalPreviewImage(null);
                        _view.SetOriginalProperties(new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) { { "Status", "Preview unavailable." } });
                        _view.SetNewPreviewStatus(string.IsNullOrWhiteSpace(newFilePath) ? "No converted file yet." : "Preview unavailable.");
                        _view.SetNewPreviewImage(null);
                        _view.SetNewProperties(new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                        {
                            { "Status", string.IsNullOrWhiteSpace(newFilePath) ? "No converted file yet." : "Preview unavailable." }
                        });
                    });
                }
            }
        }

        private void ApplyPreviewSnapshot(PreviewBundleSnapshot snapshot)
        {
            if (snapshot == null)
            {
                ClearPreviewPanels();
                return;
            }

            _view.SetOriginalPreviewStatus(snapshot.Original.StatusText);
            _view.SetOriginalPreviewImage(snapshot.Original.Image == null ? null : (Image)snapshot.Original.Image.Clone());
            _view.SetOriginalProperties(new Dictionary<string, string>(snapshot.Original.Properties, StringComparer.OrdinalIgnoreCase));
            _view.SetNewPreviewStatus(snapshot.New.StatusText);
            _view.SetNewPreviewImage(snapshot.New.Image == null ? null : (Image)snapshot.New.Image.Clone());
            _view.SetNewProperties(new Dictionary<string, string>(snapshot.New.Properties, StringComparer.OrdinalIgnoreCase));
        }

        private PreviewBundleSnapshot BuildPreviewBundleSnapshot(string originalPath, string newFilePath, PresetDefinition preset, CancellationToken cancellationToken)
        {
            var original = BuildSinglePreviewSnapshot(originalPath, preset, cancellationToken, missingFileStatus: "File not available.");
            var converted = string.IsNullOrWhiteSpace(newFilePath) || !File.Exists(newFilePath)
                ? new PreviewPaneSnapshot(new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    { "Status", "No converted file yet." }
                }, null, "No converted file yet.")
                : BuildSinglePreviewSnapshot(newFilePath, preset, cancellationToken, missingFileStatus: "Converted file not available.");

            return new PreviewBundleSnapshot(original, converted);
        }

        private PreviewPaneSnapshot BuildSinglePreviewSnapshot(string path, PresetDefinition preset, CancellationToken cancellationToken, string missingFileStatus)
        {
            var properties = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            Image image = null;
            var status = string.Empty;

            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            {
                properties["Status"] = missingFileStatus;
                return new PreviewPaneSnapshot(properties, null, missingFileStatus);
            }

            string tempRoot = null;
            try
            {
                if (preset != null)
                {
                    tempRoot = Path.Combine(Path.GetTempPath(), "FlawlessPictury", "PreviewProviders", Guid.NewGuid().ToString("N"));
                    var workDir = Path.Combine(tempRoot, "work");
                    var tempDir = Path.Combine(tempRoot, "temp");
                    var outputDir = Path.Combine(tempRoot, "output");
                    Directory.CreateDirectory(workDir);
                    Directory.CreateDirectory(tempDir);
                    Directory.CreateDirectory(outputDir);

                    var inputArtifact = Artifact.FromFilePath(path, MediaTypeGuesser.GuessFromFilePath(path));
                    PopulateSourceArtifactMetadata(inputArtifact, path);
                    var context = new StepContext(WorkflowMode.SafeOutput, workDir, tempDir, outputDir, PreviewLogger.Instance, _clock, _processRunner);
                    context.Environment["PresetId"] = preset.PresetId ?? string.Empty;
                    context.Environment["RunId"] = "preview-provider-" + Guid.NewGuid().ToString("N");

                    if (preset.PreviewProviderRef != null && !preset.PreviewProviderRef.IsEmpty)
                    {
                        try
                        {
                            var previewOutput = ExecuteProviderStep(preset.PreviewProviderRef, inputArtifact, context, cancellationToken);
                            if (previewOutput != null && previewOutput.ImageView != null && previewOutput.ImageView.HasImage)
                            {
                                image = TryLoadPreviewImage(previewOutput.ImageView);
                            }
                        }
                        catch (OperationCanceledException)
                        {
                            throw;
                        }
                        catch
                        {
                        }
                    }

                    if (preset.PropertiesProviderRefs != null)
                    {
                        for (var i = 0; i < preset.PropertiesProviderRefs.Count; i++)
                        {
                            cancellationToken.ThrowIfCancellationRequested();
                            var provider = preset.PropertiesProviderRefs[i];
                            if (provider == null || provider.IsEmpty)
                            {
                                continue;
                            }

                            try
                            {
                                var output = ExecuteProviderStep(provider, inputArtifact, context, cancellationToken);
                                AppendProperties(properties, output == null ? null : output.Properties);
                            }
                            catch (OperationCanceledException)
                            {
                                throw;
                            }
                            catch
                            {
                            }
                        }
                    }
                }

                if (image == null)
                {
                    image = TryLoadImageDirectFromFile(path);
                }

                AppendBasicFileProperties(properties, path, image);
                if (image == null)
                {
                    status = "No preview available.";
                    if (!properties.ContainsKey("Status"))
                    {
                        properties["Status"] = status;
                    }
                }
            }
            finally
            {
                if (!string.IsNullOrWhiteSpace(tempRoot))
                {
                    try
                    {
                        if (Directory.Exists(tempRoot))
                        {
                            Directory.Delete(tempRoot, true);
                        }
                    }
                    catch
                    {
                    }
                }
            }

            return new PreviewPaneSnapshot(properties, image, status);
        }

        private StepOutput ExecuteProviderStep(CapabilityReference providerRef, Artifact inputArtifact, StepContext context, CancellationToken cancellationToken)
        {
            if (providerRef == null || providerRef.IsEmpty)
            {
                return null;
            }

            var stepDefinition = new PipelineStepDefinition(providerRef.PluginId, providerRef.CapabilityId)
            {
                Parameters = new ParameterValues()
            };
            var input = new StepInput(inputArtifact, inputArtifact, null, stepDefinition.Parameters);
            var result = _executor.ExecuteStepAsync(stepDefinition, input, context, null, cancellationToken).GetAwaiter().GetResult();
            if (result.IsFailure)
            {
                throw new InvalidOperationException(result.Error == null ? "Provider step failed." : result.Error.Message);
            }

            return result.Value;
        }

        private PreviewBundleSnapshot TryCreateCachedPreviewSnapshot(string originalPath, string newFilePath, PresetDefinition preset)
        {
            try
            {
                var originalInfo = new FileInfo(originalPath);
                FileInfo newInfo = null;
                if (!string.IsNullOrWhiteSpace(newFilePath) && File.Exists(newFilePath))
                {
                    newInfo = new FileInfo(newFilePath);
                }

                var presetId = preset == null ? string.Empty : preset.PresetId ?? string.Empty;
                lock (_previewCacheSync)
                {
                    if (_previewCache == null)
                    {
                        return null;
                    }

                    if (!string.Equals(_previewCache.OriginalFilePath, originalPath, StringComparison.OrdinalIgnoreCase) ||
                        !string.Equals(_previewCache.NewFilePath, newFilePath ?? string.Empty, StringComparison.OrdinalIgnoreCase) ||
                        !string.Equals(_previewCache.PresetId, presetId, StringComparison.OrdinalIgnoreCase))
                    {
                        return null;
                    }

                    if (_previewCache.OriginalFileLength != originalInfo.Length || _previewCache.OriginalLastWriteUtc != originalInfo.LastWriteTimeUtc)
                    {
                        return null;
                    }

                    if (newInfo == null)
                    {
                        if (_previewCache.NewFileLength.HasValue || _previewCache.NewLastWriteUtc.HasValue)
                        {
                            return null;
                        }
                    }
                    else if (_previewCache.NewFileLength != newInfo.Length || _previewCache.NewLastWriteUtc != newInfo.LastWriteTimeUtc)
                    {
                        return null;
                    }

                    return _previewCache.CreateSnapshot();
                }
            }
            catch
            {
                return null;
            }
        }

        private void StorePreviewCache(string originalPath, string newFilePath, PresetDefinition preset, PreviewBundleSnapshot snapshot)
        {
            if (snapshot == null)
            {
                return;
            }

            try
            {
                var originalInfo = new FileInfo(originalPath);
                FileInfo newInfo = null;
                if (!string.IsNullOrWhiteSpace(newFilePath) && File.Exists(newFilePath))
                {
                    newInfo = new FileInfo(newFilePath);
                }

                var cacheEntry = new PreviewCacheEntry(
                    originalPath,
                    newFilePath,
                    preset == null ? string.Empty : preset.PresetId ?? string.Empty,
                    originalInfo.Length,
                    originalInfo.LastWriteTimeUtc,
                    newInfo == null ? (long?)null : newInfo.Length,
                    newInfo == null ? (DateTime?)null : newInfo.LastWriteTimeUtc,
                    snapshot);

                lock (_previewCacheSync)
                {
                    var old = _previewCache;
                    _previewCache = cacheEntry;
                    old?.Dispose();
                }
            }
            catch
            {
            }
        }

        private static Image TryLoadImageDirectFromFile(string path)
        {
            try
            {
                using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                using (var img = Image.FromStream(fs, false, false))
                {
                    return (Image)img.Clone();
                }
            }
            catch
            {
                return null;
            }
        }

        private static void AppendBasicFileProperties(IDictionary<string, string> properties, string path, Image image)
        {
            if (properties == null || string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            {
                return;
            }

            try
            {
                var info = new FileInfo(path);
                if (!properties.ContainsKey("File Name")) properties["File Name"] = info.Name;
                if (!properties.ContainsKey("Directory")) properties["Directory"] = info.DirectoryName ?? string.Empty;
                if (!properties.ContainsKey("Extension")) properties["Extension"] = info.Extension;
                if (!properties.ContainsKey("Size")) properties["Size"] = FormatBytes(info.Length);
                if (!properties.ContainsKey("Modified")) properties["Modified"] = info.LastWriteTime.ToString(CultureInfo.CurrentCulture);
                if (image != null && !properties.ContainsKey("Dimensions"))
                {
                    properties["Dimensions"] = image.Width.ToString(CultureInfo.InvariantCulture) + " x " + image.Height.ToString(CultureInfo.InvariantCulture);
                }
            }
            catch
            {
            }
        }

        private static void AppendProperties(IDictionary<string, string> target, IList<PropertyEntry> entries)
        {
            if (target == null || entries == null)
            {
                return;
            }

            for (var i = 0; i < entries.Count; i++)
            {
                var entry = entries[i];
                if (entry == null || string.IsNullOrWhiteSpace(entry.Name))
                {
                    continue;
                }

                var key = string.IsNullOrWhiteSpace(entry.Group) ? entry.Name.Trim() : (entry.Group.Trim() + ": " + entry.Name.Trim());
                if (string.IsNullOrWhiteSpace(key))
                {
                    continue;
                }

                var uniqueKey = key;
                var suffix = 2;
                while (target.ContainsKey(uniqueKey))
                {
                    uniqueKey = key + " (" + suffix.ToString(CultureInfo.InvariantCulture) + ")";
                    suffix++;
                }

                target[uniqueKey] = entry.Value ?? string.Empty;
            }
        }

        private static Image TryLoadPreviewImage(ImageViewData view)
        {
            if (view == null || !view.HasImage)
            {
                return null;
            }

            using (var ms = new MemoryStream(view.EncodedBytes, false))
            using (var img = Image.FromStream(ms, false, false))
            {
                return (Image)img.Clone();
            }
        }

        private static void PopulateSourceArtifactMetadata(Artifact artifact, string path)
        {
            if (artifact == null || string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            {
                return;
            }

            try
            {
                var info = new FileInfo(path);
                artifact.Metadata.Set("source.path", path);
                artifact.Metadata.Set("source.name", info.Name);
                artifact.Metadata.Set("source.extension", info.Extension);
                artifact.Metadata.Set("source.directory", info.DirectoryName ?? string.Empty);
                artifact.Metadata.Set("source.sizeBytes", info.Length.ToString(CultureInfo.InvariantCulture));
                artifact.Metadata.Set("source.creationUtc", info.CreationTimeUtc.ToString("o", CultureInfo.InvariantCulture));
                artifact.Metadata.Set("source.lastWriteUtc", info.LastWriteTimeUtc.ToString("o", CultureInfo.InvariantCulture));
                artifact.Metadata.Set("source.lastAccessUtc", info.LastAccessTimeUtc.ToString("o", CultureInfo.InvariantCulture));
                artifact.Metadata.Set("source.attributes", info.Attributes.ToString());
            }
            catch
            {
            }
        }

        private sealed class PreviewPaneSnapshot : IDisposable
        {
            public PreviewPaneSnapshot(Dictionary<string, string> properties, Image image, string statusText)
            {
                Properties = properties ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                Image = image;
                StatusText = statusText ?? string.Empty;
            }

            public Dictionary<string, string> Properties { get; private set; }
            public Image Image { get; private set; }
            public string StatusText { get; private set; }

            public PreviewPaneSnapshot Clone()
            {
                return new PreviewPaneSnapshot(
                    new Dictionary<string, string>(Properties, StringComparer.OrdinalIgnoreCase),
                    Image == null ? null : (Image)Image.Clone(),
                    StatusText);
            }

            public void Dispose()
            {
                if (Image != null)
                {
                    Image.Dispose();
                    Image = null;
                }
            }
        }

        private sealed class PreviewBundleSnapshot : IDisposable
        {
            public PreviewBundleSnapshot(PreviewPaneSnapshot original, PreviewPaneSnapshot converted)
            {
                Original = original ?? new PreviewPaneSnapshot(new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase), null, string.Empty);
                New = converted ?? new PreviewPaneSnapshot(new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase), null, string.Empty);
            }

            public PreviewPaneSnapshot Original { get; private set; }
            public PreviewPaneSnapshot New { get; private set; }

            public PreviewBundleSnapshot Clone()
            {
                return new PreviewBundleSnapshot(Original.Clone(), New.Clone());
            }

            public void Dispose()
            {
                Original?.Dispose();
                New?.Dispose();
            }
        }

        private sealed class PreviewCacheEntry : IDisposable
        {
            public PreviewCacheEntry(string originalFilePath, string newFilePath, string presetId, long originalFileLength, DateTime originalLastWriteUtc, long? newFileLength, DateTime? newLastWriteUtc, PreviewBundleSnapshot snapshot)
            {
                OriginalFilePath = originalFilePath ?? string.Empty;
                NewFilePath = newFilePath ?? string.Empty;
                PresetId = presetId ?? string.Empty;
                OriginalFileLength = originalFileLength;
                OriginalLastWriteUtc = originalLastWriteUtc;
                NewFileLength = newFileLength;
                NewLastWriteUtc = newLastWriteUtc;
                Snapshot = snapshot == null ? null : snapshot.Clone();
            }

            public string OriginalFilePath { get; private set; }
            public string NewFilePath { get; private set; }
            public string PresetId { get; private set; }
            public long OriginalFileLength { get; private set; }
            public DateTime OriginalLastWriteUtc { get; private set; }
            public long? NewFileLength { get; private set; }
            public DateTime? NewLastWriteUtc { get; private set; }
            public PreviewBundleSnapshot Snapshot { get; private set; }

            public PreviewBundleSnapshot CreateSnapshot()
            {
                return Snapshot == null ? null : Snapshot.Clone();
            }

            public void Dispose()
            {
                Snapshot?.Dispose();
                Snapshot = null;
            }
        }

        private sealed class PreviewLogger : ILogger
        {
            public static readonly PreviewLogger Instance = new PreviewLogger();

            private PreviewLogger()
            {
            }

            public void Log(LogLevel level, string message, Exception exception = null)
            {
            }
        }

        private static bool IsUnderDirectory(string filePath, string directory)
        {
            try
            {
                var fullFile = Path.GetFullPath(filePath).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                var fullDir = Path.GetFullPath(directory).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
                return fullFile.StartsWith(fullDir, StringComparison.OrdinalIgnoreCase);
            }
            catch
            {
                return false;
            }
        }

        private static Artifact SelectArtifactToCommit(PipelineExecutionResult result, IList<Artifact> producedArtifacts, string originalSourcePath, string stagedPath)
        {
            if (WasChosenFromInput(result) && !string.IsNullOrWhiteSpace(originalSourcePath) && File.Exists(originalSourcePath))
            {
                var mediaType = MediaTypeGuesser.GuessFromFilePath(originalSourcePath);
                return Artifact.FromFilePath(originalSourcePath, mediaType);
            }

            if (result != null && result.FinalArtifact != null && !string.IsNullOrWhiteSpace(result.FinalArtifact.Locator) && File.Exists(result.FinalArtifact.Locator))
            {
                return result.FinalArtifact;
            }

            if (producedArtifacts != null)
            {
                for (var i = producedArtifacts.Count - 1; i >= 0; i--)
                {
                    var artifact = producedArtifacts[i];
                    if (artifact != null && !string.IsNullOrWhiteSpace(artifact.Locator) && File.Exists(artifact.Locator))
                    {
                        return artifact;
                    }
                }
            }

            if (!string.IsNullOrWhiteSpace(stagedPath) && File.Exists(stagedPath))
            {
                var mediaType = MediaTypeGuesser.GuessFromFilePath(stagedPath);
                return Artifact.FromFilePath(stagedPath, mediaType);
            }

            return null;
        }

        private static bool WasChosenFromInput(PipelineExecutionResult result)
        {
            if (result == null || result.StepMetrics == null || result.StepMetrics.Count == 0)
            {
                return false;
            }

            object raw;
            if (result.StepMetrics.TryGetValue("chosenFrom", out raw) && string.Equals(raw == null ? null : raw.ToString(), "input", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            foreach (var kv in result.StepMetrics)
            {
                if (kv.Key != null && kv.Key.EndsWith(".chosenFrom", StringComparison.OrdinalIgnoreCase) && string.Equals(kv.Value == null ? null : kv.Value.ToString(), "input", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        private static int TryGetTryCount(PipelineExecutionResult result)
        {
            if (result == null || result.StepMetrics == null || result.StepMetrics.Count == 0)
            {
                return 0;
            }

            var best = 0;
            foreach (var kv in result.StepMetrics)
            {
                if (!string.Equals(kv.Key, "tries", StringComparison.OrdinalIgnoreCase) &&
                    (kv.Key == null || !kv.Key.EndsWith(".tries", StringComparison.OrdinalIgnoreCase)))
                {
                    continue;
                }

                int parsed;
                if (TryConvertToInt32(kv.Value, out parsed) && parsed > best)
                {
                    best = parsed;
                }
            }

            return best;
        }

        private static string TryGetWinningParameterKey(PipelineExecutionResult result)
        {
            return TryGetStepMetricString(result, "chosenParameterKey");
        }

        private static string TryGetWinningParameterValue(PipelineExecutionResult result)
        {
            return TryGetStepMetricString(result, "chosenParameter");
        }

        private static string TryGetStepMetricString(PipelineExecutionResult result, string metricName)
        {
            if (result == null || result.StepMetrics == null || result.StepMetrics.Count == 0 || string.IsNullOrWhiteSpace(metricName))
            {
                return null;
            }

            object raw;
            if (result.StepMetrics.TryGetValue(metricName, out raw) && raw != null)
            {
                var directValue = raw.ToString();
                if (!string.IsNullOrWhiteSpace(directValue))
                {
                    return directValue;
                }
            }

            foreach (var kv in result.StepMetrics)
            {
                if (kv.Key == null || !kv.Key.EndsWith("." + metricName, StringComparison.OrdinalIgnoreCase) || kv.Value == null)
                {
                    continue;
                }

                var value = kv.Value.ToString();
                if (!string.IsNullOrWhiteSpace(value))
                {
                    return value;
                }
            }

            return null;
        }

        private static int GetWarningCount(PipelineExecutionResult result)
        {
            if (result == null || result.StepMetrics == null || result.StepMetrics.Count == 0)
            {
                return 0;
            }

            var count = 0;
            var sawStepSpecificKey = false;

            foreach (var kv in result.StepMetrics)
            {
                if (kv.Key == null || !kv.Key.EndsWith(".nonFatalWarning", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                sawStepSpecificKey = true;
                bool value;
                if (TryConvertToBoolean(kv.Value, out value) && value)
                {
                    count++;
                }
            }

            if (sawStepSpecificKey)
            {
                return count;
            }

            object raw;
            if (result.StepMetrics.TryGetValue("nonFatalWarning", out raw))
            {
                bool value;
                if (TryConvertToBoolean(raw, out value) && value)
                {
                    return 1;
                }
            }

            return 0;
        }

        private static bool TryConvertToInt32(object value, out int parsed)
        {
            if (value is int)
            {
                parsed = (int)value;
                return true;
            }

            if (value is long)
            {
                var longValue = (long)value;
                if (longValue >= int.MinValue && longValue <= int.MaxValue)
                {
                    parsed = (int)longValue;
                    return true;
                }
            }

            if (value is short)
            {
                parsed = (short)value;
                return true;
            }

            if (value is byte)
            {
                parsed = (byte)value;
                return true;
            }

            return int.TryParse(value == null ? null : value.ToString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out parsed);
        }

        private static bool TryConvertToBoolean(object value, out bool parsed)
        {
            if (value is bool)
            {
                parsed = (bool)value;
                return true;
            }

            return bool.TryParse(value == null ? null : value.ToString(), out parsed);
        }

        private static string TryGetNonFatalWarningText(PipelineExecutionResult result)
        {
            if (result == null || result.StepMetrics == null || result.StepMetrics.Count == 0)
            {
                return null;
            }

            object raw;
            if (!result.StepMetrics.TryGetValue("nonFatalWarning", out raw) || !(raw is bool) || !((bool)raw))
            {
                return null;
            }

            if (result.StepMetrics.TryGetValue("nonFatalWarningText", out raw) && raw != null)
            {
                var text = raw.ToString();
                if (!string.IsNullOrWhiteSpace(text))
                {
                    return text;
                }
            }

            return "Completed with warning.";
        }

        private static string FormatBytes(long bytes)
        {
            if (bytes < 1024) return bytes.ToString(CultureInfo.InvariantCulture) + " B";
            if (bytes < 1024 * 1024) return (bytes / 1024.0).ToString("0.0", CultureInfo.InvariantCulture) + " KB";
            if (bytes < 1024L * 1024L * 1024L) return (bytes / (1024.0 * 1024.0)).ToString("0.0", CultureInfo.InvariantCulture) + " MB";
            return (bytes / (1024.0 * 1024.0 * 1024.0)).ToString("0.00", CultureInfo.InvariantCulture) + " GB";
        }

        private void Ui(Action action)
        {
            try
            {
                _dispatcher.BeginInvoke(action);
            }
            catch
            {
            }
        }

        private sealed class RunnerFileState
        {
            public RunnerFileState(string filePath, long originalSizeBytes)
            {
                FilePath = filePath;
                OriginalSizeBytes = originalSizeBytes;
                StatusText = "Queued";
            }

            public string FilePath { get; }
            public long OriginalSizeBytes { get; }
            public long? NewSizeBytes { get; set; }
            public string NewFilePath { get; set; }
            public int ProducedOutputCount { get; set; }
            public string StatusText { get; private set; }

            public void SetRunning() { StatusText = "Running..."; }
            public void SetDone() { StatusText = "Done"; }
            public void SetWarning(string reason)
            {
                StatusText = string.IsNullOrWhiteSpace(reason) ? "Warning" : "Warning: " + reason;
            }
            public void SetDoneNoOutput() { StatusText = "Done (no output)"; }

            public void SetFailed(string reason)
            {
                StatusText = string.IsNullOrWhiteSpace(reason) ? "Failed" : "Failed: " + reason;
            }
        }
    }
}
