using System;
using System.Collections.Generic;
using System.Windows.Forms;
using FlawlessPictury.AppCore.Presets;

namespace FlawlessPictury.Presentation.WinForms.Views
{
    public sealed partial class PresetEditorForm : Form, IPresetEditorView
    {
        private bool _suppressEvents;
        private bool _allowClose;
        private bool _handlingCloseRequest;

        public PresetEditorForm()
        {
            InitializeComponent();
            SimplifyStepDetailsSurface();
            ConfigureDialogButtons();
            WireEvents();
            SetOptimizerChildEditorsVisible(false);
        }

        public event EventHandler ViewReady;
        public event EventHandler SelectedPresetChanged;
        public event EventHandler NewPresetRequested;
        public event EventHandler DuplicatePresetRequested;
        public event EventHandler ReloadRequested;
        public event EventHandler SaveRequested;
        public event EventHandler SaveAsRequested;
        public event EventHandler DeleteRequested;
        public event EventHandler OpenPresetLocationRequested;
        public event EventHandler AddStepRequested;
        public event EventHandler RemoveStepRequested;
        public event EventHandler MoveStepUpRequested;
        public event EventHandler MoveStepDownRequested;
        public event EventHandler SelectedStructureChanged;
        public event EventHandler SelectedPluginChanged;
        public event EventHandler SelectedCapabilityChanged;
        public event EventHandler MetadataChanged;
        public event EventHandler ParameterValuesChanged;
        public event EventHandler EncoderPluginChanged;
        public event EventHandler EncoderCapabilityChanged;
        public event EventHandler EncoderParameterValuesChanged;
        public event EventHandler MetricPluginChanged;
        public event EventHandler MetricCapabilityChanged;
        public event EventHandler MetricParameterValuesChanged;
        public event EventHandler OutputPolicyChanged;
        public event EventHandler OkRequested;
        public event EventHandler CancelRequested;

        private void SimplifyStepDetailsSurface()
        {
            if (_detailsLayout == null || _tabDetails == null || _propertyGrid == null)
            {
                return;
            }

            if (_propertyGrid.Parent == _detailsLayout)
            {
                _tabDetails.Visible = false;
                return;
            }

            if (_propertyGrid.Parent != null)
            {
                _propertyGrid.Parent.Controls.Remove(_propertyGrid);
            }

            _detailsLayout.Controls.Remove(_tabDetails);
            _tabDetails.Visible = false;
            _detailsLayout.Controls.Add(_propertyGrid, 1, 3);
            _propertyGrid.Dock = DockStyle.Fill;
            _propertyGrid.BringToFront();
        }

        private void ConfigureDialogButtons()
        {
            if (_btnCancel != null)
            {
                _btnCancel.DialogResult = DialogResult.None;
            }

            if (CancelButton == _btnCancel)
            {
                CancelButton = null;
            }
        }

        private void RequestCancelClose()
        {
            if (_allowClose)
            {
                return;
            }

            _handlingCloseRequest = true;
            try
            {
                CancelRequested?.Invoke(this, EventArgs.Empty);
            }
            finally
            {
                _handlingCloseRequest = false;
                if (!_allowClose)
                {
                    DialogResult = DialogResult.None;
                }
            }
        }

        public string PresetIdText
        {
            get { return _txtPresetId.Text; }
            set { SetControlTextSuppressed(_txtPresetId, value); }
        }

        public string DisplayNameText
        {
            get { return _txtDisplayName.Text; }
            set { SetControlTextSuppressed(_txtDisplayName, value); }
        }

        public string DescriptionText
        {
            get { return _txtDescription.Text; }
            set { SetControlTextSuppressed(_txtDescription, value); }
        }

        public bool IsReadOnlyChecked
        {
            get { return false; }
            set { }
        }

        public string SelectedPluginId => GetSelectedChoiceId(_cmbPlugin);

        public string SelectedCapabilityId => GetSelectedChoiceId(_cmbCapability);

        public string SelectedEncoderPluginId => GetSelectedChoiceId(_cmbEncoderPlugin);

        public string SelectedEncoderCapabilityId => GetSelectedChoiceId(_cmbEncoderCapability);

