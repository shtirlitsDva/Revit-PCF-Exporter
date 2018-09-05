using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Windows.Forms;
using System.Diagnostics;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Plumbing;
using Autodesk.Revit.UI;
using Shared.BuildingCoder;

using mySettings = NTR_Exporter.Properties.Settings;
using NTR_Functions;
using iv = NTR_Functions.InputVars;


namespace NTR_Exporter
{
    public partial class NTR_Exporter_form : System.Windows.Forms.Form
    {
        static ExternalCommandData _commandData;
        static UIApplication _uiapp;
        static UIDocument _uidoc;
        static Document _doc;
        private string _message;

        private IList<string> pipeLinesAbbreviations;

        private string _excelPath = null;
        private string _outputDirectoryFilePath = null;

        public NTR_Exporter_form(ExternalCommandData cData, string message)
        {
            InitializeComponent();
            _commandData = cData;
            _uiapp = _commandData.Application;
            _uidoc = _uiapp.ActiveUIDocument;
            _doc = _uidoc.Document;
            _message = message;

            //Init excel path
            _excelPath = mySettings.Default.excelPath;
            if (!string.IsNullOrEmpty(_excelPath)) iv.ExcelPath = _excelPath;
            textBox20.Text = _excelPath;

            //Init output path
            _outputDirectoryFilePath = mySettings.Default.textBox5OutputPath;
            if (!string.IsNullOrEmpty(_outputDirectoryFilePath)) iv.OutputDirectoryFilePath = mySettings.Default.textBox5OutputPath;
            textBox5.Text = iv.OutputDirectoryFilePath;

            //Init Scope
            //Gather all physical piping systems and collect distinct abbreviations
            pipeLinesAbbreviations = Shared.MepUtils.GetDistinctPhysicalPipingSystemTypeNames(_doc);

            //Use the distinct abbreviations as data source for the comboBox
            comboBox2.DataSource = pipeLinesAbbreviations;

            iv.ExportAllOneFile = mySettings.Default.radioButton1AllPipelines;
            iv.ExportAllSepFiles = mySettings.Default.radioButton13AllPipelinesSeparate;
            iv.ExportSpecificPipeLine = mySettings.Default.radioButton2SpecificPipeline;
            iv.ExportSelection = mySettings.Default.radioButton14ExportSelection;
            if (!iv.ExportSpecificPipeLine)
            {
                comboBox2.Visible = false;
                textBox4.Visible = false;
            }

            //Init diameter limit
            iv.DiameterLimitGreaterOrEqThan = double.Parse(mySettings.Default.textBox22DiameterLimit);
            iv.DiameterLimitLessOrEqThan = double.Parse(mySettings.Default.textBox3DiameterLessThan);

        }

        private void Button4_Click(object sender, EventArgs e)
        {
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                //Get excel file
                _excelPath = openFileDialog1.FileName;
                textBox20.Text = _excelPath;
                //Save excel file to settings
                mySettings.Default.excelPath = _excelPath;

                iv.ExcelPath = _excelPath;

                //DATA_SET = dh.ImportExcelToDataSet(_excelPath);

                //DataTableCollection PCF_DATA_TABLES = DATA_SET.Tables;

                //PCF_DATA_TABLE_NAMES.Clear();

                //foreach (DataTable dt in PCF_DATA_TABLES)
                //{
                //    PCF_DATA_TABLE_NAMES.Add(dt.TableName);
                //}
                ////excelReader.Close();
                //comboBox1.DataSource = PCF_DATA_TABLE_NAMES;
            }
        }

        private void Button1_Click(object sender, EventArgs e)
        {
            //CreateParameterBindings CPB = new CreateParameterBindings();
            //CPB.CreateElementBindings(_uiapp, ref _message);
            //CPB.CreatePipelineBindings(_uiapp, ref _message);
        }

        private void Button2_Click(object sender, EventArgs e)
        {
            //DeleteParameters DP = new DeleteParameters();
            //DP.ExecuteMyCommand(_uiapp, ref _message);
        }

        private void Button3_Click(object sender, EventArgs e)
        {
            //PopulateParameters PP = new PopulateParameters();
            //PP.PopulateElementData(_uiapp, ref _message, _excelPath);
        }

        private void Button7_Click(object sender, EventArgs e)
        {
            //PopulateParameters PP = new PopulateParameters();
            //PP.PopulatePipelineData(_uiapp, ref _message, _excelPath);
        }

