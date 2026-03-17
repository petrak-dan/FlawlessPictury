using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using FlawlessPictury.Presentation.WinForms.Win32;

namespace FlawlessPictury.Presentation.WinForms.Views
{
    /// <summary>
    /// Designer-friendly RunnerForm implementation (no runtime recreation of menu/toolbars).
    ///
    /// Patch:
    /// - Fixes layout via root TableLayoutPanel (menu, top controls, main split, status).
    /// - Empty-state overlay is a semi-opaque panel shown above the list (list stays present).
    /// - Properties ListView columns: first autosizes to content; second fills remaining width (no horizontal scrollbar).
    /// </summary>
    public sealed partial class RunnerForm : Form, IRunnerView
    {
        private readonly List<string> _droppedPaths;
        private readonly ToolTip _presetToolTip;
        private List<RunnerInputListItem> _virtualItems;

        private bool _canRun;
        private bool _isBusy;
        private bool _suppressPresetSelectionChanged;
        private bool _suppressOutputFolderTextChanged;
        private Panel _previewStatusOverlay;
        private Label _previewStatusLabel;
        private ContextMenuStrip _inputContextMenu;
        private ToolStripMenuItem _miShowOriginalFile;
        private ToolStripMenuItem _miShowNewFile;
        private Image _cachedOriginalPreviewImage;
        private Image _cachedNewPreviewImage;
        private IReadOnlyDictionary<string, string> _cachedOriginalProperties;
        private IReadOnlyDictionary<string, string> _cachedNewProperties;
        private string _cachedOriginalPreviewStatusText;
        private string _cachedNewPreviewStatusText;

        private const int OutputBrowseButtonWidth = 34;
        private const int EM_SETMARGINS = 0x00D3;
        private const int EC_RIGHTMARGIN = 0x0002;

        public RunnerForm()
        {
            _droppedPaths = new List<string>();
            _presetToolTip = new ToolTip();
            _virtualItems = new List<RunnerInputListItem>();
            _canRun = false;
            _isBusy = false;

            InitializeComponent();
            lblEmptyState.Text = "Drop files here, or use Add Files...\r\n\r\n1) Add Files\r\n2) Choose Preset\r\n3) Click Convert";
            KeyPreview = true;
            InitializeSharedPreviewPane();
            InitializeEmbeddedOutputBrowseButton();
            InitializeInputContextMenu();
            InitializeEmptyStateActivation();
            NormalizeActionVisuals();

            // Drag/drop sources
            panelListHost.AllowDrop = true;
            listInputs.AllowDrop = true;

            panelListHost.DragEnter += OnDragEnter;
            panelListHost.DragDrop += OnDragDrop;
            listInputs.DragEnter += OnDragEnter;
            listInputs.DragDrop += OnDragDrop;

            // Virtual list support
            listInputs.VirtualMode = true;
            listInputs.RetrieveVirtualItem += OnRetrieveVirtualItem;
            listInputs.SelectedIndexChanged += (s, e) => SelectedInputChanged?.Invoke(this, EventArgs.Empty);
            listInputs.MouseDown += OnListInputsMouseDown;

            _presetToolTip.ShowAlways = true;
            cmbPresets.MouseHover += (s, e) => UpdatePresetToolTip();

            // Menu -> events
            miAddFiles.Click += (s, e) => AddFilesRequested?.Invoke(this, EventArgs.Empty);
            miAddFolder.Click += (s, e) => AddFolderRequested?.Invoke(this, EventArgs.Empty);
            miRemoveSelected.Click += (s, e) => RemoveSelectedRequested?.Invoke(this, EventArgs.Empty);
            miClearList.Click += (s, e) => ClearRequested?.Invoke(this, EventArgs.Empty);
            miExit.Click += (s, e) => Close();

            miLog.Click += (s, e) => OpenLogWindowRequested?.Invoke(this, EventArgs.Empty);
            miPreviewPane.CheckedChanged += (s, e) => ApplyPreviewPaneVisible(miPreviewPane.Checked);

            miReload.Click += (s, e) => ReloadRequested?.Invoke(this, EventArgs.Empty);
            miPluginExplorer.Click += (s, e) => OpenPluginExplorerRequested?.Invoke(this, EventArgs.Empty);

            miAbout.Click += (s, e) => ShowAbout();

            // Toolbar -> events
            tsAddFiles.Click += (s, e) => AddFilesRequested?.Invoke(this, EventArgs.Empty);
            tsAddFolder.Click += (s, e) => AddFolderRequested?.Invoke(this, EventArgs.Empty);
            tsRemoveSelected.Click += (s, e) => RemoveSelectedRequested?.Invoke(this, EventArgs.Empty);
            tsClearList.Click += (s, e) => ClearRequested?.Invoke(this, EventArgs.Empty);

            // Other controls
            cmbPresets.SelectedIndexChanged += (s, e) =>
            {
                UpdatePresetToolTip();

                if (!_suppressPresetSelectionChanged)
                {
                    PresetSelectionChanged?.Invoke(this, EventArgs.Empty);
                }
            };

            txtOutput.ReadOnly = false;
            txtOutput.TextChanged += (s, e) =>
            {
                if (!_suppressOutputFolderTextChanged)
                {
                    OutputFolderTextChanged?.Invoke(this, EventArgs.Empty);
                }
            };

            btnBrowseOutput.Click += (s, e) => BrowseOutputRequested?.Invoke(this, EventArgs.Empty);

            btnRun.Click += (s, e) => RunRequested?.Invoke(this, EventArgs.Empty);

            // Initial states
            ApplyPreviewPaneVisible(miPreviewPane.Checked);
            UpdateEmptyState();
            ApplyRunEnabledState(_isBusy);

            Shown += (s, e) =>
            {
                ViewReady?.Invoke(this, EventArgs.Empty);
            };
        }

