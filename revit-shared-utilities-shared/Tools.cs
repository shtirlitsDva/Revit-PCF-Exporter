using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using MoreLinq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Autodesk.Revit.UI;
using Autodesk.Revit.DB;
using fi = Shared.Filter;
using op = Shared.Output;
using tr = Shared.Transformation;
using mp = Shared.MepUtils;

namespace Shared.Tools
{
    public partial class Tools : System.Windows.Forms.Form
    {
        private Dictionary<int, Func<ExternalCommandData, Result>> methodDict;
        private Dictionary<int, string> nameDict;
        public Func<ExternalCommandData, Result> MethodToExecute { get; private set; }

        private int desiredStartLocationX;
        private int desiredStartLocationY;

        public Tools()
        {
            InitializeComponent();

            //From here: http://stackoverflow.com/questions/34426888/dynamic-button-creation-placing-them-in-a-predefined-order-using-c-sharp
            //Edit the number of methods in rowCount here
            int columnCount = 1;
            int rowCount = 3;

            tableLayoutPanel1.ColumnCount = columnCount;
            tableLayoutPanel1.RowCount = rowCount;

            tableLayoutPanel1.ColumnStyles.Clear();
            tableLayoutPanel1.RowStyles.Clear();

            for (int i = 0; i < columnCount; i++)
            {
                tableLayoutPanel1.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100 / columnCount));
            }
            for (int i = 0; i < rowCount; i++)
            {
                tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Percent, 100 / rowCount));
            }

            //Place methods to execute in this dict
            methodDict = new Dictionary<int, Func<ExternalCommandData, Result>>
            {
                {0, Shared.Tools.SpatialValidation.ValidateConnectorsSpatially },
                {1, Shared.Tools.ElementCoordinates.ElementCoordinatesPCF },
                {2, Shared.Tools.GetElementByUCI.GetEByUCI },
                {3, Shared.Tools.GetElementsUCI.GetEsUCI },
            };

            //Place names for methods in this dict
            nameDict = new Dictionary<int, string>
            {
                {0, "Spatial validation." },
                {1, "(ctrl) Coords PCF" },
                {2, "Get E by UCI" },
                {3, "Get UCIs from selection" }
            };

            for (int i = 0; i < methodDict.Count; i++)
            {
                var b = new Button
                {
                    Text = nameDict[i],
                    Name = string.Format("b_{0}", i)
                };
                b.Click += B_Click;
                b.Dock = DockStyle.Fill;
                b.AutoSizeMode = 0;
                tableLayoutPanel1.Controls.Add(b);
            }
        }

        public Tools(int x, int y) : this()
        {
            desiredStartLocationX = x;
            desiredStartLocationY = y;

            Load += new EventHandler(Tools_Load);
        }

        private void B_Click(object sender, EventArgs e)
        {
            var b = sender as Button;
            var position = tableLayoutPanel1.GetPositionFromControl(b);
            var index = position.Row;
            MethodToExecute = methodDict[index];
            Close();
        }

        private void Tools_Load(object sender, EventArgs e)
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

        //StringBuilder sb = new StringBuilder();
        //foreach (var f in query)
        //{
        //    sb.AppendLine(f.Name);
        //}
        //ut.InfoMsg(sb.ToString());
    }
}
