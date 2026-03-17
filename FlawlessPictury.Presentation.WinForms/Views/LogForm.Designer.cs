using System.ComponentModel;
using System.Windows.Forms;

namespace FlawlessPictury.Presentation.WinForms.Views
{
    partial class LogForm
    {
        private IContainer components = null;
        private MenuStrip menuMain;
        private ToolStripMenuItem miFile;
        private ToolStripMenuItem miFileSaveAs;
        private ToolStripSeparator miFileSep1;
        private ToolStripMenuItem miFileClose;
        private ToolStripMenuItem miView;
        private ToolStripMenuItem miViewDebugMessages;
        private ToolStripMenuItem miViewAutoScroll;
        private ToolStripMenuItem miTools;
        private ToolStripMenuItem miToolsCopyAll;
        private ToolStripMenuItem miToolsClear;
        private ListView listLog;
        private ColumnHeader colLine;

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
            this.menuMain = new System.Windows.Forms.MenuStrip();
            this.miFile = new System.Windows.Forms.ToolStripMenuItem();
            this.miFileSaveAs = new System.Windows.Forms.ToolStripMenuItem();
            this.miFileSep1 = new System.Windows.Forms.ToolStripSeparator();
            this.miFileClose = new System.Windows.Forms.ToolStripMenuItem();
            this.miView = new System.Windows.Forms.ToolStripMenuItem();
            this.miViewDebugMessages = new System.Windows.Forms.ToolStripMenuItem();
            this.miViewAutoScroll = new System.Windows.Forms.ToolStripMenuItem();
            this.miTools = new System.Windows.Forms.ToolStripMenuItem();
            this.miToolsCopyAll = new System.Windows.Forms.ToolStripMenuItem();
            this.miToolsClear = new System.Windows.Forms.ToolStripMenuItem();
            this.listLog = new System.Windows.Forms.ListView();
            this.colLine = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.menuMain.SuspendLayout();
            this.SuspendLayout();
            // 
            // menuMain
            // 
            this.menuMain.GripMargin = new System.Windows.Forms.Padding(2, 2, 0, 2);
            this.menuMain.ImageScalingSize = new System.Drawing.Size(20, 20);
            this.menuMain.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.miFile,
            this.miView,
            this.miTools});
            this.menuMain.Location = new System.Drawing.Point(0, 0);
            this.menuMain.Name = "menuMain";
            this.menuMain.Padding = new System.Windows.Forms.Padding(9, 3, 0, 3);
            this.menuMain.Size = new System.Drawing.Size(1500, 48);
            this.menuMain.TabIndex = 0;
            this.menuMain.Text = "menuStrip1";
            // 
            // miFile
            // 
            this.miFile.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.miFileSaveAs,
            this.miFileSep1,
            this.miFileClose});
            this.miFile.Name = "miFile";
            this.miFile.Size = new System.Drawing.Size(71, 71);
            this.miFile.Text = "File";
            // 
            // miFileSaveAs
            // 
            this.miFileSaveAs.Name = "miFileSaveAs";
            this.miFileSaveAs.Size = new System.Drawing.Size(244, 44);
            this.miFileSaveAs.Text = "Save As...";
            // 
            // miFileSep1
            // 
            this.miFileSep1.Name = "miFileSep1";
            this.miFileSep1.Size = new System.Drawing.Size(241, 6);
            // 
            // miFileClose
            // 
            this.miFileClose.Name = "miFileClose";
            this.miFileClose.Size = new System.Drawing.Size(244, 44);
            this.miFileClose.Text = "Close";
            // 
            // miView
            // 
            this.miView.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.miViewDebugMessages,
            this.miViewAutoScroll});
            this.miView.Name = "miView";
            this.miView.Size = new System.Drawing.Size(85, 71);
            this.miView.Text = "View";
            // 
            // miViewDebugMessages
            // 
            this.miViewDebugMessages.CheckOnClick = true;
            this.miViewDebugMessages.Name = "miViewDebugMessages";
            this.miViewDebugMessages.Size = new System.Drawing.Size(329, 44);
            this.miViewDebugMessages.Text = "Debug messages";
            // 
            // miViewAutoScroll
            // 
            this.miViewAutoScroll.Checked = true;
            this.miViewAutoScroll.CheckOnClick = true;
            this.miViewAutoScroll.CheckState = System.Windows.Forms.CheckState.Checked;
            this.miViewAutoScroll.Name = "miViewAutoScroll";
            this.miViewAutoScroll.Size = new System.Drawing.Size(329, 44);
            this.miViewAutoScroll.Text = "Auto-scroll";
            // 
            // miTools
            // 
            this.miTools.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.miToolsCopyAll,
            this.miToolsClear});
            this.miTools.Name = "miTools";
            this.miTools.Size = new System.Drawing.Size(89, 71);
            this.miTools.Text = "Tools";
            // 
            // miToolsCopyAll
            // 
            this.miToolsCopyAll.Name = "miToolsCopyAll";
            this.miToolsCopyAll.Size = new System.Drawing.Size(236, 44);
            this.miToolsCopyAll.Text = "Copy All";
            // 
            // miToolsClear
            // 
            this.miToolsClear.Name = "miToolsClear";
            this.miToolsClear.Size = new System.Drawing.Size(236, 44);
            this.miToolsClear.Text = "Clear";
            // 
            // listLog
            // 
            this.listLog.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.colLine});
            this.listLog.Dock = System.Windows.Forms.DockStyle.Fill;
            this.listLog.FullRowSelect = true;
            this.listLog.HeaderStyle = System.Windows.Forms.ColumnHeaderStyle.None;
            this.listLog.HideSelection = false;
            this.listLog.Location = new System.Drawing.Point(0, 48);
            this.listLog.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.listLog.Name = "listLog";
            this.listLog.Size = new System.Drawing.Size(1500, 890);
            this.listLog.TabIndex = 1;
            this.listLog.UseCompatibleStateImageBehavior = false;
            this.listLog.View = System.Windows.Forms.View.Details;
            // 
            // colLine
            // 
            this.colLine.Width = 900;
            // 
            // LogForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(12F, 25F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1500, 938);
            this.Controls.Add(this.listLog);
            this.Controls.Add(this.menuMain);
            this.MainMenuStrip = this.menuMain;
            this.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.Name = "LogForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Log";
            this.menuMain.ResumeLayout(false);
            this.menuMain.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }
    }
}
