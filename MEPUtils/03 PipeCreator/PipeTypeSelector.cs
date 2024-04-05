using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Autodesk.Revit.UI;
using Autodesk.Revit.DB;


namespace MEPUtils
{
    public partial class PipeTypeSelector : System.Windows.Forms.Form
    {
        public string pipeTypeName { get; private set; }

        public PipeTypeSelector(UIApplication uiApp, List<string> pipeTypeNames)
        {
            InitializeComponent();
            Document doc = uiApp.ActiveUIDocument.Document;

            var rowCount = pipeTypeNames.Count;
            var columnCount = 1;

            this.tableLayoutPanel1.ColumnCount = columnCount;
            this.tableLayoutPanel1.RowCount = rowCount;

            this.tableLayoutPanel1.ColumnStyles.Clear();
            this.tableLayoutPanel1.RowStyles.Clear();

            this.Height = pipeTypeNames.Count * 50;

            for (int i = 0; i < columnCount; i++)
            {
                this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100 / columnCount));
            }
            for (int i = 0; i < rowCount; i++)
            {
                this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100 / rowCount));
            }

            for (int i = 0; i < rowCount * columnCount; i++)
            {
                var b = new Button();
                b.Text = pipeTypeNames[i];
                b.Name = string.Format("b_{0}", i + 1);
                b.Click += b_Click;
                b.Dock = DockStyle.Fill;
                b.AutoSizeMode = 0;
                this.tableLayoutPanel1.Controls.Add(b);
            }
        }

        private void b_Click(object sender, EventArgs e)
        {
            var b = sender as Button;
            pipeTypeName = b.Text;
            this.Close();
        }

        private void PipeTypeSelector_FormClosing(object sender, FormClosingEventArgs e)
        {
            MEPUtils.Properties.Settings.Default.PipeCreator_SelectedPipeTypeName = pipeTypeName;
            MEPUtils.Properties.Settings.Default.Save();
        }
    }
}
