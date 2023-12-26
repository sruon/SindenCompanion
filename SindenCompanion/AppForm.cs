using System;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Forms.Integration;
using System.Windows.Media;
using SindenCompanionShared;
using Application = System.Windows.Forms.Application;
using RichTextBox = System.Windows.Controls.RichTextBox;

namespace SindenCompanion
{
    public partial class AppForm : Form
    {
        private readonly Config _conf;
        public readonly RichTextBox WpfRichTextBox;

        private Action<int, RecoilProfile> _callback;

        private bool _userRequestedClose;

        public AppForm(Config conf)
        {
            _conf = conf;
            InitializeComponent();
            var richTextBoxHost = new ElementHost
            {
                Dock = DockStyle.Fill
            };

            _richTextBoxPanel.Controls.Add(richTextBoxHost);

            var wpfRichTextBox = new RichTextBox
            {
                Background = Brushes.Black,
                Foreground = Brushes.LightGray,
                FontFamily = new FontFamily("Cascadia Mono, Consolas, Courier New, monospace"),
                FontSize = 14,
                IsReadOnly = true,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
                Margin = new Thickness(0)
            };

            wpfRichTextBox.TextChanged += wpfRichTextBox_TextChanged;

            richTextBoxHost.Child = wpfRichTextBox;
            WpfRichTextBox = wpfRichTextBox;
        }

        public void SetCallback(Action<int, RecoilProfile> callback)
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
            WindowState = FormWindowState.Minimized;
            Show();
            WindowState = FormWindowState.Normal;
        }

        private void showMenuItem_Click(object sender, EventArgs e)
        {
            WindowState = FormWindowState.Minimized;
            Show();
            WindowState = FormWindowState.Normal;
        }

        private void exitMenuItem_Click(object sender, EventArgs e)
        {
            _userRequestedClose = true;
            Application.Exit();
        }

        private void configFileMenuItem_Click(object sender, EventArgs e)
        {
            Process.Start($"{Directory.GetCurrentDirectory()}\\config.yaml");
        }

        private void bootMenuItem_Click(object sender, EventArgs e)
        {
            if (Startup.IsInStartup())
                Startup.RemoveFromStartup();
            else
                Startup.RunOnStartup();

            bootMenuItem.Checked = Startup.IsInStartup();
        }

        private void NotificationIconMenu_Opened(object sender, EventArgs e)
        {
            bootMenuItem.Checked = Startup.IsInStartup();
            changeProfileMenuItem.DropDownItems.Clear();
            foreach (var profile in _conf.RecoilProfiles)
            {
                var item = new ToolStripMenuItem(profile.Name);
                item.Click += (s, a) => { _callback(-1, profile); };
                changeProfileMenuItem.DropDownItems.Add(item);
            }
        }

        private void AppForm_Resize(object sender, EventArgs e)
        {
            if (WindowState == FormWindowState.Minimized)
            {
                Hide();
                NotificationIcon.Visible = true;
            }
        }
    }
}