        private void ComboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            //iv.ExcelSheet = (string)comboBox1.SelectedItem;
            ////mySettings.Default.excelWorksheetSelectedName = iv.ExcelSheet;
            //DATA_TABLE = DATA_SET.Tables[iv.ExcelSheet];
            //ParameterData.parameterNames = null;
            //ParameterData.parameterNames = (from dc in DATA_TABLE.Columns.Cast<DataColumn>() select dc.ColumnName).ToList();
            //ParameterData.parameterNames.RemoveAt(0);
            //Util.InfoMsg("Following parameters will be initialized:\n" + string.Join("\n", ParameterData.parameterNames.ToArray()));
        }

        private void RadioButton1_CheckedChanged(object sender, EventArgs e)
        {
            if (!radioButton1.Checked) return;
            iv.ExportAllOneFile = radioButton1.Checked;
            iv.ExportAllSepFiles = !radioButton1.Checked;
            iv.ExportSpecificPipeLine = !radioButton1.Checked;
            iv.ExportSelection = !radioButton1.Checked;
            comboBox2.Visible = false; textBox4.Visible = false;
        }

        private void RadioButton13_CheckedChanged(object sender, EventArgs e)
        {
            if (!radioButton13.Checked) return;
            iv.ExportAllOneFile = !radioButton13.Checked;
            iv.ExportAllSepFiles = radioButton13.Checked;
            iv.ExportSpecificPipeLine = !radioButton13.Checked;
            iv.ExportSelection = !radioButton13.Checked;
            comboBox2.Visible = false; textBox4.Visible = false;
        }

        private void RadioButton2_CheckedChanged(object sender, EventArgs e)
        {
            if (!radioButton2.Checked) return;
            iv.ExportAllOneFile = !radioButton2.Checked;
            iv.ExportAllSepFiles = !radioButton2.Checked;
            iv.ExportSpecificPipeLine = radioButton2.Checked;
            iv.ExportSelection = !radioButton2.Checked;
            comboBox2.Visible = true; textBox4.Visible = true;
        }

        private void RadioButton14_CheckedChanged(object sender, EventArgs e)
        {
            if (!radioButton14.Checked) return;
            iv.ExportAllOneFile = !radioButton14.Checked;
            iv.ExportAllSepFiles = !radioButton14.Checked;
            iv.ExportSpecificPipeLine = !radioButton14.Checked;
            iv.ExportSelection = radioButton14.Checked;
            comboBox2.Visible = false; textBox4.Visible = false;
        }

        private void Button5_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog fbd = new FolderBrowserDialog();
            DialogResult result = fbd.ShowDialog();
            if (result == DialogResult.OK)
            {
                iv.OutputDirectoryFilePath = fbd.SelectedPath;
                _outputDirectoryFilePath = iv.OutputDirectoryFilePath;
                textBox5.Text = iv.OutputDirectoryFilePath;
                mySettings.Default.textBox5OutputPath = iv.OutputDirectoryFilePath;
            }
        }

        private void Button6_Click(object sender, EventArgs e)
        {
            NTR_Exporter exporter = new NTR_Exporter();

            Result result = Result.Failed;

            if (iv.ExportAllOneFile || iv.ExportSpecificPipeLine || iv.ExportSelection)
            {
                result = exporter.ExportNtr(_commandData);
            }
            else if (iv.ExportAllSepFiles)
            {
                foreach (string name in pipeLinesAbbreviations)
                {
                    iv.SysAbbr = name;
                    result = exporter.ExportNtr(_commandData);
                }
            }

            if (result == Result.Succeeded) Util.InfoMsg("NTR data exported successfully!");
            if (result == Result.Failed) Util.InfoMsg("NTR data export failed for some reason.");
        }

        private void RadioButton3_CheckedChanged(object sender, EventArgs e)
        {
            if (radioButton3.Checked)
            {
            }
        }

        private void RadioButton4_CheckedChanged(object sender, EventArgs e)
        {
            if (radioButton4.Checked)
            {
            }
        }

        private void RadioButton5_CheckedChanged(object sender, EventArgs e)
        {
            if (radioButton5.Checked)
            {
            }
        }

        private void RadioButton6_CheckedChanged(object sender, EventArgs e)
        {
            if (radioButton6.Checked)
            {
            }
        }

        private void RadioButton7_CheckedChanged(object sender, EventArgs e)
        {
            if (radioButton7.Checked)
            {
            }
        }

        private void RadioButton8_CheckedChanged(object sender, EventArgs e)
        {
            if (radioButton8.Checked)
            {
            }
        }

        private void RadioButton9_CheckedChanged(object sender, EventArgs e)
        {
            if (radioButton9.Checked)
            {
            }
        }

        private void RadioButton10_CheckedChanged(object sender, EventArgs e)
        {
            if (radioButton10.Checked)
            {
            }
        }

        private void Button8_Click(object sender, EventArgs e)
        {
        }

        private void Button9_Click(object sender, EventArgs e)
        {
            NTR_Excel excel = new NTR_Excel();
            excel.ExportUndefinedElements(_doc);
            //ExportParameters EP = new ExportParameters();
            //var output = EP.ExecuteMyCommand(_uiapp);
            //if (output == Result.Succeeded) Util.InfoMsg("Elements exported to EXCEL successfully!");
            //else if (output == Result.Failed) Util.InfoMsg("Element export to EXCEL failed for some reason.");
        }

        private void RichTextBox1_LinkClicked(object sender, LinkClickedEventArgs e)
        {
            Process.Start(e.LinkText);
        }

        //Diamiter limit control events
        private void TextBox22_TextChanged(object sender, EventArgs e)
        {
            iv.DiameterLimitGreaterOrEqThan = double.Parse(textBox22.Text);
        }

        private void textBox3_TextChanged(object sender, EventArgs e)
        {
            iv.DiameterLimitLessOrEqThan = double.Parse(textBox3.Text);
        }

        private void RadioButton12_CheckedChanged(object sender, EventArgs e)
        {
            //if (radioButton12.Checked) iv.WriteWallThickness = true;
        }

        private void RadioButton11_CheckedChanged(object sender, EventArgs e)
        {
            //if (radioButton12.Checked) iv.WriteWallThickness = false;
        }

        private void CheckBox1_CheckedChanged(object sender, EventArgs e)
        {
            //iv.ExportToPlant3DIso = checkBox1.Checked;
        }

        private void CheckBox2_CheckedChanged(object sender, EventArgs e)
        {
            //iv.ExportToCII = checkBox2.Checked;
        }

        private void ComboBox2_SelectedIndexChanged(object sender, EventArgs e)
        {
            iv.SysAbbr = comboBox2.SelectedItem.ToString();
        }

        
    }
}