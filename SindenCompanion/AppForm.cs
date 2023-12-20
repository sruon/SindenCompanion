﻿using System;
using System.Windows.Controls;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Forms.Integration;
using System.Diagnostics;
using System.IO;
using SindenCompanionShared;

namespace SindenCompanion
{
    public partial class AppForm : Form
    {
        public readonly System.Windows.Controls.RichTextBox WpfRichTextBox;

        private bool _userRequestedClose = false;

        private readonly Config _conf;

        private Action<RecoilProfile> _callback;
        public AppForm(Config conf)
        {
            _conf = conf;
            InitializeComponent();
            var richTextBoxHost = new ElementHost
            {
                Dock = DockStyle.Fill,
            };

            _richTextBoxPanel.Controls.Add(richTextBoxHost);

            var wpfRichTextBox = new System.Windows.Controls.RichTextBox
            {
                Background = System.Windows.Media.Brushes.Black,
                Foreground = System.Windows.Media.Brushes.LightGray,
                FontFamily = new System.Windows.Media.FontFamily("Cascadia Mono, Consolas, Courier New, monospace"),
                FontSize = 14,
                IsReadOnly = true,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
                Margin = new Thickness(0),
            };

            wpfRichTextBox.TextChanged += wpfRichTextBox_TextChanged;

            richTextBoxHost.Child = wpfRichTextBox;
            WpfRichTextBox = wpfRichTextBox;
        }

        public void SetCallback(Action<RecoilProfile> callback)
        {
            _callback = callback;
        }

        private void AppForm_Load(object sender, EventArgs e)
        {

        }

        private void wpfRichTextBox_TextChanged(object sender, EventArgs e)
        {
            WpfRichTextBox.ScrollToEnd();
        }

        private void AppForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (!_userRequestedClose)
            {
                e.Cancel = true;
                Hide();
            }
        }

        private void notificationIcon_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            this.WindowState = FormWindowState.Minimized;
            Show();
            this.WindowState = FormWindowState.Normal;
        }

        private void showMenuItem_Click(object sender, EventArgs e)
        {
            this.WindowState = FormWindowState.Minimized;
            Show();
            this.WindowState = FormWindowState.Normal;
        }

        private void exitMenuItem_Click(object sender, EventArgs e)
        {
            _userRequestedClose = true;
            System.Windows.Forms.Application.Exit();
        }

        private void configFileMenuItem_Click(object sender, EventArgs e)
        {
            Process.Start($"{Directory.GetCurrentDirectory()}\\config.yaml");
        }

        private void bootMenuItem_Click(object sender, EventArgs e)
        {
            if (Startup.IsInStartup())
            {
                Startup.RemoveFromStartup();
            }
            else
            {
                Startup.RunOnStartup();
            }

            bootMenuItem.Checked = Startup.IsInStartup();
        }

        private void NotificationIconMenu_Opened(object sender, EventArgs e)
        {
            bootMenuItem.Checked = Startup.IsInStartup();
            changeProfileMenuItem.DropDownItems.Clear();
            foreach (var profile in _conf.RecoilProfiles)
            {
                var item = new ToolStripMenuItem(profile.Name);
                item.Click += (s, a) =>
                {
                    _callback(profile);
                };
                changeProfileMenuItem.DropDownItems.Add(item);
            }   
        }

        private void AppForm_Resize(object sender, EventArgs e)
        {
            if (this.WindowState == FormWindowState.Minimized)
            {
                Hide();
                NotificationIcon.Visible = true;
            }
        }

    }
}
