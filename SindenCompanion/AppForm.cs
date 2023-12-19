using System;
using System.Windows.Controls;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Forms.Integration;
using System.Diagnostics;
using System.IO;

namespace SindenCompanion
{
    public partial class AppForm : Form
    {
        public readonly System.Windows.Controls.RichTextBox WpfRichTextBox;

        private bool _userRequestedClose = false;
        public AppForm()
        {
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
            Show();
        }

        private void showMenuItem_Click(object sender, EventArgs e)
        {
            Show();
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
    }
}
