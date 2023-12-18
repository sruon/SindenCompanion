using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Forms.Integration;
using System.Windows.Media;
namespace SindenCompanion
{
    public partial class AppForm : Form
    {
        private static readonly object _syncRoot = new object();
        public readonly System.Windows.Controls.RichTextBox WpfRichTextBox;
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
            e.Cancel = true;
            Hide();
        }

        private void notificationIcon_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            Show();
        }
    }
}