        public string SelectedMetricPluginId => GetSelectedChoiceId(_cmbMetricPlugin);

        public string SelectedMetricCapabilityId => GetSelectedChoiceId(_cmbMetricCapability);

        public int SelectedPresetIndex => _cmbPresets.SelectedIndex;

        public int SelectedStructureIndex => _listSteps.SelectedIndex;

        public void SetPresets(IReadOnlyList<PresetDefinition> presets)
        {
            _suppressEvents = true;
            try
            {
                _cmbPresets.BeginUpdate();
                _cmbPresets.SelectedIndex = -1;
                _cmbPresets.Items.Clear();
                if (presets != null)
                {
                    foreach (var preset in presets)
                    {
                        _cmbPresets.Items.Add(preset);
                    }
                }
            }
            finally
            {
                _cmbPresets.EndUpdate();
                _suppressEvents = false;
            }

            _cmbPresets.Refresh();
        }

        public void SetPresetSelection(int index)
        {
            _suppressEvents = true;
            try
            {
                ForceComboSelection(_cmbPresets, index);
            }
            finally
            {
                _suppressEvents = false;
            }
        }

        void IPresetEditorView.SetStructureItems(IReadOnlyList<PresetEditorStructureItem> items)
        {
            _suppressEvents = true;
            try
            {
                _listSteps.BeginUpdate();
                _listSteps.Items.Clear();
                if (items != null)
                {
                    foreach (var item in items)
                    {
                        _listSteps.Items.Add(item);
                    }
                }
            }
            finally
            {
                _listSteps.EndUpdate();
                _suppressEvents = false;
            }
        }

        public void SetStructureSelection(int index)
        {
            _suppressEvents = true;
            try
            {
                if (index < -1 || index >= _listSteps.Items.Count)
                {
                    index = -1;
                }

                if (_listSteps.SelectedIndex == index)
                {
                    _listSteps.SelectedIndex = -1;
                }

                _listSteps.SelectedIndex = index;
            }
            finally
            {
                _suppressEvents = false;
            }
        }

        void IPresetEditorView.SetPluginChoices(IReadOnlyList<PresetEditorChoiceItem> items, string selectedPluginId)
        {
            FillChoiceList(_cmbPlugin, items, selectedPluginId);
        }

        void IPresetEditorView.SetCapabilityChoices(IReadOnlyList<PresetEditorChoiceItem> items, string selectedCapabilityId)
        {
            FillChoiceList(_cmbCapability, items, selectedCapabilityId);
        }

        public void SetParameterEditor(object selectedObject)
        {
            _propertyGrid.SelectedObject = selectedObject;
        }

        public void SetCapabilitySummary(string text)
        {
            SetControlTextSuppressed(_txtCapabilitySummary, text);
        }

        public void SetValidationSummary(string text)
        {
            SetControlTextSuppressed(_txtValidationSummary, text);
        }

        public void SetActionState(bool canSave, bool canSaveAs, bool canDelete, bool canDuplicate, bool canOpenLocation, bool canAddStep, bool canModifySelectedStep, bool canEditSelectors, bool canEditProperties)
        {
            _btnSave.Enabled = canSave;
            _btnSaveAs.Enabled = canSaveAs;
            _btnDelete.Enabled = canDelete;
            _btnDuplicate.Enabled = canDuplicate;
            _btnOpenLocation.Enabled = canOpenLocation;
            _btnAddStep.Enabled = canAddStep;
            _btnRemoveStep.Enabled = canModifySelectedStep;
            _btnMoveUp.Enabled = canModifySelectedStep;
            _btnMoveDown.Enabled = canModifySelectedStep;
            _cmbPlugin.Enabled = canEditSelectors;
            _cmbCapability.Enabled = canEditSelectors;
            _propertyGrid.Enabled = canEditProperties;
            _txtPresetId.ReadOnly = !canAddStep;
            _txtDisplayName.ReadOnly = !canAddStep;
            _txtDescription.ReadOnly = !canAddStep;
            _btnOk.Enabled = canAddStep || canSave || canSaveAs || canDelete;
        }

