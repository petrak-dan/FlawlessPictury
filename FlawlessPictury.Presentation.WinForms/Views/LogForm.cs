using System;
using System.Drawing;
using System.Windows.Forms;
using FlawlessPictury.AppCore.CrossCutting;
using FlawlessPictury.Presentation.WinForms.CrossCutting;
using FlawlessPictury.Presentation.WinForms.Win32;

namespace FlawlessPictury.Presentation.WinForms.Views
{
    /// <summary>
    /// Modeless log window. [X] hides (does not dispose) so background logging stays safe.
    /// </summary>
    public sealed partial class LogForm : Form, ILogView
    {
        private Func<int, LogEventArgs> _provider;
        private WindowsStockIconProvider _icons;
        private ImageList _imageList;
        private int _iconBlank;
        private int _iconInfo;
        private int _iconWarn;
        private int _iconError;
        private int _rowCount;
        private ListViewItem _cachedItem;
        private int _cachedIndex;

        public LogForm()
        {
            InitializeComponent();

            KeyPreview = true;
            _icons = new WindowsStockIconProvider();
            InitializeIcons();
            SetListViewDoubleBuffered(listLog);

            listLog.VirtualMode = true;
            listLog.RetrieveVirtualItem += OnRetrieveVirtualItem;
            listLog.Resize += (s, e) =>
            {
                ResizeColumnToFit();
                ReapplyAutoScrollPosition();
                ForceListRedraw();
            };
            Shown += (s, e) =>
            {
                ResizeColumnToFit();
                RefreshRows();
            };
            VisibleChanged += (s, e) =>
            {
                if (Visible)
                {
                    RefreshRows();
                }
            };
            Disposed += OnDisposedCleanup;

            miFileSaveAs.Click += (s, e) => SaveAsRequested?.Invoke(this, EventArgs.Empty);
            miFileClose.Click += (s, e) => CloseRequested?.Invoke(this, EventArgs.Empty);
            miViewAutoScroll.CheckedChanged += (s, e) => AutoScrollChanged?.Invoke(this, EventArgs.Empty);
            miViewDebugMessages.CheckedChanged += (s, e) => ShowDebugMessagesChanged?.Invoke(this, EventArgs.Empty);
            miToolsCopyAll.Click += (s, e) => CopyAllRequested?.Invoke(this, EventArgs.Empty);
            miToolsClear.Click += (s, e) => ClearRequested?.Invoke(this, EventArgs.Empty);
        }

        public event EventHandler ClearRequested;
        public event EventHandler CopyAllRequested;
        public event EventHandler SaveAsRequested;
        public event EventHandler CloseRequested;
        public event EventHandler AutoScrollChanged;
        public event EventHandler ShowDebugMessagesChanged;

        public bool AutoScrollEnabled => miViewAutoScroll.Checked;
        public bool ShowDebugMessages => miViewDebugMessages.Checked;
        public bool IsVisible => Visible;

        public void ShowModeless(object owner)
        {
            if (Visible)
            {
                RefreshRows();
                return;
            }

            Show();
            RefreshRows();
        }

        public void HideWindow()
        {
            Hide();
        }

        public void BringToFrontAndActivate()
        {
            if (!Visible)
            {
                return;
            }

            if (WindowState == FormWindowState.Minimized)
            {
                WindowState = FormWindowState.Normal;
            }

            BringToFront();
            Activate();
        }

        public void SetRowCount(int count)
        {
            _rowCount = Math.Max(0, count);
        }

        public void SetRowProvider(Func<int, LogEventArgs> provider)
        {
            _provider = provider;
        }

        public void RefreshRows()
        {
            try
            {
                listLog.BeginUpdate();
                try
                {
                    _cachedItem = null;
                    _cachedIndex = -1;
                    listLog.VirtualListSize = _rowCount;
                }
                finally
                {
                    listLog.EndUpdate();
                }

                ResizeColumnToFit();
                ReapplyAutoScrollPosition();
                ForceListRedraw();
            }
            catch
            {
            }
        }

        public void EnsureRowVisible(int index)
        {
            try
            {
                if (_rowCount <= 0)
                {
                    return;
                }

                var safeIndex = Math.Max(0, Math.Min(_rowCount - 1, index));
                var targetIndex = AllRowsFitInViewport() ? 0 : safeIndex;
                if (targetIndex >= 0 && targetIndex < listLog.VirtualListSize)
                {
                    listLog.EnsureVisible(targetIndex);
                }
            }
            catch
            {
            }
        }

        public string PromptSaveAsPath(string initialDirectory, string initialFileName)
        {
            using (var dialog = new SaveFileDialog())
            {
                dialog.Filter = "Text files (*.txt)|*.txt|Log files (*.log)|*.log|All files (*.*)|*.*";
                dialog.DefaultExt = "txt";
                dialog.AddExtension = true;
                dialog.InitialDirectory = string.IsNullOrWhiteSpace(initialDirectory) ? AppDomain.CurrentDomain.BaseDirectory : initialDirectory;
                dialog.FileName = string.IsNullOrWhiteSpace(initialFileName) ? "FlawlessPictury_Log.txt" : initialFileName;
                return dialog.ShowDialog(this) == DialogResult.OK ? dialog.FileName : null;
            }
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == Keys.L)
            {
                Hide();
                return true;
            }

