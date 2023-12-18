namespace SindenCompanion
{
    partial class AppForm
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
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
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(AppForm));
            this._richTextBoxPanel = new System.Windows.Forms.Panel();
            this.NotificationIcon = new System.Windows.Forms.NotifyIcon(this.components);
            this.NotificationIconMenu = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.toolStripMenuItem1 = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripMenuItem2 = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
            this.Exit = new System.Windows.Forms.ToolStripMenuItem();
            this.NotificationIconMenu.SuspendLayout();
            this.SuspendLayout();
            // 
            // _richTextBoxPanel
            // 
            this._richTextBoxPanel.AutoScroll = true;
            this._richTextBoxPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this._richTextBoxPanel.Location = new System.Drawing.Point(0, 0);
            this._richTextBoxPanel.Name = "_richTextBoxPanel";
            this._richTextBoxPanel.Size = new System.Drawing.Size(1620, 450);
            this._richTextBoxPanel.TabIndex = 1;
            // 
            // NotificationIcon
            // 
            this.NotificationIcon.BalloonTipIcon = System.Windows.Forms.ToolTipIcon.Info;
            this.NotificationIcon.BalloonTipText = "Test";
            this.NotificationIcon.ContextMenuStrip = this.NotificationIconMenu;
            this.NotificationIcon.Icon = ((System.Drawing.Icon)(resources.GetObject("NotificationIcon.Icon")));
            this.NotificationIcon.Text = "Sinden Companion";
            this.NotificationIcon.Visible = true;
            this.NotificationIcon.MouseDoubleClick += new System.Windows.Forms.MouseEventHandler(this.notificationIcon_MouseDoubleClick);
            // 
            // NotificationIconMenu
            // 
            this.NotificationIconMenu.ImageScalingSize = new System.Drawing.Size(24, 24);
            this.NotificationIconMenu.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripMenuItem1,
            this.toolStripMenuItem2,
            this.toolStripSeparator1,
            this.Exit});
            this.NotificationIconMenu.Name = "NotificationIconMenu";
            this.NotificationIconMenu.Size = new System.Drawing.Size(241, 139);
            // 
            // toolStripMenuItem1
            // 
            this.toolStripMenuItem1.Name = "toolStripMenuItem1";
            this.toolStripMenuItem1.Size = new System.Drawing.Size(240, 32);
            this.toolStripMenuItem1.Text = "Show";
            // 
            // toolStripMenuItem2
            // 
            this.toolStripMenuItem2.Name = "toolStripMenuItem2";
            this.toolStripMenuItem2.Size = new System.Drawing.Size(240, 32);
            this.toolStripMenuItem2.Text = "Start at boot";
            // 
            // toolStripSeparator1
            // 
            this.toolStripSeparator1.Name = "toolStripSeparator1";
            this.toolStripSeparator1.Size = new System.Drawing.Size(237, 6);
            // 
            // Exit
            // 
            this.Exit.Name = "Exit";
            this.Exit.Size = new System.Drawing.Size(240, 32);
            this.Exit.Text = "Exit";
            // 
            // AppForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(9F, 20F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1620, 450);
            this.Controls.Add(this._richTextBoxPanel);
            this.Name = "AppForm";
            this.Text = "Sinden Companion";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.AppForm_FormClosing);
            this.Load += new System.EventHandler(this.AppForm_Load);
            this.NotificationIconMenu.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Panel _richTextBoxPanel;
        private System.Windows.Forms.NotifyIcon NotificationIcon;
        private System.Windows.Forms.ContextMenuStrip NotificationIconMenu;
        private System.Windows.Forms.ToolStripMenuItem toolStripMenuItem1;
        private System.Windows.Forms.ToolStripMenuItem toolStripMenuItem2;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator1;
        private System.Windows.Forms.ToolStripMenuItem Exit;
    }
}
