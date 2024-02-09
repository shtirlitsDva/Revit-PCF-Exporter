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

        private int desiredStartLocationX;
        private int desiredStartLocationY;

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

        public BaseFormTableLayoutPanel_Basic(int x, int y, List<string> stringList) : this(stringList)
        {
            desiredStartLocationX = x;
            desiredStartLocationY = y;

            Load += new EventHandler(BaseFormTableLayoutPanel_Basic_Load);
        }

        public BaseFormTableLayoutPanel_Basic(Dictionary<string, string> dict)
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

            foreach (KeyValuePair<string, string> entry in dict)
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

        public BaseFormTableLayoutPanel_Basic(int x, int y, Dictionary<string, string> stringDict) : this(stringDict)
        {
            desiredStartLocationX = x;
            desiredStartLocationY = y;

            Load += new EventHandler(BaseFormTableLayoutPanel_Basic_Load);
        }

        private void BaseFormTableLayoutPanel_Basic_Load(object sender, EventArgs e)
        {
            // Get screen dimensions where the cursor is located
            Screen screen = Screen.FromPoint(
                new System.Drawing.Point(
                    desiredStartLocationX, desiredStartLocationY));
            System.Drawing.Rectangle screenArea = screen.WorkingArea;

            // Adjust X position
            if (desiredStartLocationX + Width > screenArea.Right)
                desiredStartLocationX = screenArea.Right - Width;
            if (desiredStartLocationX < screenArea.Left)
                desiredStartLocationX = screenArea.Left;

            // Adjust Y position
            if (desiredStartLocationY + Height > screenArea.Bottom)
                desiredStartLocationY = screenArea.Bottom - Height;
            if (desiredStartLocationY < screenArea.Top)
                desiredStartLocationY = screenArea.Top;

            SetDesktopLocation(desiredStartLocationX, desiredStartLocationY);
        }

        private void b_Click(object sender, EventArgs e)
        {
            var b = sender as Button;
            strTR = b.Text;
            this.Close();
        }

        private void b_ClickDict(object sender, EventArgs e, Dictionary<string, string> dict)
        {
            var b = sender as Button;
            strTR = dict[b.Text];
            this.Close();
        }
    }
}
