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
using DarkUI.Forms;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.Button;

namespace PCF_Exporter
{
    public partial class Dark_PCF_Exporter_form : DarkForm
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

        public Dark_PCF_Exporter_form(ExternalCommandData cData, string message)
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
                darkTextBox20.Text = _excelPath;
                if (!string.IsNullOrEmpty(_excelPath) && File.Exists(_excelPath))
                {
                    dataSetElements = dh.ReadExcelToDataSet(_excelPath);
                    //dataSetElements = dh.ImportExcelToDataSet(_excelPath, "YES");
                    dataTableElements = dh.ReadDataTable(dataSetElements, "Elements");
                }
            }
            catch (Exception)
            {
                darkTextBox20.Text = "Reading Excel Elements file threw an exception!";
            }
            try
            {
                //Init LDT path
                _LDTPath = mySettings.Default.LDTPath;
                darkTextBox7.Text = _LDTPath;
                if (!string.IsNullOrEmpty(_LDTPath) && File.Exists(_LDTPath))
                {
                    dataSetPipelines = dh.ReadExcelToDataSet(_LDTPath);

                    //dataSetPipelines = dh.ImportExcelToDataSet(_LDTPath, "YES");
                    dataTablePipelines = dh.ReadDataTable(dataSetPipelines, "Pipelines");
                }
            }
            catch (Exception)
            {
                darkTextBox7.Text = "Reading Excel Pipelines file threw an exception!";
            }

            //Init PROJECT-IDENTIFIER
            //textBox11.Text = mySettings.Default.TextBox11PROJECTIDENTIFIER;
            iv.PCF_PROJECT_IDENTIFIER = mySettings.Default.TextBox11PROJECTIDENTIFIER;

            //Init Scope
            //Gather all physical piping systems and collect distinct abbreviations
            pipeLinesAbbreviations = Shared.MepUtils.GetDistinctPhysicalPipingSystemTypeNames(_doc, true);

            darkComboBox2.SelectedIndexChanged -= comboBox2_SelectedIndexChanged;
            //Use the distinct abbreviations as data source for the comboBox
            darkComboBox2.DataSource = pipeLinesAbbreviations;

            //Set the previous sysAbbr
            if (pipeLinesAbbreviations.Contains(mySettings.Default.selectedSysAbbr))
            {
                darkComboBox2.SelectedIndex = pipeLinesAbbreviations.IndexOf(
                    mySettings.Default.selectedSysAbbr);
                iv.SysAbbr = mySettings.Default.selectedSysAbbr;
            }
            else iv.SysAbbr = pipeLinesAbbreviations[0];
            darkComboBox2.SelectedIndexChanged += comboBox2_SelectedIndexChanged;

            iv.ExportAllOneFile = mySettings.Default.radioButton1AllPipelines;
            iv.ExportAllSepFiles = mySettings.Default.radioButton13AllPipelinesSeparate;
            iv.ExportSpecificPipeLine = mySettings.Default.radioButton2SpecificPipeline;
            iv.ExportSelection = mySettings.Default.radioButton14ExportSelection;
            if (!iv.ExportSpecificPipeLine)
            {
                darkComboBox2.Visible = false;
                darkTextBox4.Visible = false;
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
            darkTextBox5.Text = iv.OutputDirectoryFilePath;

            //Init diameter limit
            iv.DiameterLimit = double.Parse(mySettings.Default.textBox22DiameterLimit);

            //Init PCF_ELEM_SPEC filter
            iv.PCF_ELEM_SPEC_FILTER = mySettings.Default.TextBoxFilterPCF_ELEM_SPEC;
            darkTextBox9.Text = iv.PCF_ELEM_SPEC_FILTER;

            //Init write wall thickness
            iv.WriteWallThickness = mySettings.Default.radioButton12WallThkTrue;

            //Init export to section
            iv.ExportToIsogen = mySettings.Default.checkBox1Checked;
            iv.ExportToCII = mySettings.Default.checkBox2Checked;

            //Init write mode section
            iv.Overwrite = mySettings.Default.radioButton15Overwrite;

            //Init Output Encoding section
            darkRadioButton18.Checked = mySettings.Default.radioButton18ANSIEncoding;
            darkRadioButton17.Checked = mySettings.Default.radioButton17UTF8_BOM;
        }

        private void button4_Click(object sender, EventArgs e)
        {
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                //Get excel file
                _excelPath = openFileDialog1.FileName;
                darkTextBox20.Text = _excelPath;
                //Save excel file to settings
                mySettings.Default.excelPath = _excelPath;

                dataSetElements = dh.ReadExcelToDataSet(_excelPath);

                //dataSetElements = dh.ImportExcelToDataSet(_excelPath, "YES");
                dataTableElements = dh.ReadDataTable(dataSetElements, "Elements");
            }
        }

        private void button13_Click(object sender, EventArgs e)
        {
            if (openFileDialog2.ShowDialog() == DialogResult.OK)
            {
                //Get excel file
                _LDTPath = openFileDialog2.FileName;
                darkTextBox7.Text = _LDTPath;
                //Save excel file to settings
                mySettings.Default.LDTPath = _LDTPath;

                dataSetPipelines = dh.ReadExcelToDataSet(_LDTPath);

                //dataSetPipelines = dh.ImportExcelToDataSet(_LDTPath, "YES");
                dataTablePipelines = dh.ReadDataTable(dataSetPipelines, "Pipelines");
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
            if (!darkRadioButton1.Checked) return;
            iv.ExportAllOneFile = darkRadioButton1.Checked;
            iv.ExportAllSepFiles = !darkRadioButton1.Checked;
            iv.ExportSpecificPipeLine = !darkRadioButton1.Checked;
            iv.ExportSelection = !darkRadioButton1.Checked;
            darkComboBox2.Visible = false; darkTextBox4.Visible = false;
        }

        private void radioButton13_CheckedChanged(object sender, EventArgs e)
        {
            if (!darkRadioButton13.Checked) return;
            iv.ExportAllOneFile = !darkRadioButton13.Checked;
            iv.ExportAllSepFiles = darkRadioButton13.Checked;
            iv.ExportSpecificPipeLine = !darkRadioButton13.Checked;
            iv.ExportSelection = !darkRadioButton13.Checked;
            darkComboBox2.Visible = false; darkTextBox4.Visible = false;
        }

        private void radioButton2_CheckedChanged(object sender, EventArgs e)
        {
            if (!darkRadioButton2.Checked) return;
            iv.ExportAllOneFile = !darkRadioButton2.Checked;
            iv.ExportAllSepFiles = !darkRadioButton2.Checked;
            iv.ExportSpecificPipeLine = darkRadioButton2.Checked;
            iv.ExportSelection = !darkRadioButton2.Checked;
            darkComboBox2.Visible = true; darkTextBox4.Visible = true;
        }

        private void radioButton14_CheckedChanged(object sender, EventArgs e)
        {
            if (!darkRadioButton14.Checked) return;
            iv.ExportAllOneFile = !darkRadioButton14.Checked;
            iv.ExportAllSepFiles = !darkRadioButton14.Checked;
            iv.ExportSpecificPipeLine = !darkRadioButton14.Checked;
            iv.ExportSelection = darkRadioButton14.Checked;
            darkComboBox2.Visible = false; darkTextBox4.Visible = false;
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
                darkTextBox5.Text = iv.OutputDirectoryFilePath;
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
            if (darkRadioButton3.Checked)
            {
                iv.UNITS_BORE_MM = true;
                iv.UNITS_BORE_INCH = false;
                iv.UNITS_BORE = "MM";
            }
        }

        private void radioButton4_CheckedChanged(object sender, EventArgs e)
        {
            if (darkRadioButton4.Checked)
            {
                iv.UNITS_BORE_MM = false;
                iv.UNITS_BORE_INCH = true;
                iv.UNITS_BORE = "INCH";
            }
        }

        private void radioButton5_CheckedChanged(object sender, EventArgs e)
        {
            if (darkRadioButton5.Checked)
            {
                iv.UNITS_CO_ORDS_MM = true;
                iv.UNITS_CO_ORDS_INCH = false;
                iv.UNITS_CO_ORDS = "MM";
            }
        }

        private void radioButton6_CheckedChanged(object sender, EventArgs e)
        {
            if (darkRadioButton6.Checked)
            {
                iv.UNITS_CO_ORDS_MM = false;
                iv.UNITS_CO_ORDS_INCH = true;
                iv.UNITS_CO_ORDS = "INCH";
            }
        }

        private void radioButton7_CheckedChanged(object sender, EventArgs e)
        {
            if (darkRadioButton7.Checked)
            {
                iv.UNITS_WEIGHT_KGS = true;
                iv.UNITS_WEIGHT_LBS = false;
                iv.UNITS_WEIGHT = "KGS";
            }
        }

        private void radioButton8_CheckedChanged(object sender, EventArgs e)
        {
            if (darkRadioButton8.Checked)
            {
                iv.UNITS_WEIGHT_KGS = false;
                iv.UNITS_WEIGHT_LBS = true;
                iv.UNITS_WEIGHT = "LBS";
            }
        }

        private void radioButton9_CheckedChanged(object sender, EventArgs e)
        {
            if (darkRadioButton9.Checked)
            {
                iv.UNITS_WEIGHT_LENGTH_METER = true;
                iv.UNITS_WEIGHT_LENGTH_FEET = false;
                iv.UNITS_WEIGHT_LENGTH = "METER";
            }
        }

        private void radioButton10_CheckedChanged(object sender, EventArgs e)
        {
            if (darkRadioButton10.Checked)
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
            iv.DiameterLimit = double.Parse(darkTextBox22.Text);
        }

        //PCF_ELEM_SPEC filter
        private void TextBox9_TextChanged(object sender, EventArgs e)
        {
            iv.PCF_ELEM_SPEC_FILTER = darkTextBox9.Text;
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            iv.ExportToIsogen = darkCheckBox1.Checked;
        }

        private void checkBox2_CheckedChanged(object sender, EventArgs e)
        {
            iv.ExportToCII = darkCheckBox2.Checked;
        }

        private void comboBox2_SelectedIndexChanged(object sender, EventArgs e)
        {
            iv.SysAbbr = darkComboBox2.SelectedItem.ToString();
            mySettings.Default.selectedSysAbbr = iv.SysAbbr;
        }

        private void radioButton15_CheckedChanged(object sender, EventArgs e)
        {
            if (darkRadioButton15.Checked) iv.Overwrite = true;
        }

        private void radioButton16_CheckedChanged(object sender, EventArgs e)
        {
            if (darkRadioButton16.Checked) iv.Overwrite = false;
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
                darkTextBox11.Text = _LDTPath;
                //Save excel file to settings
                mySettings.Default.LDTPath = _LDTPath;
            }
        }

        private void textBox11_TextChanged(object sender, EventArgs e)
        {
            iv.PCF_PROJECT_IDENTIFIER = darkTextBox11.Text;
        }

        private void PCF_Exporter_form_FormClosed(object sender, FormClosedEventArgs e)
        {
            //mySettings.Default.selectedSysAbbr = iv.SysAbbr;
            mySettings.Default.Save();
        }
    }
}
