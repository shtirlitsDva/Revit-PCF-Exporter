using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Windows.Forms;
using System.Diagnostics;
using System.IO;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Plumbing;
using Autodesk.Revit.UI;
using Shared.BuildingCoder;
using PCF_Parameters;
using PCF_Functions;
using Microsoft.WindowsAPICodePack.Dialogs;
using mySettings = PCF_Exporter.Properties.Settings;
using iv = PCF_Functions.InputVars;
using dh = Shared.DataHandler;

namespace PCF_Exporter
{
    [Autodesk.Revit.Attributes.Transaction(Autodesk.Revit.Attributes.TransactionMode.Manual)]
    public partial class PCF_Exporter_form : System.Windows.Forms.Form
    {
        static ExternalCommandData _commandData;
        static UIApplication _uiapp;
        static UIDocument _uidoc;
        static Document _doc;
        private string _message;

        private List<string> pipeLinesAbbreviations;

        private string _excelPath = null;
        private string _LDTPath = null;

        private DataSet dataSetElements = null;
        public static DataTable dataTableElements = null;

        private DataSet dataSetPipelines = null;
        public static DataTable dataTablePipelines = null;

        private Properties.Settings _mySets;

        public PCF_Exporter_form(ExternalCommandData cData, string message)
        {
            InitializeComponent();
            _mySets = mySettings.Default;

            _commandData = cData;
            _uiapp = _commandData.Application;
            _uidoc = _uiapp.ActiveUIDocument;
            _doc = _uidoc.Document;
            _message = message;

            try
            {
                //Init excel path
                _excelPath = mySettings.Default.excelPath;
                textBox20.Text = _excelPath;
                if (!string.IsNullOrEmpty(_excelPath) && File.Exists(_excelPath))
                {
                    dataSetElements = dh.ImportExcelToDataSet(_excelPath, "YES");
                    dataTableElements = dh.ReadDataTable(dataSetElements.Tables, "Elements");
                }
            }
            catch (Exception) { }
            try
            {
                //Init LDT path
                _LDTPath = mySettings.Default.LDTPath;
                textBox7.Text = _LDTPath;
                if (!string.IsNullOrEmpty(_LDTPath) && File.Exists(_LDTPath))
                {
                    dataSetPipelines = dh.ImportExcelToDataSet(_LDTPath, "YES");
                    dataTablePipelines = dh.ReadDataTable(dataSetPipelines.Tables, "Pipelines");
                }
            }
            catch (Exception) { }

            //Init PROJECT-IDENTIFIER
            //textBox11.Text = mySettings.Default.TextBox11PROJECTIDENTIFIER;
            iv.PCF_PROJECT_IDENTIFIER = mySettings.Default.TextBox11PROJECTIDENTIFIER;

            //Init Scope
            //Gather all physical piping systems and collect distinct abbreviations
            pipeLinesAbbreviations = Shared.MepUtils.GetDistinctPhysicalPipingSystemTypeNames(_doc);

            comboBox2.SelectedIndexChanged -= comboBox2_SelectedIndexChanged;
            //Use the distinct abbreviations as data source for the comboBox
            comboBox2.DataSource = pipeLinesAbbreviations;

            //Set the previous sysAbbr
            if (pipeLinesAbbreviations.Contains(mySettings.Default.selectedSysAbbr))
                comboBox2.SelectedIndex = pipeLinesAbbreviations.IndexOf(
                    mySettings.Default.selectedSysAbbr);
            comboBox2.SelectedIndexChanged += comboBox2_SelectedIndexChanged;

            iv.ExportAllOneFile = mySettings.Default.radioButton1AllPipelines;
            iv.ExportAllSepFiles = mySettings.Default.radioButton13AllPipelinesSeparate;
            iv.ExportSpecificPipeLine = mySettings.Default.radioButton2SpecificPipeline;
            iv.ExportSelection = mySettings.Default.radioButton14ExportSelection;
            if (!iv.ExportSpecificPipeLine)
            {
                comboBox2.Visible = false;
                textBox4.Visible = false;
            }

            //Init Bore
            iv.UNITS_BORE_MM = mySettings.Default.radioButton3BoreMM;
            iv.UNITS_BORE_INCH = mySettings.Default.radioButton4BoreINCH;
            iv.UNITS_BORE = iv.UNITS_BORE_MM ? "MM" : "INCH";

            //Init cooords
            iv.UNITS_CO_ORDS_MM = mySettings.Default.radioButton5CoordsMm;
            iv.UNITS_CO_ORDS_INCH = mySettings.Default.radioButton6CoordsInch;
            iv.UNITS_CO_ORDS = iv.UNITS_CO_ORDS_MM ? "MM" : "INCH";

            //Init weight
            iv.UNITS_WEIGHT_KGS = mySettings.Default.radioButton7WeightKgs;
            iv.UNITS_WEIGHT_LBS = mySettings.Default.radioButton8WeightLbs;
            iv.UNITS_WEIGHT = iv.UNITS_WEIGHT_KGS ? "KGS" : "LBS";

            //Init weight-length
            iv.UNITS_WEIGHT_LENGTH_METER = mySettings.Default.radioButton9WeightLengthM;
            iv.UNITS_WEIGHT_LENGTH_FEET = mySettings.Default.radioButton10WeightLengthF;
            iv.UNITS_WEIGHT_LENGTH = iv.UNITS_WEIGHT_LENGTH_METER ? "METER" : "FEET";

            //Init output path
            iv.OutputDirectoryFilePath = mySettings.Default.textBox5OutputPath;
            textBox5.Text = iv.OutputDirectoryFilePath;

            //Init diameter limit
            iv.DiameterLimit = double.Parse(mySettings.Default.textBox22DiameterLimit);

            //Init PCF_ELEM_SPEC filter
            iv.PCF_ELEM_SPEC_FILTER = mySettings.Default.TextBoxFilterPCF_ELEM_SPEC;
            textBox9.Text = iv.PCF_ELEM_SPEC_FILTER;

            //Init write wall thickness
            iv.WriteWallThickness = mySettings.Default.radioButton12WallThkTrue;

            //Init export to section
            iv.ExportToIsogen = mySettings.Default.checkBox1Checked;
            iv.ExportToCII = mySettings.Default.checkBox2Checked;

            //Init write mode section
            iv.Overwrite = mySettings.Default.radioButton15Overwrite;

            //Init Output Encoding section
            radioButton18.Checked = mySettings.Default.radioButton18ANSIEncoding;
            radioButton17.Checked = mySettings.Default.radioButton17UTF8_BOM;
        }

