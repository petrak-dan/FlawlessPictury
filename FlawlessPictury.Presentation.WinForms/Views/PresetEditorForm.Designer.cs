using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace FlawlessPictury.Presentation.WinForms.Views
{
    partial class PresetEditorForm
    {
        private IContainer components = null;

        private TableLayoutPanel _rootLayout;
        private TableLayoutPanel _metaLayout;
        private Label _lblPreset;
        private Label _lblPresetId;
        private Label _lblDisplayName;
        private Label _lblDescription;
        private ComboBox _cmbPresets;
        private TextBox _txtPresetId;
        private TextBox _txtDisplayName;
        private TextBox _txtDescription;
        private TableLayoutPanel _contentLayout;
        private GroupBox _grpSteps;
        private TableLayoutPanel _stepsLayout;
        private ListBox _listSteps;
        private FlowLayoutPanel _stepsButtonsPanel;
        private Button _btnAddStep;
        private Button _btnRemoveStep;
        private Button _btnMoveUp;
        private Button _btnMoveDown;
        private GroupBox _grpStepDetails;
        private TableLayoutPanel _detailsLayout;
        private Label _lblPlugin;
        private Label _lblCapability;
        private Label _lblSummary;
        private Label _lblParameters;
        private Label _lblValidation;
        private ComboBox _cmbPlugin;
        private ComboBox _cmbCapability;
        private TextBox _txtCapabilitySummary;
        private TabControl _tabDetails;
        private TabPage _tabStep;
        private TabPage _tabEncoder;
        private TabPage _tabMetric;
        private TabPage _tabOutput;
        private PropertyGrid _propertyGrid;
        private TableLayoutPanel _encoderLayout;
        private Label _lblEncoderPlugin;
        private Label _lblEncoderCapability;
        private Label _lblEncoderSummary;
        private Label _lblEncoderParameters;
        private ComboBox _cmbEncoderPlugin;
        private ComboBox _cmbEncoderCapability;
        private TextBox _txtEncoderSummary;
        private PropertyGrid _propertyGridEncoder;
        private TableLayoutPanel _metricLayout;
        private Label _lblMetricPlugin;
        private Label _lblMetricCapability;
        private Label _lblMetricSummary;
        private Label _lblMetricParameters;
        private ComboBox _cmbMetricPlugin;
        private ComboBox _cmbMetricCapability;
        private TextBox _txtMetricSummary;
        private PropertyGrid _propertyGridMetric;
        private PropertyGrid _propertyGridOutput;
        private TextBox _txtValidationSummary;
        private TableLayoutPanel _bottomButtonsLayout;
        private FlowLayoutPanel _presetButtonsPanel;
        private FlowLayoutPanel _dialogButtonsPanel;
        private Button _btnNew;
        private Button _btnDuplicate;
        private Button _btnReload;
        private Button _btnSave;
        private Button _btnSaveAs;
        private Button _btnDelete;
        private Button _btnOpenLocation;
        private Button _btnOk;
        private Button _btnCancel;

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
            this._rootLayout = new System.Windows.Forms.TableLayoutPanel();
            this._metaLayout = new System.Windows.Forms.TableLayoutPanel();
            this._lblPreset = new System.Windows.Forms.Label();
            this._cmbPresets = new System.Windows.Forms.ComboBox();
            this._lblPresetId = new System.Windows.Forms.Label();
            this._txtPresetId = new System.Windows.Forms.TextBox();
            this._lblDisplayName = new System.Windows.Forms.Label();
            this._txtDisplayName = new System.Windows.Forms.TextBox();
            this._lblDescription = new System.Windows.Forms.Label();
            this._txtDescription = new System.Windows.Forms.TextBox();
            this._contentLayout = new System.Windows.Forms.TableLayoutPanel();
            this._grpSteps = new System.Windows.Forms.GroupBox();
            this._stepsLayout = new System.Windows.Forms.TableLayoutPanel();
            this._listSteps = new System.Windows.Forms.ListBox();
            this._stepsButtonsPanel = new System.Windows.Forms.FlowLayoutPanel();
            this._btnAddStep = new System.Windows.Forms.Button();
            this._btnRemoveStep = new System.Windows.Forms.Button();
            this._btnMoveUp = new System.Windows.Forms.Button();
            this._btnMoveDown = new System.Windows.Forms.Button();
            this._grpStepDetails = new System.Windows.Forms.GroupBox();
            this._detailsLayout = new System.Windows.Forms.TableLayoutPanel();
            this._lblPlugin = new System.Windows.Forms.Label();
            this._cmbPlugin = new System.Windows.Forms.ComboBox();
            this._lblCapability = new System.Windows.Forms.Label();
            this._cmbCapability = new System.Windows.Forms.ComboBox();
            this._lblSummary = new System.Windows.Forms.Label();
            this._txtCapabilitySummary = new System.Windows.Forms.TextBox();
            this._lblParameters = new System.Windows.Forms.Label();
            this._tabDetails = new System.Windows.Forms.TabControl();
            this._tabStep = new System.Windows.Forms.TabPage();
            this._propertyGrid = new System.Windows.Forms.PropertyGrid();
            this._tabEncoder = new System.Windows.Forms.TabPage();
            this._encoderLayout = new System.Windows.Forms.TableLayoutPanel();
            this._lblEncoderPlugin = new System.Windows.Forms.Label();
            this._cmbEncoderPlugin = new System.Windows.Forms.ComboBox();
            this._lblEncoderCapability = new System.Windows.Forms.Label();
            this._cmbEncoderCapability = new System.Windows.Forms.ComboBox();
            this._lblEncoderSummary = new System.Windows.Forms.Label();
            this._txtEncoderSummary = new System.Windows.Forms.TextBox();
            this._lblEncoderParameters = new System.Windows.Forms.Label();
            this._propertyGridEncoder = new System.Windows.Forms.PropertyGrid();
            this._tabMetric = new System.Windows.Forms.TabPage();
            this._metricLayout = new System.Windows.Forms.TableLayoutPanel();
            this._lblMetricPlugin = new System.Windows.Forms.Label();
            this._cmbMetricPlugin = new System.Windows.Forms.ComboBox();
            this._lblMetricCapability = new System.Windows.Forms.Label();
            this._cmbMetricCapability = new System.Windows.Forms.ComboBox();
            this._lblMetricSummary = new System.Windows.Forms.Label();
            this._txtMetricSummary = new System.Windows.Forms.TextBox();
            this._lblMetricParameters = new System.Windows.Forms.Label();
            this._propertyGridMetric = new System.Windows.Forms.PropertyGrid();
            this._tabOutput = new System.Windows.Forms.TabPage();
            this._propertyGridOutput = new System.Windows.Forms.PropertyGrid();
            this._lblValidation = new System.Windows.Forms.Label();
            this._txtValidationSummary = new System.Windows.Forms.TextBox();
            this._bottomButtonsLayout = new System.Windows.Forms.TableLayoutPanel();
            this._presetButtonsPanel = new System.Windows.Forms.FlowLayoutPanel();
            this._btnNew = new System.Windows.Forms.Button();
            this._btnDuplicate = new System.Windows.Forms.Button();
            this._btnReload = new System.Windows.Forms.Button();
            this._btnSave = new System.Windows.Forms.Button();
            this._btnSaveAs = new System.Windows.Forms.Button();
            this._btnDelete = new System.Windows.Forms.Button();
            this._btnOpenLocation = new System.Windows.Forms.Button();
            this._dialogButtonsPanel = new System.Windows.Forms.FlowLayoutPanel();
            this._btnOk = new System.Windows.Forms.Button();
            this._btnCancel = new System.Windows.Forms.Button();
            this._rootLayout.SuspendLayout();
            this._metaLayout.SuspendLayout();
            this._contentLayout.SuspendLayout();
            this._grpSteps.SuspendLayout();
            this._stepsLayout.SuspendLayout();
            this._stepsButtonsPanel.SuspendLayout();
            this._grpStepDetails.SuspendLayout();
            this._detailsLayout.SuspendLayout();
            this._tabDetails.SuspendLayout();
            this._tabStep.SuspendLayout();
            this._tabEncoder.SuspendLayout();
            this._encoderLayout.SuspendLayout();
            this._tabMetric.SuspendLayout();
            this._metricLayout.SuspendLayout();
            this._tabOutput.SuspendLayout();
            this._bottomButtonsLayout.SuspendLayout();
            this._presetButtonsPanel.SuspendLayout();
            this._dialogButtonsPanel.SuspendLayout();
            this.SuspendLayout();
            // 
            // _rootLayout
            // 
            this._rootLayout.ColumnCount = 1;
            this._rootLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this._rootLayout.Controls.Add(this._metaLayout, 0, 0);
            this._rootLayout.Controls.Add(this._contentLayout, 0, 1);
            this._rootLayout.Controls.Add(this._bottomButtonsLayout, 0, 2);
            this._rootLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this._rootLayout.Location = new System.Drawing.Point(0, 0);
            this._rootLayout.Margin = new System.Windows.Forms.Padding(6, 6, 6, 6);
            this._rootLayout.Name = "_rootLayout";
            this._rootLayout.Padding = new System.Windows.Forms.Padding(16, 15, 16, 15);
            this._rootLayout.RowCount = 3;
            this._rootLayout.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this._rootLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this._rootLayout.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this._rootLayout.Size = new System.Drawing.Size(2480, 1462);
            this._rootLayout.TabIndex = 0;
            // 
            // _metaLayout
            // 
            this._metaLayout.AutoSize = true;
            this._metaLayout.ColumnCount = 2;
            this._metaLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 220F));
            this._metaLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this._metaLayout.Controls.Add(this._lblPreset, 0, 0);
            this._metaLayout.Controls.Add(this._cmbPresets, 1, 0);
            this._metaLayout.Controls.Add(this._lblPresetId, 0, 1);
            this._metaLayout.Controls.Add(this._txtPresetId, 1, 1);
            this._metaLayout.Controls.Add(this._lblDisplayName, 0, 2);
            this._metaLayout.Controls.Add(this._txtDisplayName, 1, 2);
            this._metaLayout.Controls.Add(this._lblDescription, 0, 3);
            this._metaLayout.Controls.Add(this._txtDescription, 1, 3);
            this._metaLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this._metaLayout.Location = new System.Drawing.Point(22, 21);
            this._metaLayout.Margin = new System.Windows.Forms.Padding(6, 6, 6, 6);
            this._metaLayout.Name = "_metaLayout";
            this._metaLayout.RowCount = 4;
            this._metaLayout.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this._metaLayout.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this._metaLayout.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this._metaLayout.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this._metaLayout.Size = new System.Drawing.Size(2436, 263);
            this._metaLayout.TabIndex = 0;
            // 
            // _lblPreset
            // 
            this._lblPreset.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this._lblPreset.AutoSize = true;
            this._lblPreset.Location = new System.Drawing.Point(6, 12);
            this._lblPreset.Margin = new System.Windows.Forms.Padding(6, 12, 6, 12);
            this._lblPreset.Name = "_lblPreset";
            this._lblPreset.Size = new System.Drawing.Size(74, 25);
            this._lblPreset.TabIndex = 0;
            this._lblPreset.Text = "Preset";
            // 
            // _cmbPresets
            // 
            this._cmbPresets.Dock = System.Windows.Forms.DockStyle.Fill;
            this._cmbPresets.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this._cmbPresets.FormattingEnabled = true;
            this._cmbPresets.Location = new System.Drawing.Point(226, 6);
            this._cmbPresets.Margin = new System.Windows.Forms.Padding(6, 6, 6, 6);
            this._cmbPresets.Name = "_cmbPresets";
            this._cmbPresets.Size = new System.Drawing.Size(2204, 33);
            this._cmbPresets.TabIndex = 1;
            // 
            // _lblPresetId
            // 
            this._lblPresetId.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this._lblPresetId.AutoSize = true;
            this._lblPresetId.Location = new System.Drawing.Point(6, 61);
            this._lblPresetId.Margin = new System.Windows.Forms.Padding(6, 12, 6, 12);
            this._lblPresetId.Name = "_lblPresetId";
            this._lblPresetId.Size = new System.Drawing.Size(97, 25);
            this._lblPresetId.TabIndex = 2;
            this._lblPresetId.Text = "Preset Id";
            // 
            // _txtPresetId
            // 
            this._txtPresetId.Dock = System.Windows.Forms.DockStyle.Fill;
            this._txtPresetId.Location = new System.Drawing.Point(226, 55);
            this._txtPresetId.Margin = new System.Windows.Forms.Padding(6, 6, 6, 6);
            this._txtPresetId.Name = "_txtPresetId";
            this._txtPresetId.Size = new System.Drawing.Size(2204, 31);
            this._txtPresetId.TabIndex = 3;
            // 
            // _lblDisplayName
            // 
            this._lblDisplayName.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this._lblDisplayName.AutoSize = true;
            this._lblDisplayName.Location = new System.Drawing.Point(6, 110);
            this._lblDisplayName.Margin = new System.Windows.Forms.Padding(6, 12, 6, 12);
            this._lblDisplayName.Name = "_lblDisplayName";
            this._lblDisplayName.Size = new System.Drawing.Size(145, 25);
            this._lblDisplayName.TabIndex = 4;
            this._lblDisplayName.Text = "Display Name";
            // 
            // _txtDisplayName
            // 
            this._txtDisplayName.Dock = System.Windows.Forms.DockStyle.Fill;
            this._txtDisplayName.Location = new System.Drawing.Point(226, 104);
            this._txtDisplayName.Margin = new System.Windows.Forms.Padding(6, 6, 6, 6);
            this._txtDisplayName.Name = "_txtDisplayName";
            this._txtDisplayName.Size = new System.Drawing.Size(2204, 31);
            this._txtDisplayName.TabIndex = 5;
            // 
            // _lblDescription
            // 
            this._lblDescription.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this._lblDescription.AutoSize = true;
            this._lblDescription.Location = new System.Drawing.Point(6, 192);
            this._lblDescription.Margin = new System.Windows.Forms.Padding(6, 12, 6, 12);
            this._lblDescription.Name = "_lblDescription";
            this._lblDescription.Size = new System.Drawing.Size(120, 25);
            this._lblDescription.TabIndex = 6;
            this._lblDescription.Text = "Description";
            // 
            // _txtDescription
            // 
            this._txtDescription.Dock = System.Windows.Forms.DockStyle.Fill;
            this._txtDescription.Location = new System.Drawing.Point(226, 153);
            this._txtDescription.Margin = new System.Windows.Forms.Padding(6, 6, 6, 6);
            this._txtDescription.Multiline = true;
            this._txtDescription.Name = "_txtDescription";
            this._txtDescription.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this._txtDescription.Size = new System.Drawing.Size(2204, 104);
            this._txtDescription.TabIndex = 7;
            // 
            // _contentLayout
            // 
            this._contentLayout.ColumnCount = 2;
            this._contentLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 34F));
            this._contentLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 66F));
            this._contentLayout.Controls.Add(this._grpSteps, 0, 0);
            this._contentLayout.Controls.Add(this._grpStepDetails, 1, 0);
            this._contentLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this._contentLayout.Location = new System.Drawing.Point(22, 296);
            this._contentLayout.Margin = new System.Windows.Forms.Padding(6, 6, 6, 6);
            this._contentLayout.Name = "_contentLayout";
            this._contentLayout.RowCount = 1;
            this._contentLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this._contentLayout.Size = new System.Drawing.Size(2436, 1055);
            this._contentLayout.TabIndex = 1;
            // 
            // _grpSteps
            // 
            this._grpSteps.Controls.Add(this._stepsLayout);
            this._grpSteps.Dock = System.Windows.Forms.DockStyle.Fill;
            this._grpSteps.Location = new System.Drawing.Point(6, 6);
            this._grpSteps.Margin = new System.Windows.Forms.Padding(6, 6, 6, 6);
            this._grpSteps.Name = "_grpSteps";
            this._grpSteps.Padding = new System.Windows.Forms.Padding(16, 15, 16, 15);
            this._grpSteps.Size = new System.Drawing.Size(816, 1043);
            this._grpSteps.TabIndex = 0;
            this._grpSteps.TabStop = false;
            this._grpSteps.Text = "Steps";
            // 
            // _stepsLayout
            // 
            this._stepsLayout.ColumnCount = 1;
            this._stepsLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this._stepsLayout.Controls.Add(this._listSteps, 0, 0);
            this._stepsLayout.Controls.Add(this._stepsButtonsPanel, 0, 1);
            this._stepsLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this._stepsLayout.Location = new System.Drawing.Point(16, 39);
            this._stepsLayout.Margin = new System.Windows.Forms.Padding(6, 6, 6, 6);
            this._stepsLayout.Name = "_stepsLayout";
            this._stepsLayout.RowCount = 2;
            this._stepsLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this._stepsLayout.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this._stepsLayout.Size = new System.Drawing.Size(784, 989);
            this._stepsLayout.TabIndex = 0;
            // 
            // _listSteps
            // 
            this._listSteps.Dock = System.Windows.Forms.DockStyle.Fill;
            this._listSteps.FormattingEnabled = true;
            this._listSteps.IntegralHeight = false;
            this._listSteps.ItemHeight = 25;
            this._listSteps.Location = new System.Drawing.Point(6, 6);
            this._listSteps.Margin = new System.Windows.Forms.Padding(6, 6, 6, 6);
            this._listSteps.Name = "_listSteps";
            this._listSteps.Size = new System.Drawing.Size(772, 909);
            this._listSteps.TabIndex = 0;
            // 
            // _stepsButtonsPanel
            // 
            this._stepsButtonsPanel.AutoSize = true;
            this._stepsButtonsPanel.Controls.Add(this._btnAddStep);
            this._stepsButtonsPanel.Controls.Add(this._btnRemoveStep);
            this._stepsButtonsPanel.Controls.Add(this._btnMoveUp);
            this._stepsButtonsPanel.Controls.Add(this._btnMoveDown);
            this._stepsButtonsPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this._stepsButtonsPanel.Location = new System.Drawing.Point(6, 927);
            this._stepsButtonsPanel.Margin = new System.Windows.Forms.Padding(6, 6, 6, 6);
            this._stepsButtonsPanel.Name = "_stepsButtonsPanel";
            this._stepsButtonsPanel.Size = new System.Drawing.Size(772, 56);
            this._stepsButtonsPanel.TabIndex = 1;
            this._stepsButtonsPanel.WrapContents = false;
            // 
            // _btnAddStep
            // 
            this._btnAddStep.Location = new System.Drawing.Point(6, 6);
            this._btnAddStep.Margin = new System.Windows.Forms.Padding(6, 6, 6, 6);
            this._btnAddStep.Name = "_btnAddStep";
            this._btnAddStep.Size = new System.Drawing.Size(150, 44);
            this._btnAddStep.TabIndex = 0;
            this._btnAddStep.Text = "Add";
            this._btnAddStep.UseVisualStyleBackColor = true;
            // 
            // _btnRemoveStep
            // 
            this._btnRemoveStep.Location = new System.Drawing.Point(168, 6);
            this._btnRemoveStep.Margin = new System.Windows.Forms.Padding(6, 6, 6, 6);
            this._btnRemoveStep.Name = "_btnRemoveStep";
            this._btnRemoveStep.Size = new System.Drawing.Size(150, 44);
            this._btnRemoveStep.TabIndex = 1;
            this._btnRemoveStep.Text = "Remove";
            this._btnRemoveStep.UseVisualStyleBackColor = true;
            // 
            // _btnMoveUp
            // 
            this._btnMoveUp.Location = new System.Drawing.Point(330, 6);
            this._btnMoveUp.Margin = new System.Windows.Forms.Padding(6, 6, 6, 6);
            this._btnMoveUp.Name = "_btnMoveUp";
            this._btnMoveUp.Size = new System.Drawing.Size(150, 44);
            this._btnMoveUp.TabIndex = 2;
            this._btnMoveUp.Text = "Move Up";
            this._btnMoveUp.UseVisualStyleBackColor = true;
            // 
            // _btnMoveDown
            // 
            this._btnMoveDown.Location = new System.Drawing.Point(492, 6);
            this._btnMoveDown.Margin = new System.Windows.Forms.Padding(6, 6, 6, 6);
            this._btnMoveDown.Name = "_btnMoveDown";
            this._btnMoveDown.Size = new System.Drawing.Size(170, 44);
            this._btnMoveDown.TabIndex = 3;
            this._btnMoveDown.Text = "Move Down";
            this._btnMoveDown.UseVisualStyleBackColor = true;
            // 
            // _grpStepDetails
            // 
            this._grpStepDetails.Controls.Add(this._detailsLayout);
            this._grpStepDetails.Dock = System.Windows.Forms.DockStyle.Fill;
            this._grpStepDetails.Location = new System.Drawing.Point(834, 6);
            this._grpStepDetails.Margin = new System.Windows.Forms.Padding(6, 6, 6, 6);
            this._grpStepDetails.Name = "_grpStepDetails";
            this._grpStepDetails.Padding = new System.Windows.Forms.Padding(16, 15, 16, 15);
            this._grpStepDetails.Size = new System.Drawing.Size(1596, 1043);
            this._grpStepDetails.TabIndex = 1;
            this._grpStepDetails.TabStop = false;
            this._grpStepDetails.Text = "Step Details";
            // 
            // _detailsLayout
            // 
            this._detailsLayout.ColumnCount = 2;
            this._detailsLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 180F));
            this._detailsLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this._detailsLayout.Controls.Add(this._lblPlugin, 0, 0);
            this._detailsLayout.Controls.Add(this._cmbPlugin, 1, 0);
            this._detailsLayout.Controls.Add(this._lblCapability, 0, 1);
            this._detailsLayout.Controls.Add(this._cmbCapability, 1, 1);
            this._detailsLayout.Controls.Add(this._lblSummary, 0, 2);
            this._detailsLayout.Controls.Add(this._txtCapabilitySummary, 1, 2);
            this._detailsLayout.Controls.Add(this._lblParameters, 0, 3);
            this._detailsLayout.Controls.Add(this._tabDetails, 1, 3);
            this._detailsLayout.Controls.Add(this._lblValidation, 0, 4);
            this._detailsLayout.Controls.Add(this._txtValidationSummary, 1, 4);
            this._detailsLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this._detailsLayout.Location = new System.Drawing.Point(16, 39);
            this._detailsLayout.Margin = new System.Windows.Forms.Padding(6, 6, 6, 6);
            this._detailsLayout.Name = "_detailsLayout";
            this._detailsLayout.RowCount = 5;
            this._detailsLayout.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this._detailsLayout.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this._detailsLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 162F));
            this._detailsLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this._detailsLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 177F));
            this._detailsLayout.Size = new System.Drawing.Size(1564, 989);
            this._detailsLayout.TabIndex = 0;
            // 
            // _lblPlugin
            // 
            this._lblPlugin.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this._lblPlugin.AutoSize = true;
            this._lblPlugin.Location = new System.Drawing.Point(6, 12);
            this._lblPlugin.Margin = new System.Windows.Forms.Padding(6, 12, 6, 12);
            this._lblPlugin.Name = "_lblPlugin";
            this._lblPlugin.Size = new System.Drawing.Size(72, 25);
            this._lblPlugin.TabIndex = 0;
            this._lblPlugin.Text = "Plugin";
            // 
            // _cmbPlugin
            // 
            this._cmbPlugin.Dock = System.Windows.Forms.DockStyle.Fill;
            this._cmbPlugin.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this._cmbPlugin.FormattingEnabled = true;
            this._cmbPlugin.Location = new System.Drawing.Point(186, 6);
            this._cmbPlugin.Margin = new System.Windows.Forms.Padding(6, 6, 6, 6);
            this._cmbPlugin.Name = "_cmbPlugin";
            this._cmbPlugin.Size = new System.Drawing.Size(1372, 33);
            this._cmbPlugin.TabIndex = 1;
            // 
            // _lblCapability
            // 
            this._lblCapability.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this._lblCapability.AutoSize = true;
            this._lblCapability.Location = new System.Drawing.Point(6, 61);
            this._lblCapability.Margin = new System.Windows.Forms.Padding(6, 12, 6, 12);
            this._lblCapability.Name = "_lblCapability";
            this._lblCapability.Size = new System.Drawing.Size(107, 25);
            this._lblCapability.TabIndex = 2;
            this._lblCapability.Text = "Capability";
            // 
            // _cmbCapability
            // 
            this._cmbCapability.Dock = System.Windows.Forms.DockStyle.Fill;
            this._cmbCapability.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this._cmbCapability.FormattingEnabled = true;
            this._cmbCapability.Location = new System.Drawing.Point(186, 55);
            this._cmbCapability.Margin = new System.Windows.Forms.Padding(6, 6, 6, 6);
            this._cmbCapability.Name = "_cmbCapability";
            this._cmbCapability.Size = new System.Drawing.Size(1372, 33);
            this._cmbCapability.TabIndex = 3;
            // 
            // _lblSummary
            // 
            this._lblSummary.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this._lblSummary.AutoSize = true;
            this._lblSummary.Location = new System.Drawing.Point(6, 166);
            this._lblSummary.Margin = new System.Windows.Forms.Padding(6, 12, 6, 12);
            this._lblSummary.Name = "_lblSummary";
            this._lblSummary.Size = new System.Drawing.Size(102, 25);
            this._lblSummary.TabIndex = 4;
            this._lblSummary.Text = "Summary";
            // 
            // _txtCapabilitySummary
            // 
            this._txtCapabilitySummary.Dock = System.Windows.Forms.DockStyle.Fill;
            this._txtCapabilitySummary.Location = new System.Drawing.Point(186, 104);
            this._txtCapabilitySummary.Margin = new System.Windows.Forms.Padding(6, 6, 6, 6);
            this._txtCapabilitySummary.Multiline = true;
            this._txtCapabilitySummary.Name = "_txtCapabilitySummary";
            this._txtCapabilitySummary.ReadOnly = true;
            this._txtCapabilitySummary.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this._txtCapabilitySummary.Size = new System.Drawing.Size(1372, 150);
            this._txtCapabilitySummary.TabIndex = 5;
            // 
            // _lblParameters
            // 
            this._lblParameters.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this._lblParameters.AutoSize = true;
            this._lblParameters.Location = new System.Drawing.Point(6, 523);
            this._lblParameters.Margin = new System.Windows.Forms.Padding(6, 12, 6, 12);
            this._lblParameters.Name = "_lblParameters";
            this._lblParameters.Size = new System.Drawing.Size(122, 25);
            this._lblParameters.TabIndex = 6;
            this._lblParameters.Text = "Parameters";
            // 
            // _tabDetails
            // 
            this._tabDetails.Controls.Add(this._tabStep);
            this._tabDetails.Controls.Add(this._tabEncoder);
            this._tabDetails.Controls.Add(this._tabMetric);
            this._tabDetails.Controls.Add(this._tabOutput);
            this._tabDetails.Dock = System.Windows.Forms.DockStyle.Fill;
            this._tabDetails.Location = new System.Drawing.Point(186, 266);
            this._tabDetails.Margin = new System.Windows.Forms.Padding(6, 6, 6, 6);
            this._tabDetails.Name = "_tabDetails";
            this._tabDetails.SelectedIndex = 0;
            this._tabDetails.Size = new System.Drawing.Size(1372, 540);
            this._tabDetails.TabIndex = 7;
            // 
            // _tabStep
            // 
            this._tabStep.Controls.Add(this._propertyGrid);
            this._tabStep.Location = new System.Drawing.Point(8, 39);
            this._tabStep.Margin = new System.Windows.Forms.Padding(6, 6, 6, 6);
            this._tabStep.Name = "_tabStep";
            this._tabStep.Padding = new System.Windows.Forms.Padding(6, 6, 6, 6);
            this._tabStep.Size = new System.Drawing.Size(1356, 493);
            this._tabStep.TabIndex = 0;
            this._tabStep.Text = "Step";
            this._tabStep.UseVisualStyleBackColor = true;
            // 
            // _propertyGrid
            // 
            this._propertyGrid.Dock = System.Windows.Forms.DockStyle.Fill;
            this._propertyGrid.HelpVisible = false;
            this._propertyGrid.Location = new System.Drawing.Point(6, 6);
            this._propertyGrid.Margin = new System.Windows.Forms.Padding(6, 6, 6, 6);
            this._propertyGrid.Name = "_propertyGrid";
            this._propertyGrid.PropertySort = System.Windows.Forms.PropertySort.NoSort;
            this._propertyGrid.Size = new System.Drawing.Size(1344, 481);
            this._propertyGrid.TabIndex = 0;
            this._propertyGrid.ToolbarVisible = false;
            // 
            // _tabEncoder
            // 
            this._tabEncoder.Controls.Add(this._encoderLayout);
            this._tabEncoder.Location = new System.Drawing.Point(8, 39);
            this._tabEncoder.Margin = new System.Windows.Forms.Padding(6, 6, 6, 6);
            this._tabEncoder.Name = "_tabEncoder";
            this._tabEncoder.Padding = new System.Windows.Forms.Padding(6, 6, 6, 6);
            this._tabEncoder.Size = new System.Drawing.Size(1356, 255);
            this._tabEncoder.TabIndex = 1;
            this._tabEncoder.Text = "Encoder Child";
            this._tabEncoder.UseVisualStyleBackColor = true;
            // 
            // _encoderLayout
            // 
            this._encoderLayout.ColumnCount = 2;
            this._encoderLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 180F));
            this._encoderLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this._encoderLayout.Controls.Add(this._lblEncoderPlugin, 0, 0);
            this._encoderLayout.Controls.Add(this._cmbEncoderPlugin, 1, 0);
            this._encoderLayout.Controls.Add(this._lblEncoderCapability, 0, 1);
            this._encoderLayout.Controls.Add(this._cmbEncoderCapability, 1, 1);
            this._encoderLayout.Controls.Add(this._lblEncoderSummary, 0, 2);
            this._encoderLayout.Controls.Add(this._txtEncoderSummary, 1, 2);
            this._encoderLayout.Controls.Add(this._lblEncoderParameters, 0, 3);
            this._encoderLayout.Controls.Add(this._propertyGridEncoder, 1, 3);
            this._encoderLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this._encoderLayout.Location = new System.Drawing.Point(6, 6);
            this._encoderLayout.Margin = new System.Windows.Forms.Padding(6, 6, 6, 6);
            this._encoderLayout.Name = "_encoderLayout";
            this._encoderLayout.RowCount = 4;
            this._encoderLayout.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this._encoderLayout.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this._encoderLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 162F));
            this._encoderLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this._encoderLayout.Size = new System.Drawing.Size(1344, 243);
            this._encoderLayout.TabIndex = 0;
            // 
            // _lblEncoderPlugin
            // 
            this._lblEncoderPlugin.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this._lblEncoderPlugin.AutoSize = true;
            this._lblEncoderPlugin.Location = new System.Drawing.Point(6, 12);
            this._lblEncoderPlugin.Margin = new System.Windows.Forms.Padding(6, 12, 6, 12);
            this._lblEncoderPlugin.Name = "_lblEncoderPlugin";
            this._lblEncoderPlugin.Size = new System.Drawing.Size(72, 25);
            this._lblEncoderPlugin.TabIndex = 0;
            this._lblEncoderPlugin.Text = "Plugin";
            // 
            // _cmbEncoderPlugin
            // 
            this._cmbEncoderPlugin.Dock = System.Windows.Forms.DockStyle.Fill;
            this._cmbEncoderPlugin.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this._cmbEncoderPlugin.FormattingEnabled = true;
            this._cmbEncoderPlugin.Location = new System.Drawing.Point(186, 6);
            this._cmbEncoderPlugin.Margin = new System.Windows.Forms.Padding(6, 6, 6, 6);
            this._cmbEncoderPlugin.Name = "_cmbEncoderPlugin";
            this._cmbEncoderPlugin.Size = new System.Drawing.Size(1152, 33);
            this._cmbEncoderPlugin.TabIndex = 1;
            // 
            // _lblEncoderCapability
            // 
            this._lblEncoderCapability.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this._lblEncoderCapability.AutoSize = true;
            this._lblEncoderCapability.Location = new System.Drawing.Point(6, 61);
            this._lblEncoderCapability.Margin = new System.Windows.Forms.Padding(6, 12, 6, 12);
            this._lblEncoderCapability.Name = "_lblEncoderCapability";
            this._lblEncoderCapability.Size = new System.Drawing.Size(107, 25);
            this._lblEncoderCapability.TabIndex = 2;
            this._lblEncoderCapability.Text = "Capability";
            // 
            // _cmbEncoderCapability
            // 
            this._cmbEncoderCapability.Dock = System.Windows.Forms.DockStyle.Fill;
            this._cmbEncoderCapability.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this._cmbEncoderCapability.FormattingEnabled = true;
            this._cmbEncoderCapability.Location = new System.Drawing.Point(186, 55);
            this._cmbEncoderCapability.Margin = new System.Windows.Forms.Padding(6, 6, 6, 6);
            this._cmbEncoderCapability.Name = "_cmbEncoderCapability";
            this._cmbEncoderCapability.Size = new System.Drawing.Size(1152, 33);
            this._cmbEncoderCapability.TabIndex = 3;
            // 
            // _lblEncoderSummary
            // 
            this._lblEncoderSummary.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this._lblEncoderSummary.AutoSize = true;
            this._lblEncoderSummary.Location = new System.Drawing.Point(6, 166);
            this._lblEncoderSummary.Margin = new System.Windows.Forms.Padding(6, 12, 6, 12);
            this._lblEncoderSummary.Name = "_lblEncoderSummary";
            this._lblEncoderSummary.Size = new System.Drawing.Size(102, 25);
            this._lblEncoderSummary.TabIndex = 4;
            this._lblEncoderSummary.Text = "Summary";
            // 
            // _txtEncoderSummary
            // 
            this._txtEncoderSummary.Dock = System.Windows.Forms.DockStyle.Fill;
            this._txtEncoderSummary.Location = new System.Drawing.Point(186, 104);
            this._txtEncoderSummary.Margin = new System.Windows.Forms.Padding(6, 6, 6, 6);
            this._txtEncoderSummary.Multiline = true;
            this._txtEncoderSummary.Name = "_txtEncoderSummary";
            this._txtEncoderSummary.ReadOnly = true;
            this._txtEncoderSummary.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this._txtEncoderSummary.Size = new System.Drawing.Size(1152, 150);
            this._txtEncoderSummary.TabIndex = 5;
            // 
            // _lblEncoderParameters
            // 
            this._lblEncoderParameters.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this._lblEncoderParameters.AutoSize = true;
            this._lblEncoderParameters.Location = new System.Drawing.Point(6, 272);
            this._lblEncoderParameters.Margin = new System.Windows.Forms.Padding(6, 12, 6, 12);
            this._lblEncoderParameters.Name = "_lblEncoderParameters";
            this._lblEncoderParameters.Size = new System.Drawing.Size(122, 1);
            this._lblEncoderParameters.TabIndex = 6;
            this._lblEncoderParameters.Text = "Parameters";
            // 
            // _propertyGridEncoder
            // 
            this._propertyGridEncoder.Dock = System.Windows.Forms.DockStyle.Fill;
            this._propertyGridEncoder.HelpVisible = false;
            this._propertyGridEncoder.Location = new System.Drawing.Point(186, 266);
            this._propertyGridEncoder.Margin = new System.Windows.Forms.Padding(6, 6, 6, 6);
            this._propertyGridEncoder.Name = "_propertyGridEncoder";
            this._propertyGridEncoder.PropertySort = System.Windows.Forms.PropertySort.NoSort;
            this._propertyGridEncoder.Size = new System.Drawing.Size(1152, 1);
            this._propertyGridEncoder.TabIndex = 7;
            this._propertyGridEncoder.ToolbarVisible = false;
            // 
            // _tabMetric
            // 
            this._tabMetric.Controls.Add(this._metricLayout);
            this._tabMetric.Location = new System.Drawing.Point(8, 39);
            this._tabMetric.Margin = new System.Windows.Forms.Padding(6, 6, 6, 6);
            this._tabMetric.Name = "_tabMetric";
            this._tabMetric.Padding = new System.Windows.Forms.Padding(6, 6, 6, 6);
            this._tabMetric.Size = new System.Drawing.Size(1356, 255);
            this._tabMetric.TabIndex = 2;
            this._tabMetric.Text = "Metric Child";
            this._tabMetric.UseVisualStyleBackColor = true;
            // 
            // _metricLayout
            // 
            this._metricLayout.ColumnCount = 2;
            this._metricLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 180F));
            this._metricLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this._metricLayout.Controls.Add(this._lblMetricPlugin, 0, 0);
            this._metricLayout.Controls.Add(this._cmbMetricPlugin, 1, 0);
            this._metricLayout.Controls.Add(this._lblMetricCapability, 0, 1);
            this._metricLayout.Controls.Add(this._cmbMetricCapability, 1, 1);
            this._metricLayout.Controls.Add(this._lblMetricSummary, 0, 2);
            this._metricLayout.Controls.Add(this._txtMetricSummary, 1, 2);
            this._metricLayout.Controls.Add(this._lblMetricParameters, 0, 3);
            this._metricLayout.Controls.Add(this._propertyGridMetric, 1, 3);
            this._metricLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this._metricLayout.Location = new System.Drawing.Point(6, 6);
            this._metricLayout.Margin = new System.Windows.Forms.Padding(6, 6, 6, 6);
            this._metricLayout.Name = "_metricLayout";
            this._metricLayout.RowCount = 4;
            this._metricLayout.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this._metricLayout.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this._metricLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 162F));
            this._metricLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this._metricLayout.Size = new System.Drawing.Size(1344, 243);
            this._metricLayout.TabIndex = 0;
            // 
            // _lblMetricPlugin
            // 
            this._lblMetricPlugin.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this._lblMetricPlugin.AutoSize = true;
            this._lblMetricPlugin.Location = new System.Drawing.Point(6, 12);
            this._lblMetricPlugin.Margin = new System.Windows.Forms.Padding(6, 12, 6, 12);
            this._lblMetricPlugin.Name = "_lblMetricPlugin";
            this._lblMetricPlugin.Size = new System.Drawing.Size(72, 25);
            this._lblMetricPlugin.TabIndex = 0;
            this._lblMetricPlugin.Text = "Plugin";
            // 
            // _cmbMetricPlugin
            // 
            this._cmbMetricPlugin.Dock = System.Windows.Forms.DockStyle.Fill;
            this._cmbMetricPlugin.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this._cmbMetricPlugin.FormattingEnabled = true;
            this._cmbMetricPlugin.Location = new System.Drawing.Point(186, 6);
            this._cmbMetricPlugin.Margin = new System.Windows.Forms.Padding(6, 6, 6, 6);
            this._cmbMetricPlugin.Name = "_cmbMetricPlugin";
            this._cmbMetricPlugin.Size = new System.Drawing.Size(1152, 33);
            this._cmbMetricPlugin.TabIndex = 1;
            // 
            // _lblMetricCapability
            // 
            this._lblMetricCapability.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this._lblMetricCapability.AutoSize = true;
            this._lblMetricCapability.Location = new System.Drawing.Point(6, 61);
            this._lblMetricCapability.Margin = new System.Windows.Forms.Padding(6, 12, 6, 12);
            this._lblMetricCapability.Name = "_lblMetricCapability";
            this._lblMetricCapability.Size = new System.Drawing.Size(107, 25);
            this._lblMetricCapability.TabIndex = 2;
            this._lblMetricCapability.Text = "Capability";
            // 
            // _cmbMetricCapability
            // 
            this._cmbMetricCapability.Dock = System.Windows.Forms.DockStyle.Fill;
            this._cmbMetricCapability.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this._cmbMetricCapability.FormattingEnabled = true;
            this._cmbMetricCapability.Location = new System.Drawing.Point(186, 55);
            this._cmbMetricCapability.Margin = new System.Windows.Forms.Padding(6, 6, 6, 6);
            this._cmbMetricCapability.Name = "_cmbMetricCapability";
            this._cmbMetricCapability.Size = new System.Drawing.Size(1152, 33);
            this._cmbMetricCapability.TabIndex = 3;
            // 
            // _lblMetricSummary
            // 
            this._lblMetricSummary.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this._lblMetricSummary.AutoSize = true;
            this._lblMetricSummary.Location = new System.Drawing.Point(6, 166);
            this._lblMetricSummary.Margin = new System.Windows.Forms.Padding(6, 12, 6, 12);
            this._lblMetricSummary.Name = "_lblMetricSummary";
            this._lblMetricSummary.Size = new System.Drawing.Size(102, 25);
            this._lblMetricSummary.TabIndex = 4;
            this._lblMetricSummary.Text = "Summary";
            // 
            // _txtMetricSummary
            // 
            this._txtMetricSummary.Dock = System.Windows.Forms.DockStyle.Fill;
            this._txtMetricSummary.Location = new System.Drawing.Point(186, 104);
            this._txtMetricSummary.Margin = new System.Windows.Forms.Padding(6, 6, 6, 6);
            this._txtMetricSummary.Multiline = true;
            this._txtMetricSummary.Name = "_txtMetricSummary";
            this._txtMetricSummary.ReadOnly = true;
            this._txtMetricSummary.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this._txtMetricSummary.Size = new System.Drawing.Size(1152, 150);
            this._txtMetricSummary.TabIndex = 5;
            // 
            // _lblMetricParameters
            // 
            this._lblMetricParameters.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this._lblMetricParameters.AutoSize = true;
            this._lblMetricParameters.Location = new System.Drawing.Point(6, 272);
            this._lblMetricParameters.Margin = new System.Windows.Forms.Padding(6, 12, 6, 12);
            this._lblMetricParameters.Name = "_lblMetricParameters";
            this._lblMetricParameters.Size = new System.Drawing.Size(122, 1);
            this._lblMetricParameters.TabIndex = 6;
            this._lblMetricParameters.Text = "Parameters";
            // 
            // _propertyGridMetric
            // 
            this._propertyGridMetric.Dock = System.Windows.Forms.DockStyle.Fill;
            this._propertyGridMetric.HelpVisible = false;
            this._propertyGridMetric.Location = new System.Drawing.Point(186, 266);
            this._propertyGridMetric.Margin = new System.Windows.Forms.Padding(6, 6, 6, 6);
            this._propertyGridMetric.Name = "_propertyGridMetric";
            this._propertyGridMetric.PropertySort = System.Windows.Forms.PropertySort.NoSort;
            this._propertyGridMetric.Size = new System.Drawing.Size(1152, 1);
            this._propertyGridMetric.TabIndex = 7;
            this._propertyGridMetric.ToolbarVisible = false;
            // 
            // _tabOutput
            // 
            this._tabOutput.Controls.Add(this._propertyGridOutput);
            this._tabOutput.Location = new System.Drawing.Point(8, 39);
            this._tabOutput.Margin = new System.Windows.Forms.Padding(6, 6, 6, 6);
            this._tabOutput.Name = "_tabOutput";
            this._tabOutput.Padding = new System.Windows.Forms.Padding(6, 6, 6, 6);
            this._tabOutput.Size = new System.Drawing.Size(1356, 255);
            this._tabOutput.TabIndex = 3;
            this._tabOutput.Text = "Output Policy";
            this._tabOutput.UseVisualStyleBackColor = true;
            // 
            // _propertyGridOutput
            // 
            this._propertyGridOutput.Dock = System.Windows.Forms.DockStyle.Fill;
            this._propertyGridOutput.Location = new System.Drawing.Point(6, 6);
            this._propertyGridOutput.Margin = new System.Windows.Forms.Padding(6, 6, 6, 6);
            this._propertyGridOutput.Name = "_propertyGridOutput";
            this._propertyGridOutput.PropertySort = System.Windows.Forms.PropertySort.Categorized;
            this._propertyGridOutput.Size = new System.Drawing.Size(1344, 243);
            this._propertyGridOutput.TabIndex = 0;
            // 
            // _lblValidation
            // 
            this._lblValidation.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this._lblValidation.AutoSize = true;
            this._lblValidation.Location = new System.Drawing.Point(6, 888);
            this._lblValidation.Margin = new System.Windows.Forms.Padding(6, 12, 6, 12);
            this._lblValidation.Name = "_lblValidation";
            this._lblValidation.Size = new System.Drawing.Size(107, 25);
            this._lblValidation.TabIndex = 8;
            this._lblValidation.Text = "Validation";
            // 
            // _txtValidationSummary
            // 
            this._txtValidationSummary.Dock = System.Windows.Forms.DockStyle.Fill;
            this._txtValidationSummary.Location = new System.Drawing.Point(186, 818);
            this._txtValidationSummary.Margin = new System.Windows.Forms.Padding(6, 6, 6, 6);
            this._txtValidationSummary.Multiline = true;
            this._txtValidationSummary.Name = "_txtValidationSummary";
            this._txtValidationSummary.ReadOnly = true;
            this._txtValidationSummary.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this._txtValidationSummary.Size = new System.Drawing.Size(1372, 165);
            this._txtValidationSummary.TabIndex = 9;
            // 
            // _bottomButtonsLayout
            // 
            this._bottomButtonsLayout.AutoSize = true;
            this._bottomButtonsLayout.ColumnCount = 2;
            this._bottomButtonsLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this._bottomButtonsLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this._bottomButtonsLayout.Controls.Add(this._presetButtonsPanel, 0, 0);
            this._bottomButtonsLayout.Controls.Add(this._dialogButtonsPanel, 1, 0);
            this._bottomButtonsLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this._bottomButtonsLayout.Location = new System.Drawing.Point(22, 1363);
            this._bottomButtonsLayout.Margin = new System.Windows.Forms.Padding(6, 6, 6, 6);
            this._bottomButtonsLayout.Name = "_bottomButtonsLayout";
            this._bottomButtonsLayout.RowCount = 1;
            this._bottomButtonsLayout.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this._bottomButtonsLayout.Size = new System.Drawing.Size(2436, 78);
            this._bottomButtonsLayout.TabIndex = 2;
            // 
            // _presetButtonsPanel
            // 
            this._presetButtonsPanel.AutoSize = true;
            this._presetButtonsPanel.Controls.Add(this._btnNew);
            this._presetButtonsPanel.Controls.Add(this._btnDuplicate);
            this._presetButtonsPanel.Controls.Add(this._btnReload);
            this._presetButtonsPanel.Controls.Add(this._btnSave);
            this._presetButtonsPanel.Controls.Add(this._btnSaveAs);
            this._presetButtonsPanel.Controls.Add(this._btnDelete);
            this._presetButtonsPanel.Controls.Add(this._btnOpenLocation);
            this._presetButtonsPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this._presetButtonsPanel.Location = new System.Drawing.Point(6, 6);
            this._presetButtonsPanel.Margin = new System.Windows.Forms.Padding(6, 6, 6, 6);
            this._presetButtonsPanel.Name = "_presetButtonsPanel";
            this._presetButtonsPanel.Size = new System.Drawing.Size(2052, 66);
            this._presetButtonsPanel.TabIndex = 0;
            this._presetButtonsPanel.WrapContents = false;
            // 
            // _btnNew
            // 
            this._btnNew.Location = new System.Drawing.Point(6, 6);
            this._btnNew.Margin = new System.Windows.Forms.Padding(6, 6, 6, 6);
            this._btnNew.Name = "_btnNew";
            this._btnNew.Size = new System.Drawing.Size(176, 54);
            this._btnNew.TabIndex = 0;
            this._btnNew.Text = "New";
            this._btnNew.UseVisualStyleBackColor = true;
            // 
            // _btnDuplicate
            // 
            this._btnDuplicate.Location = new System.Drawing.Point(194, 6);
            this._btnDuplicate.Margin = new System.Windows.Forms.Padding(6, 6, 6, 6);
            this._btnDuplicate.Name = "_btnDuplicate";
            this._btnDuplicate.Size = new System.Drawing.Size(176, 54);
            this._btnDuplicate.TabIndex = 1;
            this._btnDuplicate.Text = "Duplicate";
            this._btnDuplicate.UseVisualStyleBackColor = true;
            // 
            // _btnReload
            // 
            this._btnReload.Location = new System.Drawing.Point(382, 6);
            this._btnReload.Margin = new System.Windows.Forms.Padding(6, 6, 6, 6);
            this._btnReload.Name = "_btnReload";
            this._btnReload.Size = new System.Drawing.Size(176, 54);
            this._btnReload.TabIndex = 2;
            this._btnReload.Text = "Reload";
            this._btnReload.UseVisualStyleBackColor = true;
            // 
            // _btnSave
            // 
            this._btnSave.Location = new System.Drawing.Point(570, 6);
            this._btnSave.Margin = new System.Windows.Forms.Padding(6, 6, 6, 6);
            this._btnSave.Name = "_btnSave";
            this._btnSave.Size = new System.Drawing.Size(176, 54);
            this._btnSave.TabIndex = 3;
            this._btnSave.Text = "Save";
            this._btnSave.UseVisualStyleBackColor = true;
            // 
            // _btnSaveAs
            // 
            this._btnSaveAs.Location = new System.Drawing.Point(758, 6);
            this._btnSaveAs.Margin = new System.Windows.Forms.Padding(6, 6, 6, 6);
            this._btnSaveAs.Name = "_btnSaveAs";
            this._btnSaveAs.Size = new System.Drawing.Size(176, 54);
            this._btnSaveAs.TabIndex = 4;
            this._btnSaveAs.Text = "Save As...";
            this._btnSaveAs.UseVisualStyleBackColor = true;
            // 
            // _btnDelete
            // 
            this._btnDelete.Location = new System.Drawing.Point(946, 6);
            this._btnDelete.Margin = new System.Windows.Forms.Padding(6, 6, 6, 6);
            this._btnDelete.Name = "_btnDelete";
            this._btnDelete.Size = new System.Drawing.Size(176, 54);
            this._btnDelete.TabIndex = 5;
            this._btnDelete.Text = "Delete";
            this._btnDelete.UseVisualStyleBackColor = true;
            // 
            // _btnOpenLocation
            // 
            this._btnOpenLocation.Location = new System.Drawing.Point(1134, 6);
            this._btnOpenLocation.Margin = new System.Windows.Forms.Padding(6, 6, 6, 6);
            this._btnOpenLocation.Name = "_btnOpenLocation";
            this._btnOpenLocation.Size = new System.Drawing.Size(220, 54);
            this._btnOpenLocation.TabIndex = 6;
            this._btnOpenLocation.Text = "Open File Location";
            this._btnOpenLocation.UseVisualStyleBackColor = true;
            // 
            // _dialogButtonsPanel
            // 
            this._dialogButtonsPanel.AutoSize = true;
            this._dialogButtonsPanel.Controls.Add(this._btnOk);
            this._dialogButtonsPanel.Controls.Add(this._btnCancel);
            this._dialogButtonsPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this._dialogButtonsPanel.Location = new System.Drawing.Point(2070, 6);
            this._dialogButtonsPanel.Margin = new System.Windows.Forms.Padding(6, 6, 6, 6);
            this._dialogButtonsPanel.Name = "_dialogButtonsPanel";
            this._dialogButtonsPanel.Size = new System.Drawing.Size(360, 66);
            this._dialogButtonsPanel.TabIndex = 1;
            this._dialogButtonsPanel.WrapContents = false;
            // 
            // _btnOk
            // 
            this._btnOk.Location = new System.Drawing.Point(6, 6);
            this._btnOk.Margin = new System.Windows.Forms.Padding(6, 6, 6, 6);
            this._btnOk.Name = "_btnOk";
            this._btnOk.Size = new System.Drawing.Size(168, 54);
            this._btnOk.TabIndex = 0;
            this._btnOk.Text = "OK";
            this._btnOk.UseVisualStyleBackColor = true;
            // 
            // _btnCancel
            // 
            this._btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this._btnCancel.Location = new System.Drawing.Point(186, 6);
            this._btnCancel.Margin = new System.Windows.Forms.Padding(6, 6, 6, 6);
            this._btnCancel.Name = "_btnCancel";
            this._btnCancel.Size = new System.Drawing.Size(168, 54);
            this._btnCancel.TabIndex = 1;
            this._btnCancel.CausesValidation = false;
            this._btnCancel.Text = "Cancel";
            this._btnCancel.UseVisualStyleBackColor = true;
            // 
            // PresetEditorForm
            // 
            this.AcceptButton = this._btnOk;
            this.AutoScaleDimensions = new System.Drawing.SizeF(12F, 25F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this._btnCancel;
            this.ClientSize = new System.Drawing.Size(2480, 1462);
            this.Controls.Add(this._rootLayout);
            this.Margin = new System.Windows.Forms.Padding(6, 6, 6, 6);
            this.MinimumSize = new System.Drawing.Size(1934, 1127);
            this.Name = "PresetEditorForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Preset Editor";
            this._rootLayout.ResumeLayout(false);
            this._rootLayout.PerformLayout();
            this._metaLayout.ResumeLayout(false);
            this._metaLayout.PerformLayout();
            this._contentLayout.ResumeLayout(false);
            this._grpSteps.ResumeLayout(false);
            this._stepsLayout.ResumeLayout(false);
            this._stepsLayout.PerformLayout();
            this._stepsButtonsPanel.ResumeLayout(false);
            this._grpStepDetails.ResumeLayout(false);
            this._detailsLayout.ResumeLayout(false);
            this._detailsLayout.PerformLayout();
            this._tabDetails.ResumeLayout(false);
            this._tabStep.ResumeLayout(false);
            this._tabEncoder.ResumeLayout(false);
            this._encoderLayout.ResumeLayout(false);
            this._encoderLayout.PerformLayout();
            this._tabMetric.ResumeLayout(false);
            this._metricLayout.ResumeLayout(false);
            this._metricLayout.PerformLayout();
            this._tabOutput.ResumeLayout(false);
            this._bottomButtonsLayout.ResumeLayout(false);
            this._bottomButtonsLayout.PerformLayout();
            this._presetButtonsPanel.ResumeLayout(false);
            this._dialogButtonsPanel.ResumeLayout(false);
            this.ResumeLayout(false);

        }
    }
}
