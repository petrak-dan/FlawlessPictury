using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace FlawlessPictury.Presentation.WinForms.Views
{
    partial class RunnerForm
    {
        private IContainer components = null;

        private TableLayoutPanel rootLayout;

        private MenuStrip mainMenu;
        private ToolStripMenuItem menuFile;
        private ToolStripMenuItem miAddFiles;
        private ToolStripMenuItem miAddFolder;
        private ToolStripMenuItem miRemoveSelected;
        private ToolStripMenuItem miClearList;
        private ToolStripSeparator miFileSepBeforeRemove;
        private ToolStripSeparator miFileSepBeforeExit;
        private ToolStripMenuItem miExit;

        private ToolStripMenuItem menuView;
        private ToolStripMenuItem miPreviewPane;
        private ToolStripMenuItem miLog;

        private ToolStripMenuItem menuAdvanced;
        private ToolStripMenuItem miReload;
        private ToolStripMenuItem miPluginExplorer;

        private ToolStripMenuItem menuHelp;
        private ToolStripMenuItem miAbout;

        private SplitContainer splitMain;
        private Panel panelListHost;

        private ToolStrip listToolStrip;
        private ToolStripButton tsAddFiles;
        private ToolStripButton tsAddFolder;
        private ToolStripSeparator tsActionsSeparator;
        private ToolStripButton tsRemoveSelected;
        private ToolStripButton tsClearList;

        private ListView listInputs;
        private ColumnHeader colInputPath;
        private ColumnHeader colStatus;
        private ColumnHeader colSizeOld;
        private ColumnHeader colSizeNew;
        private ColumnHeader colNewFile;

        private Panel panelEmptyState;
        private Label lblEmptyState;

        private TableLayoutPanel previewHostLayout;
        private TabControl tabPreviewSource;
        private TabPage tabPreviewOriginal;
        private TabPage tabPreviewNew;
        private SplitContainer splitRight;
        private Panel panelPreviewFrame;
        private PictureBox picturePreview;

        private Panel panelPropertiesFrame;
        private ListView listProperties;
        private ColumnHeader colPropName;
        private ColumnHeader colPropValue;

        // Bottom controls (preset/output/convert) — placed BELOW the main split.
        private Panel bottomPanel;
        private TableLayoutPanel bottomLayout;
        private Label lblPreset;
        private ComboBox cmbPresets;
        private Label lblOutput;
        private TextBox txtOutput;
        private Button btnBrowseOutput;
        private Button btnRun;

        private StatusStrip statusStrip;
        private ToolStripStatusLabel statusLabel;
        private ToolStripProgressBar progressBar;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.rootLayout = new System.Windows.Forms.TableLayoutPanel();
            this.mainMenu = new System.Windows.Forms.MenuStrip();
            this.menuFile = new System.Windows.Forms.ToolStripMenuItem();
            this.miAddFiles = new System.Windows.Forms.ToolStripMenuItem();
            this.miAddFolder = new System.Windows.Forms.ToolStripMenuItem();
            this.miFileSepBeforeRemove = new System.Windows.Forms.ToolStripSeparator();
            this.miRemoveSelected = new System.Windows.Forms.ToolStripMenuItem();
            this.miClearList = new System.Windows.Forms.ToolStripMenuItem();
            this.miFileSepBeforeExit = new System.Windows.Forms.ToolStripSeparator();
            this.miExit = new System.Windows.Forms.ToolStripMenuItem();
            this.menuView = new System.Windows.Forms.ToolStripMenuItem();
            this.miPreviewPane = new System.Windows.Forms.ToolStripMenuItem();
            this.miLog = new System.Windows.Forms.ToolStripMenuItem();
            this.menuAdvanced = new System.Windows.Forms.ToolStripMenuItem();
            this.miReload = new System.Windows.Forms.ToolStripMenuItem();
            this.miPluginExplorer = new System.Windows.Forms.ToolStripMenuItem();
            this.menuHelp = new System.Windows.Forms.ToolStripMenuItem();
            this.miAbout = new System.Windows.Forms.ToolStripMenuItem();
            this.splitMain = new System.Windows.Forms.SplitContainer();
            this.panelListHost = new System.Windows.Forms.Panel();
            this.listInputs = new System.Windows.Forms.ListView();
            this.colInputPath = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.colStatus = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.colSizeOld = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.colSizeNew = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.colNewFile = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.panelEmptyState = new System.Windows.Forms.Panel();
            this.lblEmptyState = new System.Windows.Forms.Label();
            this.listToolStrip = new System.Windows.Forms.ToolStrip();
            this.tsAddFiles = new System.Windows.Forms.ToolStripButton();
            this.tsAddFolder = new System.Windows.Forms.ToolStripButton();
            this.tsActionsSeparator = new System.Windows.Forms.ToolStripSeparator();
            this.tsRemoveSelected = new System.Windows.Forms.ToolStripButton();
            this.tsClearList = new System.Windows.Forms.ToolStripButton();
            this.previewHostLayout = new System.Windows.Forms.TableLayoutPanel();
            this.tabPreviewSource = new System.Windows.Forms.TabControl();
            this.tabPreviewOriginal = new System.Windows.Forms.TabPage();
            this.tabPreviewNew = new System.Windows.Forms.TabPage();
            this.splitRight = new System.Windows.Forms.SplitContainer();
            this.panelPreviewFrame = new System.Windows.Forms.Panel();
            this.picturePreview = new System.Windows.Forms.PictureBox();
            this.panelPropertiesFrame = new System.Windows.Forms.Panel();
            this.listProperties = new System.Windows.Forms.ListView();
            this.colPropName = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.colPropValue = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.bottomPanel = new System.Windows.Forms.Panel();
            this.bottomLayout = new System.Windows.Forms.TableLayoutPanel();
            this.lblPreset = new System.Windows.Forms.Label();
            this.cmbPresets = new System.Windows.Forms.ComboBox();
            this.lblOutput = new System.Windows.Forms.Label();
            this.btnRun = new System.Windows.Forms.Button();
            this.txtOutput = new System.Windows.Forms.TextBox();
            this.statusStrip = new System.Windows.Forms.StatusStrip();
            this.statusLabel = new System.Windows.Forms.ToolStripStatusLabel();
            this.progressBar = new System.Windows.Forms.ToolStripProgressBar();
            this.btnBrowseOutput = new System.Windows.Forms.Button();
            this.rootLayout.SuspendLayout();
            this.mainMenu.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitMain)).BeginInit();
            this.splitMain.Panel1.SuspendLayout();
            this.splitMain.Panel2.SuspendLayout();
            this.splitMain.SuspendLayout();
            this.panelListHost.SuspendLayout();
            this.panelEmptyState.SuspendLayout();
            this.listToolStrip.SuspendLayout();
            this.previewHostLayout.SuspendLayout();
            this.tabPreviewSource.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitRight)).BeginInit();
            this.splitRight.Panel1.SuspendLayout();
            this.splitRight.Panel2.SuspendLayout();
            this.splitRight.SuspendLayout();
            this.panelPreviewFrame.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.picturePreview)).BeginInit();
            this.panelPropertiesFrame.SuspendLayout();
            this.bottomPanel.SuspendLayout();
            this.bottomLayout.SuspendLayout();
            this.statusStrip.SuspendLayout();
            this.SuspendLayout();
            // 
            // rootLayout
            // 
            this.rootLayout.ColumnCount = 1;
            this.rootLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.rootLayout.Controls.Add(this.mainMenu, 0, 0);
            this.rootLayout.Controls.Add(this.splitMain, 0, 1);
            this.rootLayout.Controls.Add(this.bottomPanel, 0, 2);
            this.rootLayout.Controls.Add(this.statusStrip, 0, 3);
            this.rootLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.rootLayout.Location = new System.Drawing.Point(0, 0);
            this.rootLayout.Margin = new System.Windows.Forms.Padding(2, 3, 2, 3);
            this.rootLayout.Name = "rootLayout";
            this.rootLayout.RowCount = 4;
            this.rootLayout.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.rootLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.rootLayout.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.rootLayout.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.rootLayout.Size = new System.Drawing.Size(787, 431);
            this.rootLayout.TabIndex = 0;
            // 
            // mainMenu
            // 
            this.mainMenu.Dock = System.Windows.Forms.DockStyle.Fill;
            this.mainMenu.ImageScalingSize = new System.Drawing.Size(20, 20);
            this.mainMenu.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.menuFile,
            this.menuView,
            this.menuAdvanced,
            this.menuHelp});
            this.mainMenu.Location = new System.Drawing.Point(0, 0);
            this.mainMenu.Name = "mainMenu";
            this.mainMenu.Padding = new System.Windows.Forms.Padding(4, 2, 0, 2);
            this.mainMenu.Size = new System.Drawing.Size(787, 24);
            this.mainMenu.TabIndex = 0;
            // 
            // menuFile
            // 
            this.menuFile.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.miAddFiles,
            this.miAddFolder,
            this.miFileSepBeforeRemove,
            this.miRemoveSelected,
            this.miClearList,
            this.miFileSepBeforeExit,
            this.miExit});
            this.menuFile.Name = "menuFile";
            this.menuFile.Size = new System.Drawing.Size(37, 20);
            this.menuFile.Text = "&File";
            // 
            // miAddFiles
            // 
            this.miAddFiles.Name = "miAddFiles";
            this.miAddFiles.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.O)));
            this.miAddFiles.Size = new System.Drawing.Size(188, 22);
            this.miAddFiles.Text = "Add Files...";
            // 
            // miAddFolder
            // 
            this.miAddFolder.Name = "miAddFolder";
            this.miAddFolder.Size = new System.Drawing.Size(188, 22);
            this.miAddFolder.Text = "Add Folder...";
            // 
            // miFileSepBeforeRemove
            // 
            this.miFileSepBeforeRemove.Name = "miFileSepBeforeRemove";
            this.miFileSepBeforeRemove.Size = new System.Drawing.Size(185, 6);
            // 
            // miRemoveSelected
            // 
            this.miRemoveSelected.Name = "miRemoveSelected";
            this.miRemoveSelected.ShortcutKeys = System.Windows.Forms.Keys.Delete;
            this.miRemoveSelected.Size = new System.Drawing.Size(188, 22);
            this.miRemoveSelected.Text = "Remove Selected";
            // 
            // miClearList
            // 
            this.miClearList.Name = "miClearList";
            this.miClearList.Size = new System.Drawing.Size(188, 22);
            this.miClearList.Text = "Clear List";
            // 
            // miFileSepBeforeExit
            // 
            this.miFileSepBeforeExit.Name = "miFileSepBeforeExit";
            this.miFileSepBeforeExit.Size = new System.Drawing.Size(185, 6);
            // 
            // miExit
            // 
            this.miExit.Name = "miExit";
            this.miExit.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Alt | System.Windows.Forms.Keys.F4)));
            this.miExit.Size = new System.Drawing.Size(188, 22);
            this.miExit.Text = "E&xit";
            // 
            // menuView
            // 
            this.menuView.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.miPreviewPane,
            this.miLog});
            this.menuView.Name = "menuView";
            this.menuView.Size = new System.Drawing.Size(44, 20);
            this.menuView.Text = "&View";
            // 
            // miPreviewPane
            // 
            this.miPreviewPane.Checked = true;
            this.miPreviewPane.CheckOnClick = true;
            this.miPreviewPane.CheckState = System.Windows.Forms.CheckState.Checked;
            this.miPreviewPane.Name = "miPreviewPane";
            this.miPreviewPane.ShortcutKeyDisplayString = "P";
            this.miPreviewPane.Size = new System.Drawing.Size(158, 22);
            this.miPreviewPane.Text = "Preview Pane";
            // 
            // miLog
            // 
            this.miLog.Name = "miLog";
            this.miLog.ShortcutKeyDisplayString = "L";
            this.miLog.Size = new System.Drawing.Size(158, 22);
            this.miLog.Text = "Log...";
            // 
            // menuAdvanced
            // 
            this.menuAdvanced.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.miReload,
            this.miPluginExplorer});
            this.menuAdvanced.Name = "menuAdvanced";
            this.menuAdvanced.Size = new System.Drawing.Size(72, 20);
            this.menuAdvanced.Text = "&Advanced";
            // 
            // miReload
            // 
            this.miReload.Name = "miReload";
            this.miReload.ShortcutKeys = System.Windows.Forms.Keys.F5;
            this.miReload.Size = new System.Drawing.Size(213, 22);
            this.miReload.Text = "Reload Plugins/Presets";
            // 
            // miPluginExplorer
            // 
            this.miPluginExplorer.Name = "miPluginExplorer";
            this.miPluginExplorer.Size = new System.Drawing.Size(213, 22);
            this.miPluginExplorer.Text = "Preset Editor...";
            // 
            // menuHelp
            // 
            this.menuHelp.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
            this.menuHelp.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.miAbout});
            this.menuHelp.Name = "menuHelp";
            this.menuHelp.Size = new System.Drawing.Size(44, 20);
            this.menuHelp.Text = "&Help";
            // 
            // miAbout
            // 
            this.miAbout.Name = "miAbout";
            this.miAbout.Size = new System.Drawing.Size(193, 22);
            this.miAbout.Text = "About Flawless Pictury";
            // 
            // splitMain
            // 
            this.splitMain.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitMain.Location = new System.Drawing.Point(2, 27);
            this.splitMain.Margin = new System.Windows.Forms.Padding(2, 3, 2, 3);
            this.splitMain.Name = "splitMain";
            // 
            // splitMain.Panel1
            // 
            this.splitMain.Panel1.Controls.Add(this.panelListHost);
            // 
            // splitMain.Panel2
            // 
            this.splitMain.Panel2.Controls.Add(this.previewHostLayout);
            this.splitMain.Panel2MinSize = 180;
            this.splitMain.Size = new System.Drawing.Size(783, 300);
            this.splitMain.SplitterDistance = 515;
            this.splitMain.SplitterWidth = 3;
            this.splitMain.TabIndex = 1;
            // 
            // panelListHost
            // 
            this.panelListHost.Controls.Add(this.listInputs);
            this.panelListHost.Controls.Add(this.panelEmptyState);
            this.panelListHost.Controls.Add(this.listToolStrip);
            this.panelListHost.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panelListHost.Location = new System.Drawing.Point(0, 0);
            this.panelListHost.Margin = new System.Windows.Forms.Padding(2, 3, 2, 3);
            this.panelListHost.Name = "panelListHost";
            this.panelListHost.Size = new System.Drawing.Size(515, 300);
            this.panelListHost.TabIndex = 0;
            // 
            // listInputs
            // 
            this.listInputs.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.colInputPath,
            this.colStatus,
            this.colSizeOld,
            this.colSizeNew,
            this.colNewFile});
            this.listInputs.Dock = System.Windows.Forms.DockStyle.Fill;
            this.listInputs.FullRowSelect = true;
            this.listInputs.HideSelection = false;
            this.listInputs.Location = new System.Drawing.Point(0, 25);
            this.listInputs.Margin = new System.Windows.Forms.Padding(2, 3, 2, 3);
            this.listInputs.Name = "listInputs";
            this.listInputs.Size = new System.Drawing.Size(515, 275);
            this.listInputs.TabIndex = 0;
            this.listInputs.UseCompatibleStateImageBehavior = false;
            this.listInputs.View = System.Windows.Forms.View.Details;
            // 
            // colInputPath
            // 
            this.colInputPath.Text = "File";
            this.colInputPath.Width = 380;
            // 
            // colStatus
            // 
            this.colStatus.Text = "Status";
            this.colStatus.Width = 130;
            // 
            // colSizeOld
            // 
            this.colSizeOld.Text = "Original Size";
            this.colSizeOld.Width = 110;
            // 
            // colSizeNew
            // 
            this.colSizeNew.Text = "New Size";
            this.colSizeNew.Width = 110;
            // 
            // colNewFile
            // 
            this.colNewFile.Text = "New File";
            this.colNewFile.Width = 260;
            // 
            // panelEmptyState
            // 
            this.panelEmptyState.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(242)))), ((int)(((byte)(255)))), ((int)(((byte)(255)))), ((int)(((byte)(255)))));
            this.panelEmptyState.Controls.Add(this.lblEmptyState);
            this.panelEmptyState.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panelEmptyState.Location = new System.Drawing.Point(0, 25);
            this.panelEmptyState.Margin = new System.Windows.Forms.Padding(2, 3, 2, 3);
            this.panelEmptyState.Name = "panelEmptyState";
            this.panelEmptyState.Size = new System.Drawing.Size(515, 275);
            this.panelEmptyState.TabIndex = 1;
            // 
            // lblEmptyState
            // 
            this.lblEmptyState.AutoSize = true;
            this.lblEmptyState.Location = new System.Drawing.Point(14, 15);
            this.lblEmptyState.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.lblEmptyState.Name = "lblEmptyState";
            this.lblEmptyState.Size = new System.Drawing.Size(165, 65);
            this.lblEmptyState.TabIndex = 0;
            this.lblEmptyState.Text = "Drop files here, or use Add Files...\r\n\r\n1) Add input files\r\n2) Choose a preset\r\n3" +
    ") Click Convert";
            // 
            // listToolStrip
            // 
            this.listToolStrip.GripStyle = System.Windows.Forms.ToolStripGripStyle.Hidden;
            this.listToolStrip.ImageScalingSize = new System.Drawing.Size(20, 20);
            this.listToolStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.tsAddFiles,
            this.tsAddFolder,
            this.tsActionsSeparator,
            this.tsRemoveSelected,
            this.tsClearList});
            this.listToolStrip.Location = new System.Drawing.Point(0, 0);
            this.listToolStrip.Name = "listToolStrip";
            this.listToolStrip.Padding = new System.Windows.Forms.Padding(0, 0, 2, 0);
            this.listToolStrip.Size = new System.Drawing.Size(515, 25);
            this.listToolStrip.TabIndex = 2;
            // 
            // tsAddFiles
            // 
            this.tsAddFiles.Name = "tsAddFiles";
            this.tsAddFiles.Size = new System.Drawing.Size(68, 22);
            this.tsAddFiles.Text = "Add Files...";
            // 
            // tsAddFolder
            // 
            this.tsAddFolder.Name = "tsAddFolder";
            this.tsAddFolder.Size = new System.Drawing.Size(78, 22);
            this.tsAddFolder.Text = "Add Folder...";
            // 
            // tsActionsSeparator
            // 
            this.tsActionsSeparator.Name = "tsActionsSeparator";
            this.tsActionsSeparator.Size = new System.Drawing.Size(6, 25);
            // 
            // tsRemoveSelected
            // 
            this.tsRemoveSelected.Name = "tsRemoveSelected";
            this.tsRemoveSelected.Size = new System.Drawing.Size(101, 22);
            this.tsRemoveSelected.Text = "Remove Selected";
            // 
            // tsClearList
            // 
            this.tsClearList.Name = "tsClearList";
            this.tsClearList.Size = new System.Drawing.Size(59, 22);
            this.tsClearList.Text = "Clear List";
            // 
            // previewHostLayout
            // 
            this.previewHostLayout.ColumnCount = 1;
            this.previewHostLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.previewHostLayout.Controls.Add(this.tabPreviewSource, 0, 0);
            this.previewHostLayout.Controls.Add(this.splitRight, 0, 1);
            this.previewHostLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.previewHostLayout.Location = new System.Drawing.Point(0, 0);
            this.previewHostLayout.Margin = new System.Windows.Forms.Padding(0);
            this.previewHostLayout.Name = "previewHostLayout";
            this.previewHostLayout.RowCount = 2;
            this.previewHostLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 21F));
            this.previewHostLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.previewHostLayout.Size = new System.Drawing.Size(265, 300);
            this.previewHostLayout.TabIndex = 1;
            // 
            // tabPreviewSource
            // 
            this.tabPreviewSource.Controls.Add(this.tabPreviewOriginal);
            this.tabPreviewSource.Controls.Add(this.tabPreviewNew);
            this.tabPreviewSource.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tabPreviewSource.Location = new System.Drawing.Point(0, 0);
            this.tabPreviewSource.Margin = new System.Windows.Forms.Padding(0);
            this.tabPreviewSource.Name = "tabPreviewSource";
            this.tabPreviewSource.SelectedIndex = 0;
            this.tabPreviewSource.Size = new System.Drawing.Size(265, 21);
            this.tabPreviewSource.TabIndex = 0;
            // 
            // tabPreviewOriginal
            // 
            this.tabPreviewOriginal.Location = new System.Drawing.Point(4, 22);
            this.tabPreviewOriginal.Margin = new System.Windows.Forms.Padding(2);
            this.tabPreviewOriginal.Name = "tabPreviewOriginal";
            this.tabPreviewOriginal.Padding = new System.Windows.Forms.Padding(2);
            this.tabPreviewOriginal.Size = new System.Drawing.Size(257, 0);
            this.tabPreviewOriginal.TabIndex = 0;
            this.tabPreviewOriginal.Text = "Original File";
            this.tabPreviewOriginal.UseVisualStyleBackColor = true;
            // 
            // tabPreviewNew
            // 
            this.tabPreviewNew.Location = new System.Drawing.Point(4, 22);
            this.tabPreviewNew.Margin = new System.Windows.Forms.Padding(2);
            this.tabPreviewNew.Name = "tabPreviewNew";
            this.tabPreviewNew.Padding = new System.Windows.Forms.Padding(2);
            this.tabPreviewNew.Size = new System.Drawing.Size(257, 0);
            this.tabPreviewNew.TabIndex = 1;
            this.tabPreviewNew.Text = "New File";
            this.tabPreviewNew.UseVisualStyleBackColor = true;
            // 
            // splitRight
            // 
            this.splitRight.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitRight.Location = new System.Drawing.Point(2, 25);
            this.splitRight.Margin = new System.Windows.Forms.Padding(2, 4, 2, 0);
            this.splitRight.Name = "splitRight";
            this.splitRight.Orientation = System.Windows.Forms.Orientation.Horizontal;
            // 
            // splitRight.Panel1
            // 
            this.splitRight.Panel1.Controls.Add(this.panelPreviewFrame);
            // 
            // splitRight.Panel2
            // 
            this.splitRight.Panel2.Controls.Add(this.panelPropertiesFrame);
            this.splitRight.Panel2MinSize = 100;
            this.splitRight.Size = new System.Drawing.Size(261, 275);
            this.splitRight.SplitterDistance = 146;
            this.splitRight.SplitterWidth = 3;
            this.splitRight.TabIndex = 0;
            // 
            // panelPreviewFrame
            // 
            this.panelPreviewFrame.BackColor = System.Drawing.SystemColors.ActiveBorder;
            this.panelPreviewFrame.Controls.Add(this.picturePreview);
            this.panelPreviewFrame.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panelPreviewFrame.Location = new System.Drawing.Point(0, 0);
            this.panelPreviewFrame.Margin = new System.Windows.Forms.Padding(2);
            this.panelPreviewFrame.Name = "panelPreviewFrame";
            this.panelPreviewFrame.Padding = new System.Windows.Forms.Padding(1);
            this.panelPreviewFrame.Size = new System.Drawing.Size(261, 146);
            this.panelPreviewFrame.TabIndex = 0;
            // 
            // picturePreview
            // 
            this.picturePreview.BackColor = System.Drawing.SystemColors.ControlDark;
            this.picturePreview.Dock = System.Windows.Forms.DockStyle.Fill;
            this.picturePreview.Location = new System.Drawing.Point(1, 1);
            this.picturePreview.Margin = new System.Windows.Forms.Padding(0);
            this.picturePreview.Name = "picturePreview";
            this.picturePreview.Size = new System.Drawing.Size(259, 144);
            this.picturePreview.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.picturePreview.TabIndex = 0;
            this.picturePreview.TabStop = false;
            // 
            // panelPropertiesFrame
            // 
            this.panelPropertiesFrame.BackColor = System.Drawing.SystemColors.ActiveBorder;
            this.panelPropertiesFrame.Controls.Add(this.listProperties);
            this.panelPropertiesFrame.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panelPropertiesFrame.Location = new System.Drawing.Point(0, 0);
            this.panelPropertiesFrame.Margin = new System.Windows.Forms.Padding(2);
            this.panelPropertiesFrame.Name = "panelPropertiesFrame";
            this.panelPropertiesFrame.Padding = new System.Windows.Forms.Padding(1);
            this.panelPropertiesFrame.Size = new System.Drawing.Size(261, 126);
            this.panelPropertiesFrame.TabIndex = 0;
            // 
            // listProperties
            // 
            this.listProperties.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.listProperties.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.colPropName,
            this.colPropValue});
            this.listProperties.Dock = System.Windows.Forms.DockStyle.Fill;
            this.listProperties.FullRowSelect = true;
            this.listProperties.HideSelection = false;
            this.listProperties.Location = new System.Drawing.Point(1, 1);
            this.listProperties.Margin = new System.Windows.Forms.Padding(2, 3, 2, 3);
            this.listProperties.Name = "listProperties";
            this.listProperties.Size = new System.Drawing.Size(259, 124);
            this.listProperties.TabIndex = 0;
            this.listProperties.UseCompatibleStateImageBehavior = false;
            this.listProperties.View = System.Windows.Forms.View.Details;
            // 
            // colPropName
            // 
            this.colPropName.Text = "Property";
            this.colPropName.Width = 140;
            // 
            // colPropValue
            // 
            this.colPropValue.Text = "Value";
            this.colPropValue.Width = 240;
            // 
            // bottomPanel
            // 
            this.bottomPanel.Controls.Add(this.bottomLayout);
            this.bottomPanel.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.bottomPanel.Location = new System.Drawing.Point(2, 333);
            this.bottomPanel.Margin = new System.Windows.Forms.Padding(2, 3, 2, 3);
            this.bottomPanel.Name = "bottomPanel";
            this.bottomPanel.Padding = new System.Windows.Forms.Padding(8, 5, 8, 6);
            this.bottomPanel.Size = new System.Drawing.Size(783, 65);
            this.bottomPanel.TabIndex = 2;
            // 
            // bottomLayout
            // 
            this.bottomLayout.ColumnCount = 3;
            this.bottomLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.bottomLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.bottomLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.bottomLayout.Controls.Add(this.lblPreset, 0, 0);
            this.bottomLayout.Controls.Add(this.cmbPresets, 1, 0);
            this.bottomLayout.Controls.Add(this.lblOutput, 0, 1);
            this.bottomLayout.Controls.Add(this.btnRun, 2, 0);
            this.bottomLayout.Controls.Add(this.txtOutput, 1, 1);
            this.bottomLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.bottomLayout.Location = new System.Drawing.Point(8, 5);
            this.bottomLayout.Margin = new System.Windows.Forms.Padding(2, 3, 2, 3);
            this.bottomLayout.Name = "bottomLayout";
            this.bottomLayout.RowCount = 2;
            this.bottomLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.bottomLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.bottomLayout.Size = new System.Drawing.Size(767, 54);
            this.bottomLayout.TabIndex = 0;
            // 
            // lblPreset
            // 
            this.lblPreset.AutoSize = true;
            this.lblPreset.Dock = System.Windows.Forms.DockStyle.Top;
            this.lblPreset.Location = new System.Drawing.Point(2, 7);
            this.lblPreset.Margin = new System.Windows.Forms.Padding(2, 7, 2, 0);
            this.lblPreset.MinimumSize = new System.Drawing.Size(80, 0);
            this.lblPreset.Name = "lblPreset";
            this.lblPreset.Size = new System.Drawing.Size(87, 13);
            this.lblPreset.TabIndex = 0;
            this.lblPreset.Text = "Choose Preset:";
            this.lblPreset.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // cmbPresets
            // 
            this.cmbPresets.Dock = System.Windows.Forms.DockStyle.Top;
            this.cmbPresets.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbPresets.FormattingEnabled = true;
            this.cmbPresets.Location = new System.Drawing.Point(93, 3);
            this.cmbPresets.Margin = new System.Windows.Forms.Padding(2, 3, 2, 3);
            this.cmbPresets.Name = "cmbPresets";
            this.cmbPresets.Size = new System.Drawing.Size(568, 21);
            this.cmbPresets.TabIndex = 1;
            // 
            // lblOutput
            // 
            this.lblOutput.AutoSize = true;
            this.lblOutput.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.lblOutput.Location = new System.Drawing.Point(2, 34);
            this.lblOutput.Margin = new System.Windows.Forms.Padding(2, 0, 2, 7);
            this.lblOutput.MinimumSize = new System.Drawing.Size(80, 0);
            this.lblOutput.Name = "lblOutput";
            this.lblOutput.Size = new System.Drawing.Size(87, 13);
            this.lblOutput.TabIndex = 2;
            this.lblOutput.Text = "Output Directory:";
            this.lblOutput.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // btnRun
            // 
            this.btnRun.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnRun.Location = new System.Drawing.Point(665, 2);
            this.btnRun.Margin = new System.Windows.Forms.Padding(2);
            this.btnRun.MinimumSize = new System.Drawing.Size(100, 0);
            this.btnRun.Name = "btnRun";
            this.btnRun.Padding = new System.Windows.Forms.Padding(2);
            this.bottomLayout.SetRowSpan(this.btnRun, 2);
            this.btnRun.Size = new System.Drawing.Size(100, 50);
            this.btnRun.TabIndex = 5;
            this.btnRun.Text = "Convert";
            this.btnRun.UseVisualStyleBackColor = true;
            // 
            // txtOutput
            // 
            this.txtOutput.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.txtOutput.Location = new System.Drawing.Point(93, 31);
            this.txtOutput.Margin = new System.Windows.Forms.Padding(2, 3, 2, 3);
            this.txtOutput.Name = "txtOutput";
            this.txtOutput.Size = new System.Drawing.Size(568, 20);
            this.txtOutput.TabIndex = 3;
            // 
            // statusStrip
            // 
            this.statusStrip.Dock = System.Windows.Forms.DockStyle.Fill;
            this.statusStrip.ImageScalingSize = new System.Drawing.Size(20, 20);
            this.statusStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.statusLabel,
            this.progressBar});
            this.statusStrip.Location = new System.Drawing.Point(0, 401);
            this.statusStrip.Name = "statusStrip";
            this.statusStrip.Padding = new System.Windows.Forms.Padding(1, 0, 10, 0);
            this.statusStrip.Size = new System.Drawing.Size(787, 30);
            this.statusStrip.TabIndex = 3;
            // 
            // statusLabel
            // 
            this.statusLabel.Name = "statusLabel";
            this.statusLabel.Size = new System.Drawing.Size(699, 25);
            this.statusLabel.Spring = true;
            this.statusLabel.Text = "Ready.";
            this.statusLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // progressBar
            // 
            this.progressBar.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
            this.progressBar.Name = "progressBar";
            this.progressBar.Size = new System.Drawing.Size(75, 24);
            // 
            // btnBrowseOutput
            // 
            this.btnBrowseOutput.Location = new System.Drawing.Point(0, 0);
            this.btnBrowseOutput.Name = "btnBrowseOutput";
            this.btnBrowseOutput.Size = new System.Drawing.Size(30, 22);
            this.btnBrowseOutput.TabIndex = 4;
            this.btnBrowseOutput.UseVisualStyleBackColor = true;
            // 
            // RunnerForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(787, 431);
            this.Controls.Add(this.rootLayout);
            this.MainMenuStrip = this.mainMenu;
            this.Margin = new System.Windows.Forms.Padding(2, 3, 2, 3);
            this.MinimumSize = new System.Drawing.Size(520, 393);
            this.Name = "RunnerForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Flawless Pictury";
            this.rootLayout.ResumeLayout(false);
            this.rootLayout.PerformLayout();
            this.mainMenu.ResumeLayout(false);
            this.mainMenu.PerformLayout();
            this.splitMain.Panel1.ResumeLayout(false);
            this.splitMain.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitMain)).EndInit();
            this.splitMain.ResumeLayout(false);
            this.panelListHost.ResumeLayout(false);
            this.panelListHost.PerformLayout();
            this.panelEmptyState.ResumeLayout(false);
            this.panelEmptyState.PerformLayout();
            this.listToolStrip.ResumeLayout(false);
            this.listToolStrip.PerformLayout();
            this.previewHostLayout.ResumeLayout(false);
            this.tabPreviewSource.ResumeLayout(false);
            this.splitRight.Panel1.ResumeLayout(false);
            this.splitRight.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitRight)).EndInit();
            this.splitRight.ResumeLayout(false);
            this.panelPreviewFrame.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.picturePreview)).EndInit();
            this.panelPropertiesFrame.ResumeLayout(false);
            this.bottomPanel.ResumeLayout(false);
            this.bottomLayout.ResumeLayout(false);
            this.bottomLayout.PerformLayout();
            this.statusStrip.ResumeLayout(false);
            this.statusStrip.PerformLayout();
            this.ResumeLayout(false);

        }
    }
}
