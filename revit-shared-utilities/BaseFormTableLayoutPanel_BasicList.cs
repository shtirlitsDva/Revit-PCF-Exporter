using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace Shared
{
    public partial class BaseFormTableLayoutPanel_Basic : System.Windows.Forms.Form
    {
        /// <summary>
        /// String to return.
        /// </summary>
        public string strTR { get; private set; }

        public BaseFormTableLayoutPanel_Basic(List<string> stringList)
        {
            InitializeComponent();

            var rowCount = stringList.Count;
            var columnCount = 1;

            this.Height = stringList.Count * 50;
            this.Width = 200;

            this.tableLayoutPanel1.ColumnCount = columnCount;
            this.tableLayoutPanel1.RowCount = rowCount;

            this.tableLayoutPanel1.ColumnStyles.Clear();
            this.tableLayoutPanel1.RowStyles.Clear();

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
                b.Text = stringList[i];
                b.Name = string.Format("b_{0}", i + 1);
                b.Click += b_Click;
                b.Dock = DockStyle.Fill;
                b.AutoSizeMode = 0;
                this.tableLayoutPanel1.Controls.Add(b);
            }
        }

        public BaseFormTableLayoutPanel_Basic(Dictionary<string,string> dict)
        {
            InitializeComponent();

            var rowCount = dict.Count;
            var columnCount = 1;

            this.Height = dict.Count * 50;
            this.Width = 200;

            this.tableLayoutPanel1.ColumnCount = columnCount;
            this.tableLayoutPanel1.RowCount = rowCount;

            this.tableLayoutPanel1.ColumnStyles.Clear();
            this.tableLayoutPanel1.RowStyles.Clear();

            for (int i = 0; i < columnCount; i++)
            {
                this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100 / columnCount));
            }
            for (int i = 0; i < rowCount; i++)
            {
                this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100 / rowCount));
            }

            foreach (KeyValuePair<string,string> entry in dict)
            {
                var b = new Button();
                b.Text = entry.Key;
                //https://stackoverflow.com/questions/5652515/how-can-i-pass-addition-local-object-variable-to-my-event-handler
                b.Click += (sender, e) => b_ClickDict(sender, e, dict);
                b.Dock = DockStyle.Fill;
                b.AutoSizeMode = 0;
                this.tableLayoutPanel1.Controls.Add(b);
            }
        }

        private void b_Click(object sender, EventArgs e)
        {
            var b = sender as Button;
            strTR = b.Text;
            this.Close();
        }

        private void b_ClickDict(object sender, EventArgs e, Dictionary<string,string> dict)
        {
            var b = sender as Button;
            strTR = dict[b.Text];
            this.Close();
        }
    }
}