            return base.ProcessCmdKey(ref msg, keyData);
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.UserClosing || e.CloseReason == CloseReason.None)
            {
                e.Cancel = true;
                Hide();
                return;
            }

            base.OnFormClosing(e);
        }

        private ListViewItem BuildListViewItem(LogEventArgs evt)
        {
            if (evt == null)
            {
                return new ListViewItem(string.Empty) { ImageIndex = _iconBlank };
            }

            var text = string.Format("{0:HH:mm:ss}  {1}", evt.Timestamp, BuildUserVisibleMessage(evt));
            return new ListViewItem(text) { ImageIndex = GetIconIndex(evt.Level) };
        }

        private static string BuildUserVisibleMessage(LogEventArgs evt)
        {
            if (evt.Exception == null)
            {
                return evt.Message ?? string.Empty;
            }

            return (evt.Message ?? string.Empty) + " | " + evt.Exception.GetType().Name + ": " + evt.Exception.Message;
        }

        private int GetIconIndex(LogLevel level)
        {
            switch (level)
            {
                case LogLevel.Warn:
                    return _iconWarn;
                case LogLevel.Error:
                    return _iconError;
                case LogLevel.Debug:
                case LogLevel.Trace:
                    return _iconBlank;
                default:
                    return _iconInfo;
            }
        }

        private void InitializeIcons()
        {
            _imageList = new ImageList
            {
                ImageSize = new Size(16, 16),
                ColorDepth = ColorDepth.Depth32Bit
            };

            var blank = new Bitmap(16, 16);
            _imageList.Images.Add(blank);
            _iconBlank = _imageList.Images.Count - 1;

            AddOrFallback(WindowsStockIconProvider.WindowsStockIconId.Info, SystemIcons.Information, out _iconInfo);
            AddOrFallback(WindowsStockIconProvider.WindowsStockIconId.Warning, SystemIcons.Warning, out _iconWarn);
            AddOrFallback(WindowsStockIconProvider.WindowsStockIconId.Error, SystemIcons.Error, out _iconError);

            listLog.SmallImageList = _imageList;
        }

        private void AddOrFallback(WindowsStockIconProvider.WindowsStockIconId id, Icon fallback, out int index)
        {
            Icon icon = null;

            try
            {
                icon = _icons.GetSmall(id);
            }
            catch
            {
                icon = null;
            }

            if (icon == null)
            {
                icon = fallback;
            }

            _imageList.Images.Add(icon.ToBitmap());
            index = _imageList.Images.Count - 1;
        }

        private void ReapplyAutoScrollPosition()
        {
            if (!miViewAutoScroll.Checked || !IsHandleCreated)
            {
                return;
            }

            try
            {
                BeginInvoke((MethodInvoker)delegate
                {
                    try
                    {
                        if (_rowCount <= 0)
                        {
                            return;
                        }

                        EnsureRowVisible(_rowCount - 1);
                        ForceListRedraw();
                    }
                    catch
                    {
                    }
                });
            }
            catch
            {
            }
        }

        private bool AllRowsFitInViewport()
        {
            if (_rowCount <= 0)
            {
                return true;
            }

            var rowHeight = MeasureRowHeight();
            var usableHeight = Math.Max(0, listLog.ClientSize.Height - 2);
            return (_rowCount * rowHeight) <= usableHeight;
        }

        private int MeasureRowHeight()
        {
            try
            {
                using (var g = listLog.CreateGraphics())
                {
                    var height = TextRenderer.MeasureText(g, "Ag", listLog.Font).Height + 4;
                    return Math.Max(18, height);
                }
            }
            catch
            {
                return Math.Max(18, listLog.Font.Height + 6);
            }
        }

        private void OnRetrieveVirtualItem(object sender, RetrieveVirtualItemEventArgs e)
        {
            if (e == null)
            {
                return;
            }

            if (_cachedItem != null && e.ItemIndex == _cachedIndex)
            {
                e.Item = _cachedItem;
                return;
            }

            var evt = _provider == null ? null : _provider(e.ItemIndex);
            var item = BuildListViewItem(evt);
            _cachedIndex = e.ItemIndex;
            _cachedItem = item;
            e.Item = item;
        }

        private void ResizeColumnToFit()
        {
            try
            {
                colLine.Width = Math.Max(100, listLog.ClientSize.Width - 4);
            }
            catch
            {
            }
        }

        private void ForceListRedraw()
        {
            try
            {
                listLog.Invalidate();
                listLog.Update();
            }
            catch
            {
            }
        }

        private static void SetListViewDoubleBuffered(ListView list)
        {
            if (list == null)
            {
                return;
            }

            try
            {
                var property = typeof(Control).GetProperty("DoubleBuffered", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                property?.SetValue(list, true, null);
            }
            catch
            {
            }
        }

        private void OnDisposedCleanup(object sender, EventArgs e)
        {
            try { _icons?.Dispose(); } catch { }
            try { _imageList?.Dispose(); } catch { }

            _icons = null;
            _imageList = null;
        }
    }
}
