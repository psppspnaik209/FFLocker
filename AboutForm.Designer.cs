namespace FFLocker
{
    partial class AboutForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(AboutForm));
            this.lblTitle = new System.Windows.Forms.Label();
            this.lblMadeWithLove = new System.Windows.Forms.Label();
            this.txtLicenses = new System.Windows.Forms.TextBox();
            this.btnDonate = new System.Windows.Forms.Button();
            this.lblLicenses = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // lblTitle
            // 
            this.lblTitle.AutoSize = true;
            this.lblTitle.Font = new System.Drawing.Font("Segoe UI", 14.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point);
            this.lblTitle.Location = new System.Drawing.Point(12, 9);
            this.lblTitle.Name = "lblTitle";
            this.lblTitle.Size = new System.Drawing.Size(89, 25);
            this.lblTitle.TabIndex = 0;
            this.lblTitle.Text = "FFLocker";
            // 
            // lblMadeWithLove
            // 
            this.lblMadeWithLove.AutoSize = true;
            this.lblMadeWithLove.Location = new System.Drawing.Point(12, 44);
            this.lblMadeWithLove.Name = "lblMadeWithLove";
            this.lblMadeWithLove.Size = new System.Drawing.Size(156, 15);
            this.lblMadeWithLove.TabIndex = 1;
            this.lblMadeWithLove.Text = "Made with ❤️ by TNBB Team";
            // 
            // txtLicenses
            // 
            this.txtLicenses.Location = new System.Drawing.Point(12, 87);
            this.txtLicenses.Multiline = true;
            this.txtLicenses.Name = "txtLicenses";
            this.txtLicenses.ReadOnly = true;
            this.txtLicenses.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.txtLicenses.Size = new System.Drawing.Size(360, 150);
            this.txtLicenses.TabIndex = 2;
            this.txtLicenses.Text = "MIT License...";
            // 
            // btnDonate
            // 
            this.btnDonate.Location = new System.Drawing.Point(297, 243);
            this.btnDonate.Name = "btnDonate";
            this.btnDonate.Size = new System.Drawing.Size(75, 23);
            this.btnDonate.TabIndex = 3;
            this.btnDonate.Text = "Donate";
            this.btnDonate.UseVisualStyleBackColor = true;
            this.btnDonate.Click += new System.EventHandler(this.btnDonate_Click);
            // 
            // lblLicenses
            // 
            this.lblLicenses.AutoSize = true;
            this.lblLicenses.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point);
            this.lblLicenses.Location = new System.Drawing.Point(12, 69);
            this.lblLicenses.Name = "lblLicenses";
            this.lblLicenses.Size = new System.Drawing.Size(53, 15);
            this.lblLicenses.TabIndex = 4;
            this.lblLicenses.Text = "Licenses";
            // 
            // AboutForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(384, 278);
            this.Controls.Add(this.lblLicenses);
            this.Controls.Add(this.btnDonate);
            this.Controls.Add(this.txtLicenses);
            this.Controls.Add(this.lblMadeWithLove);
            this.Controls.Add(this.lblTitle);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "AboutForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "About FFLocker";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label lblTitle;
        private System.Windows.Forms.Label lblMadeWithLove;
        private System.Windows.Forms.TextBox txtLicenses;
        private System.Windows.Forms.Button btnDonate;
        private System.Windows.Forms.Label lblLicenses;
    }
}