        public event EventHandler ViewReady;
        public event EventHandler AddFilesRequested;
        public event EventHandler AddFolderRequested;
        public event EventHandler RemoveSelectedRequested;
        public event EventHandler ClearRequested;
        public event EventHandler FilesDropped;
        public event EventHandler PresetSelectionChanged;
        public event EventHandler BrowseOutputRequested;
        public event EventHandler OutputFolderTextChanged;
        public event EventHandler RunRequested;
        public event EventHandler ReloadRequested;
        public event EventHandler OpenPluginExplorerRequested;
        public event EventHandler OpenLogWindowRequested;
        public event EventHandler SelectedInputChanged;
        public event EventHandler PreviewPaneSelectionChanged;

        public IReadOnlyList<string> DroppedPaths => _droppedPaths;

        public string SelectedPresetId
        {
            get
            {
                return UiGet(() =>
                {
                    var item = cmbPresets.SelectedItem as PresetListItem;
                    return item == null ? null : item.PresetId;
                });
            }
        }

        public void SetPresets(IReadOnlyList<PresetListItem> presets)
        {
            Ui(() =>
            {
                var previousId = (cmbPresets.SelectedItem as PresetListItem)?.PresetId;

                _suppressPresetSelectionChanged = true;
                cmbPresets.BeginUpdate();
                try
                {
                    cmbPresets.SelectedIndex = -1;
                    cmbPresets.Items.Clear();
                    if (presets != null)
                    {
                        foreach (var p in presets)
                        {
                            cmbPresets.Items.Add(p);
                        }
                    }

                    var targetIndex = FindPresetIndexById(previousId);
                    if (targetIndex < 0 && cmbPresets.Items.Count > 0)
                    {
                        targetIndex = 0;
                    }

                    ForcePresetSelection(targetIndex);
                }
                finally
                {
                    cmbPresets.EndUpdate();
                    _suppressPresetSelectionChanged = false;
                }

                cmbPresets.Refresh();
                UpdatePresetToolTip();
                ApplyRunEnabledState(_isBusy);
            });
        }

        public void SetCanRun(bool canRun)
        {
            Ui(() =>
            {
                _canRun = canRun;
                ApplyRunEnabledState(_isBusy);
            });
        }

        public void SetSelectedPresetId(string presetId)
        {
            Ui(() =>
            {
                _suppressPresetSelectionChanged = true;
                try
                {
                    ForcePresetSelection(FindPresetIndexById(presetId));
                }
                finally
                {
                    _suppressPresetSelectionChanged = false;
                }

                cmbPresets.Refresh();
                UpdatePresetToolTip();
            });
        }

        private int FindPresetIndexById(string presetId)
        {
            if (string.IsNullOrWhiteSpace(presetId))
            {
                return -1;
            }

            for (var i = 0; i < cmbPresets.Items.Count; i++)
            {
                var item = cmbPresets.Items[i] as PresetListItem;
                if (item != null && string.Equals(item.PresetId, presetId, StringComparison.OrdinalIgnoreCase))
                {
                    return i;
                }
            }

            return -1;
        }

        private void ForcePresetSelection(int targetIndex)
        {
            if (targetIndex < -1 || targetIndex >= cmbPresets.Items.Count)
            {
                targetIndex = -1;
            }

            if (cmbPresets.SelectedIndex == targetIndex)
            {
                cmbPresets.SelectedIndex = -1;
            }

            cmbPresets.SelectedIndex = targetIndex;
            if (targetIndex >= 0 && targetIndex < cmbPresets.Items.Count)
            {
                cmbPresets.Text = cmbPresets.Items[targetIndex].ToString();
            }
            else
            {
                cmbPresets.Text = string.Empty;
            }
        }