        public void SetOptimizerChildEditorsVisible(bool visible)
        {
            _tabEncoder.Text = visible ? "Encoder Child" : "Encoder Child (not used)";
            _tabMetric.Text = visible ? "Metric Child" : "Metric Child (not used)";
            SetChildEditorEnabled(_cmbEncoderPlugin, _cmbEncoderCapability, _propertyGridEncoder, visible);
            SetChildEditorEnabled(_cmbMetricPlugin, _cmbMetricCapability, _propertyGridMetric, visible);
            if (!visible)
            {
                SetControlTextSuppressed(_txtEncoderSummary, "This step does not use an encoder child step.");
                SetControlTextSuppressed(_txtMetricSummary, "This step does not use a metric child step.");
                _propertyGridEncoder.SelectedObject = null;
                _propertyGridMetric.SelectedObject = null;
            }
        }

        void IPresetEditorView.SetEncoderPluginChoices(IReadOnlyList<PresetEditorChoiceItem> items, string selectedPluginId)
        {
            FillChoiceList(_cmbEncoderPlugin, items, selectedPluginId);
        }

        void IPresetEditorView.SetEncoderCapabilityChoices(IReadOnlyList<PresetEditorChoiceItem> items, string selectedCapabilityId)
        {
            FillChoiceList(_cmbEncoderCapability, items, selectedCapabilityId);
        }

        public void SetEncoderParameterEditor(object selectedObject)
        {
            _propertyGridEncoder.SelectedObject = selectedObject;
        }

        public void SetEncoderSummary(string text)
        {
            SetControlTextSuppressed(_txtEncoderSummary, text);
        }

        void IPresetEditorView.SetMetricPluginChoices(IReadOnlyList<PresetEditorChoiceItem> items, string selectedPluginId)
        {
            FillChoiceList(_cmbMetricPlugin, items, selectedPluginId);
        }

        void IPresetEditorView.SetMetricCapabilityChoices(IReadOnlyList<PresetEditorChoiceItem> items, string selectedCapabilityId)
        {
            FillChoiceList(_cmbMetricCapability, items, selectedCapabilityId);
        }

        public void SetMetricParameterEditor(object selectedObject)
        {
            _propertyGridMetric.SelectedObject = selectedObject;
        }

        public void SetMetricSummary(string text)
        {
            SetControlTextSuppressed(_txtMetricSummary, text);
        }

        public void SetOutputPolicyEditor(object selectedObject)
        {
            _propertyGridOutput.SelectedObject = selectedObject;
        }

        public void SelectStepTab()
        {
            _tabDetails.SelectedTab = _tabStep;
        }

        public void SelectEncoderTab()
        {
            _tabDetails.SelectedTab = _tabEncoder;
        }

        public void SelectMetricTab()
        {
            _tabDetails.SelectedTab = _tabMetric;
        }

        public void SelectOutputTab()
        {
            _tabDetails.SelectedTab = _tabOutput;
        }

        public string PromptSaveAsPath(string initialDirectory, string initialFileName)
        {
            using (var dialog = new SaveFileDialog())
            {
                dialog.Filter = "Preset JSON (*.json)|*.json|All files (*.*)|*.*";
                dialog.DefaultExt = "json";
                dialog.AddExtension = true;
                dialog.InitialDirectory = string.IsNullOrWhiteSpace(initialDirectory) ? AppDomain.CurrentDomain.BaseDirectory : initialDirectory;
                dialog.FileName = string.IsNullOrWhiteSpace(initialFileName) ? "preset.json" : initialFileName;
                return dialog.ShowDialog(this) == DialogResult.OK ? dialog.FileName : null;
            }
        }

        public DialogResult ConfirmKeepSessionChanges(string presetName)
        {
            var name = string.IsNullOrWhiteSpace(presetName) ? "current preset" : presetName;
            return MessageBox.Show(this, "Keep unsaved session changes for " + name + " in the workspace before closing?", "Preset Editor", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);
        }

