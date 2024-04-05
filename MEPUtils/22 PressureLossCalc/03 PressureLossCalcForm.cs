using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

using Autodesk.Revit.UI;
using Autodesk.Revit.DB;

using mySettings = MEPUtils.Properties.Settings;
using Form = System.Windows.Forms.Form;

namespace MEPUtils.PressureLossCalc
{
    public partial class PressureLossCalcForm : Form
    {
        Document doc { get; set; }
        double TempSupply { get; set; }
        double TempReturn { get; set; }
        double Power { get; set; }
        private CultureInfo culture = CultureInfo.CreateSpecificCulture("da-DK");

        public PressureLossCalcForm(ExternalCommandData externalCommandData)
        {
            InitializeComponent();

            textBox_InputPower.Text = mySettings.Default.InputPowerText;
            textBox_InputTempSupply.Text = mySettings.Default.InputTempSupplyText;
            textBox_InputTempReturn.Text = mySettings.Default.InputTempReturnText;

            this.doc = externalCommandData.Application.ActiveUIDocument.Document;
        }

        private void textBox_PowerInput_TextChanged(object sender, EventArgs e)
        {
            this.Power = parseNumber(textBox_InputPower.Text);

            textBox_DisplayPower.Text = this.Power.ToString();

            calculateFlow(Power, TempSupply, TempReturn);
        }

        private void textBox_InputTempSupply_TextChanged(object sender, EventArgs e)
        {
            this.TempSupply = parseNumber(textBox_InputTempSupply.Text);

            textBox_DisplayTempSupply.Text = this.TempSupply.ToString();

            calculateFlow(Power, TempSupply, TempReturn);
        }

        private void textBox_InputTempReturn_TextChanged(object sender, EventArgs e)
        {
            this.TempReturn = parseNumber(textBox_InputTempReturn.Text);

            textBox_DisplayTempReturn.Text = this.TempReturn.ToString();

            calculateFlow(Power, TempSupply, TempReturn);
        }
        private void calculateFlow(double power, double tF, double tR)
        {
            CalcPressureLoss.currentFlow = CalcFlow.calculateFlow(power, tF, tR);
            textBox_DisplayFlow.Text = Math.Round(
                    CalcPressureLoss.currentFlow, 2, MidpointRounding.AwayFromZero)
                .ToString();
                //.ToString("#,##");
        }
        private double parseNumber(string input)
        {
            double power = 0;

            if (double.TryParse(
                input,
                NumberStyles.Number,
                this.culture,
                out power))
            {
                return power;
            }
            else return 0;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            RevitInteraction.PrintSegmentInfo(this.doc);
        }
    }
}
