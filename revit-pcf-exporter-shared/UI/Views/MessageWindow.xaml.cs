using System.Windows;

namespace PcfExporter.UI.Views
{
    /// <summary>
    /// Info/error dialog with selectable, copyable message text (a requirement:
    /// users report exceptions by copying the full text including stack trace).
    /// </summary>
    public partial class MessageWindow : Window
    {
        public MessageWindow(string header, string message, bool isError)
        {
            InitializeComponent();
            DarkTitleBar.Apply(this);
            Title = isError ? "PCF Exporter — Error" : "PCF Exporter";
            HeaderText.Text = header;
            MessageText.Text = message;
        }

        private void CopyButton_Click(object sender, RoutedEventArgs e)
        {
            if (!ClipboardText.TrySet(MessageText.Text))
                HeaderText.Text = "Could not access the clipboard — select the text and press Ctrl+C instead.";
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e) => Close();
    }
}