        public bool ConfirmDelete(string presetName)
        {
            var name = string.IsNullOrWhiteSpace(presetName) ? "this preset" : presetName;
            return MessageBox.Show(this, "Delete " + name + "?", "Preset Editor", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes;
        }

        public void ShowMessage(string message)
        {
            MessageBox.Show(this, message ?? string.Empty, "Preset Editor", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        public void ShowError(string message)
        {
            MessageBox.Show(this, message ?? string.Empty, "Preset Editor", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        public DialogResult ShowDialogView(object owner)
        {
            var win = owner as IWin32Window;
            return win == null ? ShowDialog() : ShowDialog(win);
        }

        public void CloseView(DialogResult result)
        {
            _allowClose = true;
            DialogResult = result;
            Close();
        }

        protected override void OnShown(EventArgs e)
        {
            _allowClose = false;
            _handlingCloseRequest = false;
            DialogResult = DialogResult.None;
            base.OnShown(e);
            var handler = ViewReady;
            handler?.Invoke(this, EventArgs.Empty);
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (!_allowClose && e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                if (!_handlingCloseRequest)
                {
                    RequestCancelClose();
                }

                return;
            }

            base.OnFormClosing(e);
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == Keys.Escape && !_allowClose)
            {
                RequestCancelClose();
                return true;
            }

            return base.ProcessCmdKey(ref msg, keyData);
        }

        private void WireEvents()
        {
            _cmbPresets.SelectedIndexChanged += delegate
            {
                if (!_suppressEvents)
                {
                    SelectedPresetChanged?.Invoke(this, EventArgs.Empty);
                }
            };

            _btnNew.Click += delegate { NewPresetRequested?.Invoke(this, EventArgs.Empty); };
            _btnDuplicate.Click += delegate { DuplicatePresetRequested?.Invoke(this, EventArgs.Empty); };
            _btnReload.Click += delegate { ReloadRequested?.Invoke(this, EventArgs.Empty); };
            _btnSave.Click += delegate { SaveRequested?.Invoke(this, EventArgs.Empty); };
            _btnSaveAs.Click += delegate { SaveAsRequested?.Invoke(this, EventArgs.Empty); };
            _btnDelete.Click += delegate { DeleteRequested?.Invoke(this, EventArgs.Empty); };
            _btnOpenLocation.Click += delegate { OpenPresetLocationRequested?.Invoke(this, EventArgs.Empty); };
            _btnOk.Click += delegate { OkRequested?.Invoke(this, EventArgs.Empty); };
            _btnCancel.Click += delegate { RequestCancelClose(); };

            _listSteps.SelectedIndexChanged += delegate
            {
                if (!_suppressEvents)
                {
                    SelectedStructureChanged?.Invoke(this, EventArgs.Empty);
                }
            };

            _btnAddStep.Click += delegate { AddStepRequested?.Invoke(this, EventArgs.Empty); };
            _btnRemoveStep.Click += delegate { RemoveStepRequested?.Invoke(this, EventArgs.Empty); };
            _btnMoveUp.Click += delegate { MoveStepUpRequested?.Invoke(this, EventArgs.Empty); };
            _btnMoveDown.Click += delegate { MoveStepDownRequested?.Invoke(this, EventArgs.Empty); };

            _cmbPlugin.SelectedIndexChanged += delegate
            {
                if (!_suppressEvents)
                {
                    SelectedPluginChanged?.Invoke(this, EventArgs.Empty);
                }
            };

            _cmbCapability.SelectedIndexChanged += delegate
            {
                if (!_suppressEvents)
                {
                    SelectedCapabilityChanged?.Invoke(this, EventArgs.Empty);
                }
            };

            _cmbEncoderPlugin.SelectedIndexChanged += delegate
            {
                if (!_suppressEvents)
                {
                    EncoderPluginChanged?.Invoke(this, EventArgs.Empty);
                }
            };

            _cmbEncoderCapability.SelectedIndexChanged += delegate
            {
                if (!_suppressEvents)
                {
                    EncoderCapabilityChanged?.Invoke(this, EventArgs.Empty);
                }
            };

            _cmbMetricPlugin.SelectedIndexChanged += delegate
            {
                if (!_suppressEvents)
                {
                    MetricPluginChanged?.Invoke(this, EventArgs.Empty);
                }
            };

            _cmbMetricCapability.SelectedIndexChanged += delegate
            {
                if (!_suppressEvents)
                {
                    MetricCapabilityChanged?.Invoke(this, EventArgs.Empty);
                }
            };

            _txtPresetId.TextChanged += delegate { RaiseMetadataChanged(); };
            _txtDisplayName.TextChanged += delegate { RaiseMetadataChanged(); };
            _txtDescription.TextChanged += delegate { RaiseMetadataChanged(); };
            _propertyGrid.PropertyValueChanged += delegate
            {
                if (!_suppressEvents)
                {
                    ParameterValuesChanged?.Invoke(this, EventArgs.Empty);
                }
            };
            _propertyGridEncoder.PropertyValueChanged += delegate
            {
                if (!_suppressEvents)
                {
                    EncoderParameterValuesChanged?.Invoke(this, EventArgs.Empty);
                }
            };
            _propertyGridMetric.PropertyValueChanged += delegate
            {
                if (!_suppressEvents)
                {
                    MetricParameterValuesChanged?.Invoke(this, EventArgs.Empty);
                }
            };
            _propertyGridOutput.PropertyValueChanged += delegate
            {
                if (!_suppressEvents)
                {
                    OutputPolicyChanged?.Invoke(this, EventArgs.Empty);
                }
            };
        }

        private void RaiseMetadataChanged()
        {
            if (_suppressEvents)
            {
                return;
            }

            MetadataChanged?.Invoke(this, EventArgs.Empty);
        }

        private void FillChoiceList(ComboBox combo, IReadOnlyList<PresetEditorChoiceItem> items, string selectedId)
        {
            _suppressEvents = true;
            try
            {
                combo.BeginUpdate();
                combo.SelectedIndex = -1;
                combo.Items.Clear();
                if (items != null)
                {
                    foreach (var item in items)
                    {
                        combo.Items.Add(item);
                    }
                }

                var selectIndex = -1;
                if (!string.IsNullOrWhiteSpace(selectedId))
                {
                    for (var i = 0; i < combo.Items.Count; i++)
                    {
                        var item = combo.Items[i] as PresetEditorChoiceItem;
                        if (item != null && string.Equals(item.Id, selectedId, StringComparison.OrdinalIgnoreCase))
                        {
                            selectIndex = i;
                            break;
                        }
                    }
                }

                if (selectIndex < 0 && combo.Items.Count > 0)
                {
                    selectIndex = 0;
                }

                ForceComboSelection(combo, selectIndex);
            }
            finally
            {
                combo.EndUpdate();
                _suppressEvents = false;
            }

            combo.Refresh();
        }

        private static void ForceComboSelection(ComboBox combo, int index)
        {
            if (combo == null)
            {
                return;
            }

            if (index < -1 || index >= combo.Items.Count)
            {
                index = -1;
            }

            if (combo.SelectedIndex == index)
            {
                combo.SelectedIndex = -1;
            }

            combo.SelectedIndex = index;
            if (index >= 0 && index < combo.Items.Count)
            {
                combo.Text = combo.Items[index].ToString();
            }
            else
            {
                combo.Text = string.Empty;
            }

            combo.Refresh();
        }

        private static string GetSelectedChoiceId(ComboBox combo)
        {
            var item = combo.SelectedItem as PresetEditorChoiceItem;
            return item == null ? null : item.Id;
        }

        private static void SetChildEditorEnabled(ComboBox pluginCombo, ComboBox capabilityCombo, PropertyGrid grid, bool enabled)
        {
            if (pluginCombo != null) pluginCombo.Enabled = enabled;
            if (capabilityCombo != null) capabilityCombo.Enabled = enabled;
            if (grid != null) grid.Enabled = enabled;
        }

        private void SetControlTextSuppressed(Control control, string text)
        {
            if (control == null)
            {
                return;
            }

            var previous = _suppressEvents;
            _suppressEvents = true;
            try
            {
                var value = text ?? string.Empty;
                if (!string.Equals(control.Text, value, StringComparison.Ordinal))
                {
                    control.Text = value;
                }
            }
            finally
            {
                _suppressEvents = previous;
            }
        }
    }
}
