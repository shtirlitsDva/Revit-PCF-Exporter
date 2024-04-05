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

namespace MEPUtils.SupportTools
{
    public partial class SupportTools : System.Windows.Forms.Form
    {
        private int desiredStartLocationX;
        private int desiredStartLocationY;

        public Action<UIApplication> ToolToInvoke { get; private set; } = CalculateHeightByLevel.Calculate;
        public bool Cancelled = true;

        public SupportTools()
        {
            InitializeComponent();

            radioButton1.Checked = true;
        }

        private void radioButton1_CheckedChanged(object sender, EventArgs e)
        {
            if (radioButton1.Checked) ToolToInvoke = CalculateHeightByLevel.Calculate;
        }

        private void radioButton2_CheckedChanged(object sender, EventArgs e)
        {
            if (radioButton2.Checked) ToolToInvoke = CalculateHeightBySteelSupport.Calculate;
        }

        private void radioButton3_CheckedChanged(object sender, EventArgs e)
        {
            if (radioButton3.Checked) ToolToInvoke = DetermineCorrectLevel.Execute;
        }

        private void radioButton4_CheckedChanged(object sender, EventArgs e)
        {
            if (radioButton4.Checked) ToolToInvoke = CalculateHeightBySteelOrLevel.Calculate;

        }

        private void radioButton5_CheckedChanged(object sender, EventArgs e)
        {
            if (radioButton5.Checked) ToolToInvoke = UpdateLoadsFromR2Loader.Update;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            Cancelled = false;
            this.Close();
        }

        public SupportTools(int x, int y) : this()
        {
            desiredStartLocationX = x;
            desiredStartLocationY = y;

            Load += new EventHandler(SupportTools_Load);
        }

        private void SupportTools_Load(object sender, EventArgs e)
        {
            SetDesktopLocation(desiredStartLocationX, desiredStartLocationY);
        }
    }
}