        private void UpdatePresetToolTip()
        {
            var item = cmbPresets.SelectedItem as PresetListItem;
            var text = item == null || string.IsNullOrWhiteSpace(item.Description)
                ? string.Empty
                : item.Description;

            _presetToolTip.SetToolTip(cmbPresets, text);
        }

        public bool UseAutoOutputFolder => false;

        public string OutputFolderText => UiGet(() => txtOutput.Text);

        public void SetOutputFolderText(string text) => Ui(() =>
        {
            _suppressOutputFolderTextChanged = true;
            try
            {
                txtOutput.Text = text ?? string.Empty;
            }
            finally
            {
                _suppressOutputFolderTextChanged = false;
            }
        });

        public void SetOutputChooserEnabled(bool enabled) => Ui(() => btnBrowseOutput.Enabled = enabled && !_isBusy);

        public void SetInputItems(IReadOnlyList<RunnerInputListItem> items)
        {
            Ui(() =>
            {
                _virtualItems = items == null ? new List<RunnerInputListItem>() : new List<RunnerInputListItem>(items);
                listInputs.VirtualListSize = _virtualItems.Count;
                listInputs.Invalidate();
                UpdateEmptyState();
            });
        }

        public IReadOnlyList<string> GetSelectedInputPaths()
        {
            return ControlGet(listInputs, () =>
            {
                var selected = new List<string>();

                foreach (int idx in listInputs.SelectedIndices)
                {
                    if (idx >= 0 && idx < _virtualItems.Count)
                    {
                        var p = _virtualItems[idx].FilePath;
                        if (!string.IsNullOrWhiteSpace(p))
                        {
                            selected.Add(p);
                        }
                    }
                }

                return (IReadOnlyList<string>)selected;
            });
        }

        public string SelectedSingleInputPath
        {
            get
            {
                return ControlGet(listInputs, () =>
                {
                    if (listInputs.SelectedIndices.Count != 1) return null;

                    var idx = listInputs.SelectedIndices[0];
                    if (idx < 0 || idx >= _virtualItems.Count) return null;

                    return _virtualItems[idx].FilePath;
                });
            }
        }

        public bool IsNewPreviewSelected
        {
            get
            {
                return tabPreviewSource != null && tabPreviewSource.SelectedIndex > 0;
            }
        }

        public void SetStatus(string text) => Ui(() => statusLabel.Text = string.IsNullOrWhiteSpace(text) ? "Ready." : text);

        public void SetProgress(int percent)
        {
            Ui(() =>
            {
                var v = Math.Max(0, Math.Min(100, percent));
                progressBar.Value = v;
            });
        }

        public void SetBusy(bool isBusy) => Ui(() => ApplyRunEnabledState(isBusy));

        private void ApplyRunEnabledState(bool isBusy)
        {
            btnRun.Enabled = (!isBusy) && _canRun;

            cmbPresets.Enabled = !isBusy && (cmbPresets.Items.Count > 0);
            txtOutput.Enabled = !isBusy;
            btnBrowseOutput.Enabled = !isBusy;

            miAddFiles.Enabled = !isBusy;
            miAddFolder.Enabled = !isBusy;
            miRemoveSelected.Enabled = !isBusy;
            miClearList.Enabled = !isBusy;

            miReload.Enabled = !isBusy;
            miPluginExplorer.Enabled = !isBusy;

            tsAddFiles.Enabled = !isBusy;
            tsAddFolder.Enabled = !isBusy;
            tsRemoveSelected.Enabled = !isBusy;
            tsClearList.Enabled = !isBusy;
        }
        public void DisplayLogLine(string line)
        {
            Ui(() => statusLabel.Text = string.IsNullOrWhiteSpace(line) ? "Ready." : line);
        }