        private void button4_Click(object sender, EventArgs e)
        {
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                //Get excel file
                _excelPath = openFileDialog1.FileName;
                textBox20.Text = _excelPath;
                //Save excel file to settings
                mySettings.Default.excelPath = _excelPath;

                dataSetElements = dh.ImportExcelToDataSet(_excelPath, "YES");

                dataTableElements = dh.ReadDataTable(dataSetElements.Tables, "Elements");
            }
        }

        private void button13_Click(object sender, EventArgs e)
        {
            if (openFileDialog2.ShowDialog() == DialogResult.OK)
            {
                //Get excel file
                _LDTPath = openFileDialog2.FileName;
                textBox7.Text = _LDTPath;
                //Save excel file to settings
                mySettings.Default.LDTPath = _LDTPath;

                dataSetPipelines = dh.ImportExcelToDataSet(_LDTPath, "YES");

                dataTablePipelines = dh.ReadDataTable(dataSetPipelines.Tables, "Pipelines");
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            CreateParameterBindings CPB = new CreateParameterBindings();
            CPB.CreateElementBindings(_uiapp, ref _message);
            CPB.CreatePipelineBindings(_uiapp, ref _message);
        }

        private void button2_Click(object sender, EventArgs e)
        {
            DeleteParameters DP = new DeleteParameters();
            DP.ExecuteMyCommand(_uiapp, ref _message);
        }

        private void button3_Click(object sender, EventArgs e)
        {
            PopulateParameters PP = new PopulateParameters();
            if (dataTableElements == null)
            {
                BuildingCoderUtilities.ErrorMsg("dataTableElements is null!");
                return;
            }
            PP.PopulateElementData(_uiapp, ref _message, dataTableElements);
        }

        private void button7_Click(object sender, EventArgs e)
        {
            PopulateParameters PP = new PopulateParameters();
            if (dataTablePipelines == null)
            {
                Shared.BuildingCoder.BuildingCoderUtilities.ErrorMsg("dataTablePipelines is null!");
                return;
            }
            PP.PopulatePipelineData(_uiapp, ref _message, dataTablePipelines);
        }

        private void radioButton1_CheckedChanged(object sender, EventArgs e)
        {
            if (!radioButton1.Checked) return;
            iv.ExportAllOneFile = radioButton1.Checked;
            iv.ExportAllSepFiles = !radioButton1.Checked;
            iv.ExportSpecificPipeLine = !radioButton1.Checked;
            iv.ExportSelection = !radioButton1.Checked;
            comboBox2.Visible = false; textBox4.Visible = false;
        }

        private void radioButton13_CheckedChanged(object sender, EventArgs e)
        {
            if (!radioButton13.Checked) return;
            iv.ExportAllOneFile = !radioButton13.Checked;
            iv.ExportAllSepFiles = radioButton13.Checked;
            iv.ExportSpecificPipeLine = !radioButton13.Checked;
            iv.ExportSelection = !radioButton13.Checked;
            comboBox2.Visible = false; textBox4.Visible = false;
        }

        private void radioButton2_CheckedChanged(object sender, EventArgs e)
        {
            if (!radioButton2.Checked) return;
            iv.ExportAllOneFile = !radioButton2.Checked;
            iv.ExportAllSepFiles = !radioButton2.Checked;
            iv.ExportSpecificPipeLine = radioButton2.Checked;
            iv.ExportSelection = !radioButton2.Checked;
            comboBox2.Visible = true; textBox4.Visible = true;
        }

        private void radioButton14_CheckedChanged(object sender, EventArgs e)
        {
            if (!radioButton14.Checked) return;
            iv.ExportAllOneFile = !radioButton14.Checked;
            iv.ExportAllSepFiles = !radioButton14.Checked;
            iv.ExportSpecificPipeLine = !radioButton14.Checked;
            iv.ExportSelection = radioButton14.Checked;
            comboBox2.Visible = false; textBox4.Visible = false;
        }

        private void button5_Click(object sender, EventArgs e)
        {
            CommonOpenFileDialog dialog = new CommonOpenFileDialog
            {
                IsFolderPicker = true
            };
            if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
            {
                iv.OutputDirectoryFilePath = dialog.FileName; //"\\" is added in the output part.
                textBox5.Text = iv.OutputDirectoryFilePath;
                mySettings.Default.textBox5OutputPath = iv.OutputDirectoryFilePath;
            }
            //FolderBrowserDialog fbd = new FolderBrowserDialog();
            //DialogResult result = fbd.ShowDialog();
            //if (result == DialogResult.OK)
            //{
            //    iv.OutputDirectoryFilePath = fbd.SelectedPath;
            //    textBox5.Text = iv.OutputDirectoryFilePath;
            //    mySettings.Default.textBox5OutputPath = iv.OutputDirectoryFilePath;
            //}
        }

        private void button6_Click(object sender, EventArgs e)
        {
            PCFExport pcfExporter = new PCFExport();
            Result result = Result.Failed;

            if (iv.ExportAllOneFile || iv.ExportSpecificPipeLine || iv.ExportSelection)
            {
                result = pcfExporter.ExecuteMyCommand(_uiapp, ref _message);
            }
            else if (iv.ExportAllSepFiles)
            {
                foreach (string name in pipeLinesAbbreviations)
                {
                    iv.SysAbbr = name;
                    result = pcfExporter.ExecuteMyCommand(_uiapp, ref _message);
                }
            }

            if (result == Result.Succeeded) BuildingCoderUtilities.InfoMsg("PCF data exported successfully!");
            if (result == Result.Failed) BuildingCoderUtilities.InfoMsg("PCF data export failed for some reason.");
        }

        private void radioButton3_CheckedChanged(object sender, EventArgs e)
        {
            if (radioButton3.Checked)
            {
                iv.UNITS_BORE_MM = true;
                iv.UNITS_BORE_INCH = false;
                iv.UNITS_BORE = "MM";
            }
        }

        private void radioButton4_CheckedChanged(object sender, EventArgs e)
        {
            if (radioButton4.Checked)
            {
                iv.UNITS_BORE_MM = false;
                iv.UNITS_BORE_INCH = true;
                iv.UNITS_BORE = "INCH";
            }
        }

        private void radioButton5_CheckedChanged(object sender, EventArgs e)
        {
            if (radioButton5.Checked)
            {
                iv.UNITS_CO_ORDS_MM = true;
                iv.UNITS_CO_ORDS_INCH = false;
                iv.UNITS_CO_ORDS = "MM";
            }
        }

        private void radioButton6_CheckedChanged(object sender, EventArgs e)
        {
            if (radioButton6.Checked)
            {
                iv.UNITS_CO_ORDS_MM = false;
                iv.UNITS_CO_ORDS_INCH = true;
                iv.UNITS_CO_ORDS = "INCH";
            }
        }

        private void radioButton7_CheckedChanged(object sender, EventArgs e)
        {
            if (radioButton7.Checked)
            {
                iv.UNITS_WEIGHT_KGS = true;
                iv.UNITS_WEIGHT_LBS = false;
                iv.UNITS_WEIGHT = "KGS";
            }
        }

        private void radioButton8_CheckedChanged(object sender, EventArgs e)
        {
            if (radioButton8.Checked)
            {
                iv.UNITS_WEIGHT_KGS = false;
                iv.UNITS_WEIGHT_LBS = true;
                iv.UNITS_WEIGHT = "LBS";
            }
        }

        private void radioButton9_CheckedChanged(object sender, EventArgs e)
        {
            if (radioButton9.Checked)
            {
                iv.UNITS_WEIGHT_LENGTH_METER = true;
                iv.UNITS_WEIGHT_LENGTH_FEET = false;
                iv.UNITS_WEIGHT_LENGTH = "METER";
            }
        }

        private void radioButton10_CheckedChanged(object sender, EventArgs e)
        {
            if (radioButton10.Checked)
            {
                iv.UNITS_WEIGHT_LENGTH_METER = false;
                iv.UNITS_WEIGHT_LENGTH_FEET = true;
                iv.UNITS_WEIGHT_LENGTH = "FEET";
            }
        }

        private void button8_Click(object sender, EventArgs e)
        {
            ScheduleCreator SC = new ScheduleCreator();
            var output = SC.CreateAllItemsSchedule(_uidoc);

            if (output == Result.Succeeded) BuildingCoderUtilities.InfoMsg("Schedules created successfully!");
            else if (output == Result.Failed) BuildingCoderUtilities.InfoMsg("Schedule creation failed for some reason.");
        }

        private void button9_Click(object sender, EventArgs e)
        {
            ExportParameters EP = new ExportParameters();
            var output = EP.ExecuteMyCommand(_uiapp);
            if (output == Result.Succeeded) BuildingCoderUtilities.InfoMsg("Elements exported to EXCEL successfully!");
            else if (output == Result.Failed) BuildingCoderUtilities.InfoMsg("Element export to EXCEL failed for some reason.");
        }

        private void richTextBox1_LinkClicked(object sender, LinkClickedEventArgs e)
        {
            Process.Start(e.LinkText);
        }

        private void textBox22_TextChanged(object sender, EventArgs e)
        {
            iv.DiameterLimit = double.Parse(textBox22.Text);
        }

        //PCF_ELEM_SPEC filter
        private void TextBox9_TextChanged(object sender, EventArgs e)
        {
            iv.PCF_ELEM_SPEC_FILTER = textBox9.Text;
        }

        private void radioButton12_CheckedChanged(object sender, EventArgs e)
        {
            if (radioButton12.Checked) iv.WriteWallThickness = true;
        }

        private void radioButton11_CheckedChanged(object sender, EventArgs e)
        {
            if (radioButton12.Checked) iv.WriteWallThickness = false;
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            iv.ExportToIsogen = checkBox1.Checked;
        }

        private void checkBox2_CheckedChanged(object sender, EventArgs e)
        {
            iv.ExportToCII = checkBox2.Checked;
        }

        private void comboBox2_SelectedIndexChanged(object sender, EventArgs e)
        {
            iv.SysAbbr = comboBox2.SelectedItem.ToString();
            mySettings.Default.selectedSysAbbr = iv.SysAbbr;
        }

        private void radioButton15_CheckedChanged(object sender, EventArgs e)
        {
            if (radioButton15.Checked) iv.Overwrite = true;
        }

        private void radioButton16_CheckedChanged(object sender, EventArgs e)
        {
            if (radioButton16.Checked) iv.Overwrite = false;
        }

        private void button10_Click(object sender, EventArgs e)
        {
            ExportParameters EP = new ExportParameters();
            EP.ExportUndefinedElements(_uiapp, _doc, _excelPath);
        }

        private void button12_Click(object sender, EventArgs e)
        {
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                //Get excel file
                _LDTPath = openFileDialog1.FileName;
                textBox11.Text = _LDTPath;
                //Save excel file to settings
                mySettings.Default.LDTPath = _LDTPath;
            }
        }

        private void textBox11_TextChanged(object sender, EventArgs e)
        {
            iv.PCF_PROJECT_IDENTIFIER = textBox11.Text;
        }

        private void PCF_Exporter_form_FormClosed(object sender, FormClosedEventArgs e)
        {
            //mySettings.Default.selectedSysAbbr = iv.SysAbbr;
            mySettings.Default.Save();
        }
    }
}