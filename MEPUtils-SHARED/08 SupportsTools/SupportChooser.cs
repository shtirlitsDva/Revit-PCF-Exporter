using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
//using MoreLinq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Autodesk.Revit.UI;
using Autodesk.Revit.DB;
using fi = Shared.Filter;
using op = Shared.Output;
using tr = Shared.Transformation;
using mp = Shared.MepUtils;

namespace MEPUtils.PlaceSupport
{
    public partial class SupportChooser : System.Windows.Forms.Form
    {
        public string supportName { get; private set; }

        public SupportChooser(ExternalCommandData commandData)
        {
            InitializeComponent();
            Document doc = commandData.Application.ActiveUIDocument.Document;

            FilteredElementCollector collector = new FilteredElementCollector(doc);
            var family = collector.OfClass(typeof(Family)).Where(e => e.Name == "Support Symbolic").Cast<Family>().FirstOrDefault();
            if (family == null) throw new Exception("No Support Symbolic family in project!");
            var famSymbolList = family.GetFamilySymbolIds();
            var query = famSymbolList.Select(t => doc.GetElement(t)).ToHashSet();
            var list = query.Select(e => e.Name).ToList();
            list.Sort();

            //From here: http://stackoverflow.com/questions/34426888/dynamic-button-creation-placing-them-in-a-predefined-order-using-c-sharp
            var rowCount = list.Count;
            var columnCount = 1;

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
                b.Text = list[i];
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
            supportName = b.Text;
            this.Close();
        }

        //StringBuilder sb = new StringBuilder();
        //foreach (var f in query)
        //{
        //    sb.AppendLine(f.Name);
        //}
        //ut.InfoMsg(sb.ToString());
    }
}