        public void ShowCriticalError(string message)
        {
            Ui(() =>
            {
                MessageBox.Show(this, message ?? "Unknown error.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            });
        }

        public void SetPreviewImage(Image image)
        {
            if (IsNewPreviewSelected)
            {
                SetNewPreviewImage(image);
                return;
            }

            SetOriginalPreviewImage(image);
        }

        public void SetProperties(IReadOnlyDictionary<string, string> props)
        {
            if (IsNewPreviewSelected)
            {
                SetNewProperties(props);
                return;
            }

            SetOriginalProperties(props);
        }

        public void SetOriginalPreviewImage(Image image)
        {
            Ui(() =>
            {
                ReplaceCachedImage(ref _cachedOriginalPreviewImage, image);
                if (!IsNewPreviewSelected)
                {
                    ApplySelectedPreviewPane();
                }
            });
        }

        public void SetOriginalPreviewStatus(string text)
        {
            Ui(() =>
            {
                _cachedOriginalPreviewStatusText = text ?? string.Empty;
                if (!IsNewPreviewSelected)
                {
                    ApplySelectedPreviewPane();
                }
            });
        }

        public void SetOriginalProperties(IReadOnlyDictionary<string, string> props)
        {
            Ui(() =>
            {
                _cachedOriginalProperties = CloneProperties(props);
                if (!IsNewPreviewSelected)
                {
                    ApplySelectedPreviewPane();
                }
            });
        }

        public void SetNewPreviewImage(Image image)
        {
            Ui(() =>
            {
                ReplaceCachedImage(ref _cachedNewPreviewImage, image);
                if (IsNewPreviewSelected)
                {
                    ApplySelectedPreviewPane();
                }
            });
        }

        public void SetNewPreviewStatus(string text)
        {
            Ui(() =>
            {
                _cachedNewPreviewStatusText = text ?? string.Empty;
                if (IsNewPreviewSelected)
                {
                    ApplySelectedPreviewPane();
                }
            });
        }

        public void SetNewProperties(IReadOnlyDictionary<string, string> props)
        {
            Ui(() =>
            {
                _cachedNewProperties = CloneProperties(props);
                if (IsNewPreviewSelected)
                {
                    ApplySelectedPreviewPane();
                }
            });
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (ShouldLetFocusedControlHandleKey(keyData))
            {
                return false;
            }

            // Non-standard single-letter shortcuts:
            if (keyData == Keys.L)
            {
                OpenLogWindowRequested?.Invoke(this, EventArgs.Empty);
                return true;
            }

            if (keyData == Keys.P)
            {
                miPreviewPane.Checked = !miPreviewPane.Checked;
                return true;
            }

            return base.ProcessCmdKey(ref msg, keyData);
        }

        private bool ShouldLetFocusedControlHandleKey(Keys keyData)
        {
            if ((keyData & Keys.Modifiers) != Keys.None)
            {
                return false;
            }

            var activeControl = GetDeepActiveControl(ActiveControl);
            if (activeControl == null)
            {
                return false;
            }

            if (activeControl is TextBoxBase)
            {
                return true;
            }

            var comboBox = activeControl as ComboBox;
            if (comboBox != null)
            {
                return comboBox.DroppedDown || comboBox.DropDownStyle != ComboBoxStyle.DropDownList;
            }

            return false;
        }

        private static Control GetDeepActiveControl(Control control)
        {
            var current = control;
            while (current is ContainerControl)
            {
                var container = current as ContainerControl;
                if (container == null || container.ActiveControl == null)
                {
                    break;
                }

                current = container.ActiveControl;
            }

            return current;
        }

        private void ApplyPreviewPaneVisible(bool visible)
        {
            try
            {
                splitMain.Panel2Collapsed = !visible;
                if (!splitMain.Panel2Collapsed)
                {
                    splitRight.Panel1Collapsed = false;
                    splitRight.Panel2Collapsed = false;
                    ApplySelectedPreviewPane();
                }
            }
            catch { }
        }

        private void InitializeSharedPreviewPane()
        {
            try
            {
                if (tabPreviewSource != null)
                {
                    tabPreviewSource.SelectedIndexChanged += (s, e) =>
                    {
                        ApplySelectedPreviewPane();
                        PreviewPaneSelectionChanged?.Invoke(this, EventArgs.Empty);
                    };
                }

                InitializePreviewStatusOverlay(picturePreview, out _previewStatusOverlay, out _previewStatusLabel);
                _cachedOriginalProperties = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                _cachedNewProperties = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                _cachedOriginalPreviewStatusText = string.Empty;
                _cachedNewPreviewStatusText = string.Empty;
                ApplySelectedPreviewPane();
            }
            catch
            {
            }
        }

        private static void InitializePreviewStatusOverlay(PictureBox targetPictureBox, out Panel overlay, out Label label)
        {
            overlay = null;
            label = null;

            try
            {
                if (targetPictureBox == null || targetPictureBox.Parent == null)
                {
                    return;
                }

                label = new Label
                {
                    Dock = DockStyle.Fill,
                    TextAlign = ContentAlignment.MiddleCenter,
                    Text = string.Empty,
                    BackColor = SystemColors.Control,
                    ForeColor = SystemColors.ControlText,
                    AutoEllipsis = true
                };

                overlay = new Panel
                {
                    Dock = DockStyle.Fill,
                    Visible = false,
                    BackColor = SystemColors.Control
                };

                overlay.Controls.Add(label);
                targetPictureBox.Parent.Controls.Add(overlay);
                overlay.BringToFront();
            }
            catch
            {
                overlay = null;
                label = null;
            }
        }

        private void ApplySelectedPreviewPane()
        {
            var selectedImage = IsNewPreviewSelected ? _cachedNewPreviewImage : _cachedOriginalPreviewImage;
            var selectedStatus = IsNewPreviewSelected ? _cachedNewPreviewStatusText : _cachedOriginalPreviewStatusText;
            var selectedProperties = IsNewPreviewSelected ? _cachedNewProperties : _cachedOriginalProperties;

            SetPictureBoxImage(picturePreview, CloneImageSafely(selectedImage));
            SetPreviewOverlayState(_previewStatusOverlay, _previewStatusLabel, selectedStatus);
            PopulatePropertiesList(listProperties, selectedProperties);
        }

        private static Image CloneImageSafely(Image image)
        {
            try
            {
                return image == null ? null : (Image)image.Clone();
            }
            catch
            {
                return null;
            }
        }

        private static IReadOnlyDictionary<string, string> CloneProperties(IReadOnlyDictionary<string, string> props)
        {
            var clone = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (props != null)
            {
                foreach (var pair in props)
                {
                    clone[pair.Key] = pair.Value ?? string.Empty;
                }
            }

            return clone;
        }

        private static void ReplaceCachedImage(ref Image target, Image replacement)
        {
            var old = target;
            target = replacement;
            if (old != null && !ReferenceEquals(old, replacement))
            {
                old.Dispose();
            }
        }

        private void InitializeEmbeddedOutputBrowseButton()
        {
            try
            {
                if (txtOutput == null || btnBrowseOutput == null)
                {
                    return;
                }

                btnBrowseOutput.FlatStyle = FlatStyle.Standard;
                btnBrowseOutput.TabStop = false;
                btnBrowseOutput.Cursor = Cursors.Default;

                txtOutput.Controls.Add(btnBrowseOutput);
                txtOutput.Resize += (s, e) => LayoutEmbeddedOutputBrowseButton();
                txtOutput.HandleCreated += (s, e) => LayoutEmbeddedOutputBrowseButton();
                txtOutput.FontChanged += (s, e) => LayoutEmbeddedOutputBrowseButton();

                LayoutEmbeddedOutputBrowseButton();
            }
            catch
            {
            }
        }

        private void LayoutEmbeddedOutputBrowseButton()
        {
            try
            {
                if (txtOutput == null || btnBrowseOutput == null)
                {
                    return;
                }

                var width = OutputBrowseButtonWidth;
                var height = txtOutput.ClientSize.Height + 2;

                btnBrowseOutput.Size = new Size(width, height);
                btnBrowseOutput.Location = new Point(Math.Max(0, txtOutput.ClientSize.Width - width), -1);
                btnBrowseOutput.BringToFront();
                RefreshEmbeddedBrowseButtonImage();

                if (txtOutput.IsHandleCreated)
                {
                    SendMessage(txtOutput.Handle, EM_SETMARGINS, (IntPtr)EC_RIGHTMARGIN, (IntPtr)((width + 2) << 16));
                }
            }
            catch
            {
            }
        }

        private void NormalizeActionVisuals()
        {
            try
            {
                var iconSize = GetUiIconSize();

                if (mainMenu != null)
                {
                    mainMenu.ImageScalingSize = new Size(iconSize, iconSize);
                }

                if (listToolStrip != null)
                {
                    listToolStrip.ImageScalingSize = new Size(iconSize, iconSize);
                }

                btnBrowseOutput.Image = null;
                btnBrowseOutput.Text = string.Empty;
                btnBrowseOutput.TextAlign = ContentAlignment.MiddleCenter;
                btnBrowseOutput.ImageAlign = ContentAlignment.MiddleCenter;
                btnBrowseOutput.TextImageRelation = TextImageRelation.Overlay;
                btnBrowseOutput.Padding = Padding.Empty;
                btnBrowseOutput.UseCompatibleTextRendering = false;

                btnRun.TextImageRelation = TextImageRelation.ImageBeforeText;
                btnRun.ImageAlign = ContentAlignment.MiddleLeft;
                btnRun.TextAlign = ContentAlignment.MiddleCenter;
                btnRun.Padding = new Padding(6, 0, 6, 0);
                btnRun.Image = CreatePlayGlyphBitmap(iconSize);

                using (var stockIcons = new WindowsStockIconProvider())
                {
                    tsAddFiles.Image = CreateStockBitmap(stockIcons, WindowsStockIconProvider.WindowsStockIconId.ImageFiles, iconSize);
                    tsAddFolder.Image = CreateStockBitmap(stockIcons, WindowsStockIconProvider.WindowsStockIconId.FolderOpen, iconSize);
                    tsRemoveSelected.Image = null;
                    tsClearList.Image = null;

                    miAddFiles.Image = CreateStockBitmap(stockIcons, WindowsStockIconProvider.WindowsStockIconId.ImageFiles, iconSize);
                    miAddFolder.Image = CreateStockBitmap(stockIcons, WindowsStockIconProvider.WindowsStockIconId.FolderOpen, iconSize);
                    miRemoveSelected.Image = null;
                    miClearList.Image = null;
                    miLog.Image = CreateStockBitmap(stockIcons, WindowsStockIconProvider.WindowsStockIconId.Info, iconSize);
                    miPreviewPane.Image = null;
                    miReload.Image = null;
                    miPluginExplorer.Image = CreateStockBitmap(stockIcons, WindowsStockIconProvider.WindowsStockIconId.Find, iconSize);
                    miAbout.Image = CreateStockBitmap(stockIcons, WindowsStockIconProvider.WindowsStockIconId.Info, iconSize);

                    // Embedded browse button image is refreshed separately after layout so it can stay centered and un-stretched.
                }
            }
            catch
            {
            }
        }

        private void RefreshEmbeddedBrowseButtonImage()
        {
            try
            {
                if (btnBrowseOutput == null || btnBrowseOutput.IsDisposed || btnBrowseOutput.ClientSize.Width <= 0 || btnBrowseOutput.ClientSize.Height <= 0)
                {
                    return;
                }

                using (var stockIcons = new WindowsStockIconProvider())
                {
                    var bitmap = CreateEmbeddedBrowseButtonBitmap(stockIcons, WindowsStockIconProvider.WindowsStockIconId.FolderOpen, btnBrowseOutput.ClientSize);
                    var previousImage = btnBrowseOutput.Image;
                    btnBrowseOutput.Image = bitmap;
                    if (previousImage != null && !ReferenceEquals(previousImage, bitmap))
                    {
                        previousImage.Dispose();
                    }
                }
            }
            catch
            {
            }
        }

        private int GetUiIconSize()
        {
            try
            {
                var dpi = DeviceDpi > 0 ? DeviceDpi : 96;
                return Math.Max(16, (int)Math.Round(16f * dpi / 96f));
            }
            catch
            {
                return 16;
            }
        }

        private static Bitmap CreateNativeStockBitmap(WindowsStockIconProvider provider, WindowsStockIconProvider.WindowsStockIconId id)
        {
            if (provider == null)
            {
                return null;
            }

            var icon = provider.GetSmall(id);
            if (icon == null)
            {
                return null;
            }

            return icon.ToBitmap();
        }

        private static Bitmap CreateEmbeddedBrowseButtonBitmap(WindowsStockIconProvider provider, WindowsStockIconProvider.WindowsStockIconId id, Size buttonSize)
        {
            if (provider == null)
            {
                return null;
            }

            var icon = provider.GetSmall(id);
            if (icon == null)
            {
                return null;
            }

            var available = Math.Max(8, Math.Min(buttonSize.Width - 8, buttonSize.Height - 6));
            var iconSize = Math.Min(16, available);
            if (iconSize < 8)
            {
                iconSize = 8;
            }

            var canvasWidth = Math.Max(buttonSize.Width, iconSize + 6);
            var canvasHeight = Math.Max(buttonSize.Height, iconSize + 4);
            var bitmap = new Bitmap(canvasWidth, canvasHeight);

            using (var graphics = Graphics.FromImage(bitmap))
            {
                graphics.Clear(Color.Transparent);
                graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;

                var x = Math.Max(0, (canvasWidth - iconSize) / 2);
                var y = Math.Max(0, (canvasHeight - iconSize) / 2);
                graphics.DrawIcon(icon, new Rectangle(x, y, iconSize, iconSize));
            }

            return bitmap;
        }

        private static Bitmap CreateStockBitmap(WindowsStockIconProvider provider, WindowsStockIconProvider.WindowsStockIconId id, int size)
        {
            if (provider == null)
            {
                return null;
            }

            var icon = provider.GetSmall(id);
            if (icon == null)
            {
                return null;
            }

            var bitmap = new Bitmap(size, size);
            using (var graphics = Graphics.FromImage(bitmap))
            {
                graphics.Clear(Color.Transparent);
                graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
                graphics.DrawIcon(icon, new Rectangle(0, 0, size, size));
            }

            return bitmap;
        }

        private static Bitmap CreatePlayGlyphBitmap(int size)
        {
            var bitmap = new Bitmap(size, size);
            using (var graphics = Graphics.FromImage(bitmap))
            {
                graphics.Clear(Color.Transparent);
                graphics.SmoothingMode = SmoothingMode.AntiAlias;
                graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;

                var left = size * 0.28f;
                var top = size * 0.18f;
                var bottom = size * 0.82f;
                var right = size * 0.78f;

                PointF[] triangle =
                {
                    new PointF(left, top),
                    new PointF(left, bottom),
                    new PointF(right, size * 0.50f)
                };

                using (var brush = new SolidBrush(Color.ForestGreen))
                {
                    graphics.FillPolygon(brush, triangle);
                }
            }

            return bitmap;
        }

        private void EnsureInputColumns()
        {
        }

        private void InitializeInputContextMenu()
        {
            try
            {
                _inputContextMenu = new ContextMenuStrip();
                _miShowOriginalFile = new ToolStripMenuItem("Show File in Explorer...");
                _miShowNewFile = new ToolStripMenuItem("Show New File in Explorer...");
                _miShowOriginalFile.Click += (s, e) => OpenSelectedOriginalFileInExplorer();
                _miShowNewFile.Click += (s, e) => OpenSelectedNewFileInExplorer();
                _inputContextMenu.Opening += OnInputContextMenuOpening;
                _inputContextMenu.Items.AddRange(new ToolStripItem[] { _miShowOriginalFile, _miShowNewFile });
                listInputs.ContextMenuStrip = _inputContextMenu;
            }
            catch
            {
            }
        }

        private void InitializeEmptyStateActivation()
        {
            panelEmptyState.DoubleClick += (s, e) => AddFilesRequested?.Invoke(this, EventArgs.Empty);
            lblEmptyState.DoubleClick += (s, e) => AddFilesRequested?.Invoke(this, EventArgs.Empty);
        }

        private void OnListInputsMouseDown(object sender, MouseEventArgs e)
        {
            try
            {
                if (e == null || e.Button != MouseButtons.Right)
                {
                    return;
                }

                var hit = listInputs.HitTest(e.Location);
                if (hit == null || hit.Item == null)
                {
                    return;
                }

                if (!hit.Item.Selected)
                {
                    listInputs.SelectedIndices.Clear();
                    hit.Item.Selected = true;
                }
            }
            catch
            {
            }
        }

        private void OnInputContextMenuOpening(object sender, CancelEventArgs e)
        {
            var item = GetSelectedRunnerInputListItem();
            if (item == null)
            {
                e.Cancel = true;
                return;
            }

            var hasNewFile = !string.IsNullOrWhiteSpace(item.NewFilePath);
            _miShowOriginalFile.Text = hasNewFile ? "Show Original File in Explorer..." : "Show File in Explorer...";
            _miShowOriginalFile.Enabled = !string.IsNullOrWhiteSpace(item.FilePath);
            _miShowNewFile.Enabled = hasNewFile;
            _miShowNewFile.Visible = hasNewFile;
        }

        private RunnerInputListItem GetSelectedRunnerInputListItem()
        {
            try
            {
                if (listInputs.SelectedIndices.Count != 1)
                {
                    return null;
                }

                var index = listInputs.SelectedIndices[0];
                if (index < 0 || index >= _virtualItems.Count)
                {
                    return null;
                }

                return _virtualItems[index];
            }
            catch
            {
                return null;
            }
        }

        private void OpenSelectedOriginalFileInExplorer()
        {
            var item = GetSelectedRunnerInputListItem();
            if (item != null)
            {
                ShowInExplorer(item.FilePath);
            }
        }

        private void OpenSelectedNewFileInExplorer()
        {
            var item = GetSelectedRunnerInputListItem();
            if (item != null)
            {
                ShowInExplorer(item.NewFilePath);
            }
        }

        private static void ShowInExplorer(string path)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(path))
                {
                    return;
                }

                if (File.Exists(path))
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = "explorer.exe",
                        Arguments = "/select,\"" + path + "\"",
                        UseShellExecute = true
                    });
                    return;
                }

