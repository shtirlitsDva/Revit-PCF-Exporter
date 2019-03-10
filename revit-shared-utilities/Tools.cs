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

namespace MEPUtils
{
    public partial class MEPUtilsChooser : System.Windows.Forms.Form
    {
        private Dictionary<int, Func<ExternalCommandData, Result>> methodDict;
        private Dictionary<int, string> nameDict;
        public Func<ExternalCommandData, Result> MethodToExecute { get; private set; }

        private int desiredStartLocationX;
        private int desiredStartLocationY;

        public MEPUtilsChooser()
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
                {0, InsulationHandler.CreateAllInsulation },
                {1, InsulationHandler.DeleteAllPipeInsulation },
                {2, new InsulationHandler().ExecuteInsulationSettings },
                {3, PipeCreator.CreatePipeFromConnector },
                {4, FlangeCreator.CreateFlangeForElements },
                {5, TotalLineLength.TotalLineLengths },
                {6, CreateInstrumentation.StartCreatingInstrumentation.StartCreating },
                {7, PAHangers.CalculateHeight.Calculate },
                {8, MoveToDistance.MoveToDistance.Move },
                {9, new CountWelds.CountWelds().CountWeldsMethod }
            };

            //Place names for methods in this dict
            nameDict = new Dictionary<int, string>
            {
                {0, "Create all insulation" },
                {1, "Delete all insulation" },
                {2, "Insulation settings" },
                {3, "Create pipe from connector" },
                {4, "Create flanges" },
                {5, "Total length of lines" },
                {6, "Create Instrument!" },
                {7, "Hanger height calc" },
                {8, "Move e to distance" },
                {9, "(ctrl) Count welds" }
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

        public MEPUtilsChooser(int x, int y) : this()
        {
            desiredStartLocationX = x;
            desiredStartLocationY = y;

            Load += new EventHandler(MEPUtilsChooser_Load);
        }

        private void B_Click(object sender, EventArgs e)
        {
            var b = sender as Button;
            var position = tableLayoutPanel1.GetPositionFromControl(b);
            var index = position.Row;
            MethodToExecute = methodDict[index];
            Close();
        }

        private void MEPUtilsChooser_Load(object sender, EventArgs e)
        {
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
