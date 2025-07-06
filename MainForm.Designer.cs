namespace FFLocker
{
    partial class MainForm
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        private void InitializeComponent()
        {
            this.panelMain = new System.Windows.Forms.Panel();
            this.txtPath = new System.Windows.Forms.TextBox();
            this.btnBrowse = new System.Windows.Forms.Button();
            this.rbFile = new System.Windows.Forms.RadioButton();
            this.rbFolder = new System.Windows.Forms.RadioButton();
            this.btnLock = new System.Windows.Forms.Button();
            this.btnUnlock = new System.Windows.Forms.Button();
            this.panelPassword = new System.Windows.Forms.Panel();
            this.btnCancel = new System.Windows.Forms.Button();
            this.btnConfirm = new System.Windows.Forms.Button();
            this.txtPassword = new System.Windows.Forms.TextBox();
            this.lblPassword = new System.Windows.Forms.Label();
            this.progressBar = new System.Windows.Forms.ProgressBar();
            this.panelLockedItems = new System.Windows.Forms.Panel();
            this.cmbDisplayNameType = new System.Windows.Forms.ComboBox();
            this.lstLockedItems = new System.Windows.Forms.ListBox();
            this.panelLog = new System.Windows.Forms.Panel();
            this.btnClearLog = new System.Windows.Forms.Button();
            this.txtLog = new System.Windows.Forms.TextBox();
            this.flowLayoutPanelControls = new System.Windows.Forms.FlowLayoutPanel();
            this.lblStatus = new System.Windows.Forms.Label();
            this.chkShowInfo = new System.Windows.Forms.CheckBox();
            this.btnShowLocked = new System.Windows.Forms.Button();
            this.chkContextMenu = new System.Windows.Forms.CheckBox();
            this.chkDarkMode = new System.Windows.Forms.CheckBox();
            this.btnAbout = new System.Windows.Forms.Button();
            this.panelMain.SuspendLayout();
            this.panelPassword.SuspendLayout();
            this.panelLockedItems.SuspendLayout();
            this.panelLog.SuspendLayout();
            this.flowLayoutPanelControls.SuspendLayout();
            this.SuspendLayout();
            // 
            // panelMain
            // 
            this.panelMain.Controls.Add(this.txtPath);
            this.panelMain.Controls.Add(this.btnBrowse);
            this.panelMain.Controls.Add(this.rbFile);
            this.panelMain.Controls.Add(this.rbFolder);
            this.panelMain.Controls.Add(this.btnLock);
            this.panelMain.Controls.Add(this.btnUnlock);
            this.panelMain.Controls.Add(this.panelPassword);
            this.panelMain.Controls.Add(this.progressBar);
            this.panelMain.Dock = System.Windows.Forms.DockStyle.Top;
            this.panelMain.Location = new System.Drawing.Point(0, 0);
            this.panelMain.Name = "panelMain";
            this.panelMain.Padding = new System.Windows.Forms.Padding(9);
            this.panelMain.Size = new System.Drawing.Size(584, 185);
            this.panelMain.TabIndex = 0;
            // 
            // txtPath
            // 
            this.txtPath.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txtPath.Location = new System.Drawing.Point(12, 12);
            this.txtPath.Name = "txtPath";
            this.txtPath.Size = new System.Drawing.Size(471, 23);
            this.txtPath.TabIndex = 0;
            // 
            // btnBrowse
            // 
            this.btnBrowse.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnBrowse.Location = new System.Drawing.Point(489, 12);
            this.btnBrowse.Name = "btnBrowse";
            this.btnBrowse.Size = new System.Drawing.Size(75, 23);
            this.btnBrowse.TabIndex = 1;
            this.btnBrowse.Text = "Browse...";
            this.btnBrowse.UseVisualStyleBackColor = true;
            this.btnBrowse.Click += new System.EventHandler(this.btnBrowse_Click);
            // 
            // rbFile
            // 
            this.rbFile.AutoSize = true;
            this.rbFile.Location = new System.Drawing.Point(12, 41);
            this.rbFile.Name = "rbFile";
            this.rbFile.Size = new System.Drawing.Size(43, 19);
            this.rbFile.TabIndex = 2;
            this.rbFile.Text = "File";
            this.rbFile.UseVisualStyleBackColor = true;
            // 
            // rbFolder
            // 
            this.rbFolder.AutoSize = true;
            this.rbFolder.Location = new System.Drawing.Point(61, 41);
            this.rbFolder.Name = "rbFolder";
            this.rbFolder.Size = new System.Drawing.Size(58, 19);
            this.rbFolder.TabIndex = 3;
            this.rbFolder.Text = "Folder";
            this.rbFolder.UseVisualStyleBackColor = true;
            // 
            // btnLock
            // 
            this.btnLock.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.btnLock.Location = new System.Drawing.Point(12, 66);
            this.btnLock.Name = "btnLock";
            this.btnLock.Size = new System.Drawing.Size(277, 35);
            this.btnLock.TabIndex = 4;
            this.btnLock.Text = "Lock";
            this.btnLock.UseVisualStyleBackColor = true;
            this.btnLock.Click += new System.EventHandler(this.btnLock_Click);
            // 
            // btnUnlock
            // 
            this.btnUnlock.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnUnlock.Location = new System.Drawing.Point(295, 66);
            this.btnUnlock.Name = "btnUnlock";
            this.btnUnlock.Size = new System.Drawing.Size(277, 35);
            this.btnUnlock.TabIndex = 5;
            this.btnUnlock.Text = "Unlock";
            this.btnUnlock.UseVisualStyleBackColor = true;
            this.btnUnlock.Click += new System.EventHandler(this.btnUnlock_Click);
            // 
            // panelPassword
            // 
            this.panelPassword.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.panelPassword.Controls.Add(this.btnCancel);
            this.panelPassword.Controls.Add(this.btnConfirm);
            this.panelPassword.Controls.Add(this.txtPassword);
            this.panelPassword.Controls.Add(this.lblPassword);
            this.panelPassword.Location = new System.Drawing.Point(12, 107);
            this.panelPassword.Name = "panelPassword";
            this.panelPassword.Size = new System.Drawing.Size(560, 40);
            this.panelPassword.TabIndex = 6;
            this.panelPassword.Visible = false;
            // 
            // btnCancel
            // 
            this.btnCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnCancel.Location = new System.Drawing.Point(482, 8);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(75, 23);
            this.btnCancel.TabIndex = 3;
            this.btnCancel.Text = "Cancel";
            this.btnCancel.UseVisualStyleBackColor = true;
            this.btnCancel.Click += new System.EventHandler(this.btnCancel_Click);
            // 
            // btnConfirm
            // 
            this.btnConfirm.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnConfirm.Location = new System.Drawing.Point(401, 8);
            this.btnConfirm.Name = "btnConfirm";
            this.btnConfirm.Size = new System.Drawing.Size(75, 23);
            this.btnConfirm.TabIndex = 2;
            this.btnConfirm.Text = "Confirm";
            this.btnConfirm.UseVisualStyleBackColor = true;
            this.btnConfirm.Click += new System.EventHandler(this.btnConfirm_Click);
            // 
            // txtPassword
            // 
            this.txtPassword.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txtPassword.Location = new System.Drawing.Point(68, 8);
            this.txtPassword.Name = "txtPassword";
            this.txtPassword.Size = new System.Drawing.Size(327, 23);
            this.txtPassword.TabIndex = 1;
            this.txtPassword.UseSystemPasswordChar = true;
            this.txtPassword.KeyDown += new System.Windows.Forms.KeyEventHandler(this.txtPassword_KeyDown);
            // 
            // lblPassword
            // 
            this.lblPassword.AutoSize = true;
            this.lblPassword.Location = new System.Drawing.Point(3, 11);
            this.lblPassword.Name = "lblPassword";
            this.lblPassword.Size = new System.Drawing.Size(59, 15);
            this.lblPassword.TabIndex = 0;
            this.lblPassword.Text = "Password:";
            // 
            // progressBar
            // 
            this.progressBar.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.progressBar.Location = new System.Drawing.Point(12, 153);
            this.progressBar.Name = "progressBar";
            this.progressBar.Size = new System.Drawing.Size(560, 23);
            this.progressBar.TabIndex = 7;
            // 
            // panelLockedItems
            // 
            this.panelLockedItems.Controls.Add(this.cmbDisplayNameType);
            this.panelLockedItems.Controls.Add(this.lstLockedItems);
            this.panelLockedItems.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panelLockedItems.Location = new System.Drawing.Point(3, 191);
            this.panelLockedItems.Name = "panelLockedItems";
            this.panelLockedItems.Padding = new System.Windows.Forms.Padding(9);
            this.panelLockedItems.Size = new System.Drawing.Size(578, 150);
            this.panelLockedItems.TabIndex = 1;
            this.panelLockedItems.Visible = false;
            // 
            // cmbDisplayNameType
            // 
            this.cmbDisplayNameType.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.cmbDisplayNameType.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbDisplayNameType.FormattingEnabled = true;
            this.cmbDisplayNameType.Items.AddRange(new object[] {
            "Original Name",
            "Locked Name"});
            this.cmbDisplayNameType.Location = new System.Drawing.Point(427, 9);
            this.cmbDisplayNameType.Name = "cmbDisplayNameType";
            this.cmbDisplayNameType.Size = new System.Drawing.Size(140, 23);
            this.cmbDisplayNameType.TabIndex = 16;
            this.cmbDisplayNameType.SelectedIndexChanged += new System.EventHandler(this.cmbDisplayNameType_SelectedIndexChanged);
            // 
            // lstLockedItems
            // 
            this.lstLockedItems.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.lstLockedItems.FormattingEnabled = true;
            this.lstLockedItems.ItemHeight = 15;
            this.lstLockedItems.Location = new System.Drawing.Point(9, 38);
            this.lstLockedItems.Name = "lstLockedItems";
            this.lstLockedItems.Size = new System.Drawing.Size(558, 94);
            this.lstLockedItems.TabIndex = 15;
            // 
            // panelLog
            // 
            this.panelLog.Controls.Add(this.btnClearLog);
            this.panelLog.Controls.Add(this.txtLog);
            this.panelLog.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panelLog.Location = new System.Drawing.Point(3, 347);
            this.panelLog.Name = "panelLog";
            this.panelLog.Padding = new System.Windows.Forms.Padding(9);
            this.panelLog.Size = new System.Drawing.Size(578, 150);
            this.panelLog.TabIndex = 2;
            this.panelLog.Visible = false;
            // 
            // btnClearLog
            // 
            this.btnClearLog.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnClearLog.Location = new System.Drawing.Point(492, 9);
            this.btnClearLog.Name = "btnClearLog";
            this.btnClearLog.Size = new System.Drawing.Size(75, 23);
            this.btnClearLog.TabIndex = 11;
            this.btnClearLog.Text = "Clear";
            this.btnClearLog.UseVisualStyleBackColor = true;
            this.btnClearLog.Click += new System.EventHandler(this.btnClearLog_Click);
            // 
            // txtLog
            // 
            this.txtLog.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txtLog.Location = new System.Drawing.Point(9, 38);
            this.txtLog.Multiline = true;
            this.txtLog.Name = "txtLog";
            this.txtLog.ReadOnly = true;
            this.txtLog.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.txtLog.Size = new System.Drawing.Size(558, 103);
            this.txtLog.TabIndex = 10;
            // 
            // tableLayoutPanelControls
            // 
            this.flowLayoutPanelControls.Controls.Add(this.lblStatus);
            this.flowLayoutPanelControls.Controls.Add(this.chkShowInfo);
            this.flowLayoutPanelControls.Controls.Add(this.btnShowLocked);
            this.flowLayoutPanelControls.Controls.Add(this.chkContextMenu);
            this.flowLayoutPanelControls.Controls.Add(this.chkDarkMode);
            this.flowLayoutPanelControls.Controls.Add(this.btnAbout);
            this.flowLayoutPanelControls.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.flowLayoutPanelControls.Location = new System.Drawing.Point(0, 223);
            this.flowLayoutPanelControls.Name = "flowLayoutPanelControls";
            this.flowLayoutPanelControls.Padding = new System.Windows.Forms.Padding(9);
            this.flowLayoutPanelControls.Size = new System.Drawing.Size(584, 38);
            this.flowLayoutPanelControls.TabIndex = 3;
            // 
            // lblStatus
            // 
            this.lblStatus.Location = new System.Drawing.Point(12, 12);
            this.lblStatus.Name = "lblStatus";
            this.lblStatus.Size = new System.Drawing.Size(150, 23);
            this.lblStatus.TabIndex = 8;
            this.lblStatus.Text = "Ready";
            this.lblStatus.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // chkShowInfo
            // 
            this.chkShowInfo.AutoSize = true;
            this.chkShowInfo.Location = new System.Drawing.Point(168, 12);
            this.chkShowInfo.Name = "chkShowInfo";
            this.chkShowInfo.Size = new System.Drawing.Size(107, 19);
            this.chkShowInfo.TabIndex = 9;
            this.chkShowInfo.Text = "Show more info";
            this.chkShowInfo.UseVisualStyleBackColor = true;
            this.chkShowInfo.CheckedChanged += new System.EventHandler(this.chkShowInfo_CheckedChanged);
            // 
            // btnShowLocked
            // 
            this.btnShowLocked.Location = new System.Drawing.Point(281, 12);
            this.btnShowLocked.Name = "btnShowLocked";
            this.btnShowLocked.Size = new System.Drawing.Size(120, 23);
            this.btnShowLocked.TabIndex = 14;
            this.btnShowLocked.Text = "Show Locked Items";
            this.btnShowLocked.UseVisualStyleBackColor = true;
            this.btnShowLocked.Click += new System.EventHandler(this.btnShowLocked_Click);
            // 
            // chkContextMenu
            // 
            this.chkContextMenu.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.chkContextMenu.AutoSize = true;
            this.chkContextMenu.Location = new System.Drawing.Point(407, 12);
            this.chkContextMenu.Name = "chkContextMenu";
            this.chkContextMenu.Size = new System.Drawing.Size(117, 19);
            this.chkContextMenu.TabIndex = 13;
            this.chkContextMenu.Text = "Context Menu";
            this.chkContextMenu.UseVisualStyleBackColor = true;
            // 
            // chkDarkMode
            // 
            this.chkDarkMode.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.chkDarkMode.AutoSize = true;
            this.chkDarkMode.Location = new System.Drawing.Point(530, 12);
            this.chkDarkMode.Name = "chkDarkMode";
            this.chkDarkMode.Size = new System.Drawing.Size(89, 19);
            this.chkDarkMode.TabIndex = 12;
            this.chkDarkMode.Text = "Dark Mode";
            this.chkDarkMode.UseVisualStyleBackColor = true;
            this.chkDarkMode.CheckedChanged += new System.EventHandler(this.chkDarkMode_CheckedChanged);
            // 
            // btnAbout
            // 
            this.btnAbout.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnAbout.Location = new System.Drawing.Point(625, 12);
            this.btnAbout.Name = "btnAbout";
            this.btnAbout.Size = new System.Drawing.Size(58, 23);
            this.btnAbout.TabIndex = 15;
            this.btnAbout.Text = "About";
            this.btnAbout.UseVisualStyleBackColor = true;
            this.btnAbout.Click += new System.EventHandler(this.btnAbout_Click);
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(700, 561);
            this.Controls.Add(this.flowLayoutPanelControls);
            this.Controls.Add(this.panelLog);
            this.Controls.Add(this.panelLockedItems);
            this.Controls.Add(this.panelMain);
            this.MinimumSize = new System.Drawing.Size(700, 300);
            this.Name = "MainForm";
            this.Text = "FFLocker";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.MainForm_FormClosing);
            this.panelMain.ResumeLayout(false);
            this.panelMain.PerformLayout();
            this.panelPassword.ResumeLayout(false);
            this.panelPassword.PerformLayout();
            this.panelLockedItems.ResumeLayout(false);
            this.panelLog.ResumeLayout(false);
            this.panelLog.PerformLayout();
            this.flowLayoutPanelControls.ResumeLayout(false);
            this.flowLayoutPanelControls.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Panel panelMain;
        private System.Windows.Forms.TextBox txtPath;
        private System.Windows.Forms.Button btnBrowse;
        private System.Windows.Forms.RadioButton rbFile;
        private System.Windows.Forms.RadioButton rbFolder;
        private System.Windows.Forms.Button btnLock;
        private System.Windows.Forms.Button btnUnlock;
        private System.Windows.Forms.Panel panelPassword;
        private System.Windows.Forms.Button btnCancel;
        private System.Windows.Forms.Button btnConfirm;
        private System.Windows.Forms.TextBox txtPassword;
        private System.Windows.Forms.Label lblPassword;
        private System.Windows.Forms.ProgressBar progressBar;
        private System.Windows.Forms.Panel panelLockedItems;
        private System.Windows.Forms.ComboBox cmbDisplayNameType;
        private System.Windows.Forms.ListBox lstLockedItems;
        private System.Windows.Forms.Panel panelLog;
        private System.Windows.Forms.Button btnClearLog;
        private System.Windows.Forms.TextBox txtLog;
        private System.Windows.Forms.FlowLayoutPanel flowLayoutPanelControls;
        private System.Windows.Forms.Label lblStatus;
        private System.Windows.Forms.CheckBox chkShowInfo;
        private System.Windows.Forms.Button btnShowLocked;
        private System.Windows.Forms.CheckBox chkContextMenu;
        private System.Windows.Forms.CheckBox chkDarkMode;
        private System.Windows.Forms.Button btnAbout;
    }
}