                var directory = Directory.Exists(path) ? path : Path.GetDirectoryName(path);
                if (!string.IsNullOrWhiteSpace(directory) && Directory.Exists(directory))
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = "explorer.exe",
                        Arguments = "\"" + directory + "\"",
                        UseShellExecute = true
                    });
                }
            }
            catch
            {
            }
        }

        private static void SetPictureBoxImage(PictureBox pictureBox, Image image)
        {
            if (pictureBox == null)
            {
                if (image != null)
                {
                    image.Dispose();
                }
                return;
            }

            var old = pictureBox.Image;
            if (ReferenceEquals(old, image))
            {
                return;
            }

            pictureBox.Image = image;
            if (old != null)
            {
                old.Dispose();
            }
        }

        private static void SetPreviewOverlayState(Panel overlay, Label label, string text)
        {
            var visible = !string.IsNullOrWhiteSpace(text);
            if (label != null)
            {
                label.Text = text ?? string.Empty;
            }

            if (overlay != null)
            {
                overlay.Visible = visible;
                if (visible)
                {
                    overlay.BringToFront();
                }
            }
        }

        private void PopulatePropertiesList(ListView listView, IReadOnlyDictionary<string, string> props)
        {
            if (listView == null)
            {
                return;
            }

            listView.BeginUpdate();
            try
            {
                listView.Items.Clear();
                if (props != null)
                {
                    foreach (var kvp in props)
                    {
                        listView.Items.Add(new ListViewItem(new[] { kvp.Key, kvp.Value }));
                    }
                }
            }
            finally
            {
                listView.EndUpdate();
            }

        }

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);

        private void ShowAbout()
        {
            MessageBox.Show(
                this,
                "Flawless Pictury\r\n\r\nA modular, preset-driven batch converter.",
                "About Flawless Pictury",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }

        private void UpdateEmptyState()
        {
            try
            {
                panelEmptyState.Visible = _virtualItems.Count == 0;

                if (panelEmptyState.Visible)
                {
                    // Ensure overlay is above list (but list remains in place underneath).
                    panelEmptyState.BringToFront();
                }
            }
            catch { }
        }

        private void OnRetrieveVirtualItem(object sender, RetrieveVirtualItemEventArgs e)
        {
            if (e.ItemIndex < 0 || e.ItemIndex >= _virtualItems.Count)
            {
                e.Item = new ListViewItem(new[] { string.Empty, string.Empty, string.Empty, string.Empty, string.Empty });
                return;
            }

            var it = _virtualItems[e.ItemIndex];
            e.Item = new ListViewItem(new[]
            {
                it.FilePath ?? string.Empty,
                it.StatusText ?? string.Empty,
                it.OriginalSizeText ?? string.Empty,
                it.NewSizeText ?? string.Empty,
                it.NewFileText ?? string.Empty
            });
        }

        private void OnDragEnter(object sender, DragEventArgs e)
        {
            if (e.Data != null && e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effect = DragDropEffects.Copy;
            }
        }

        private void OnDragDrop(object sender, DragEventArgs e)
        {
            _droppedPaths.Clear();

            if (e.Data == null || !e.Data.GetDataPresent(DataFormats.FileDrop)) return;

            var arr = e.Data.GetData(DataFormats.FileDrop) as string[];
            if (arr == null || arr.Length == 0) return;

            foreach (var p in arr)
            {
                if (!string.IsNullOrWhiteSpace(p))
                {
                    _droppedPaths.Add(p);
                }
            }

            if (_droppedPaths.Count > 0)
            {
                FilesDropped?.Invoke(this, EventArgs.Empty);
            }
        }

        private void Ui(Action action)
        {
            if (action == null) return;

            try
            {
                var target = GetMarshalTarget();
                if (target == null || target.IsDisposed) return;

                if (target.InvokeRequired)
                {
                    target.BeginInvoke((Action)(() =>
                    {
                        try { action(); } catch { }
                    }));
                    return;
                }

                action();
            }
            catch { }
        }

        private T UiGet<T>(Func<T> func)
        {
            if (func == null) return default(T);

            try
            {
                var target = GetMarshalTarget();
                if (target == null || target.IsDisposed) return default(T);

                if (target.InvokeRequired)
                {
                    return (T)target.Invoke(func);
                }

                return func();
            }
            catch
            {
                return default(T);
            }
        }

        private T ControlGet<T>(Control control, Func<T> func)
        {
            if (func == null) return default(T);

            try
            {
                if (control == null || control.IsDisposed) return default(T);

                if (!control.IsHandleCreated)
                {
                    return UiGet(func);
                }

                if (control.InvokeRequired)
                {
                    return (T)control.Invoke(func);
                }

                return func();
            }
            catch
            {
                return default(T);
            }
        }

        private Control GetMarshalTarget()
        {
            if (listInputs != null && !listInputs.IsDisposed && listInputs.IsHandleCreated)
            {
                return listInputs;
            }

            if (!IsDisposed && IsHandleCreated)
            {
                return this;
            }

            return this;
        }
    }
}
