using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows;

namespace PcfExporter.UI.Views
{
    /// <summary>
    /// Shows one or more tables in copy-friendly grids. Replaces the COM-automated
    /// live Excel window: the user copies rows from here into the setup workbooks.
    /// </summary>
    public partial class TableWindow : Window
    {
        public TableWindow(string title, IReadOnlyList<DataTable> tables)
        {
            InitializeComponent();
            DarkTitleBar.Apply(this);
            Title = "PCF Exporter — " + title;
            Tabs.ItemsSource = tables;
            Tabs.SelectedIndex = 0;
        }

        private void CopyAllButton_Click(object sender, RoutedEventArgs e)
        {
            if (!(Tabs.SelectedItem is DataTable table)) return;

            var text = new StringBuilder();
            text.AppendLine(string.Join("\t", table.Columns.Cast<DataColumn>().Select(c => c.ColumnName)));
            foreach (DataRow row in table.Rows)
                text.AppendLine(string.Join("\t", row.ItemArray.Select(v => v?.ToString())));

            if (!ClipboardText.TrySet(text.ToString()))
                HintText.Text = "Could not access the clipboard — select rows and press Ctrl+C instead.";
            else
                HintText.Text = $"Copied {table.Rows.Count} row(s) — paste into Excel.";
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e) => Close();
    }
}
