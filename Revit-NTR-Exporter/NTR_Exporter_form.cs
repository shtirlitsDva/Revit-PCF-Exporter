﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Windows.Forms;
using System.Diagnostics;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Plumbing;
using Autodesk.Revit.UI;
using BuildingCoder;
using PCF_Parameters;
using PCF_Functions;
using mySettings = NTR_Exporter.Properties.Settings;
using iv = NTR_Functions.InputVars;
using dh = PCF_Functions.DataHandler;

namespace NTR_Exporter
{
    [Autodesk.Revit.Attributes.Transaction(Autodesk.Revit.Attributes.TransactionMode.Manual)]
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
            //textBox20.Text = _excelPath;

            //Init output path
            _outputDirectoryFilePath = mySettings.Default.textBox5OutputPath;
            if (!string.IsNullOrEmpty(_outputDirectoryFilePath)) iv.OutputDirectoryFilePath = mySettings.Default.textBox5OutputPath;
            textBox5.Text = iv.OutputDirectoryFilePath;

            //Init Scope
            //Gather all physical piping systems and collect distinct abbreviations
            pipeLinesAbbreviations = MepUtils.GetDistinctPhysicalPipingSystemTypeNames(_doc);

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
            iv.DiameterLimit = double.Parse(mySettings.Default.textBox22DiameterLimit);

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

        private void button1_Click(object sender, EventArgs e)
        {
            //CreateParameterBindings CPB = new CreateParameterBindings();
            //CPB.CreateElementBindings(_uiapp, ref _message);
            //CPB.CreatePipelineBindings(_uiapp, ref _message);
        }

        private void button2_Click(object sender, EventArgs e)
        {
            //DeleteParameters DP = new DeleteParameters();
            //DP.ExecuteMyCommand(_uiapp, ref _message);
        }

        private void button3_Click(object sender, EventArgs e)
        {
            //PopulateParameters PP = new PopulateParameters();
            //PP.PopulateElementData(_uiapp, ref _message, _excelPath);
        }

        private void button7_Click(object sender, EventArgs e)
        {
            //PopulateParameters PP = new PopulateParameters();
            //PP.PopulatePipelineData(_uiapp, ref _message, _excelPath);
        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            //iv.ExcelSheet = (string)comboBox1.SelectedItem;
            ////mySettings.Default.excelWorksheetSelectedName = iv.ExcelSheet;
            //DATA_TABLE = DATA_SET.Tables[iv.ExcelSheet];
            //ParameterData.parameterNames = null;
            //ParameterData.parameterNames = (from dc in DATA_TABLE.Columns.Cast<DataColumn>() select dc.ColumnName).ToList();
            //ParameterData.parameterNames.RemoveAt(0);
            //Util.InfoMsg("Following parameters will be initialized:\n" + string.Join("\n", ParameterData.parameterNames.ToArray()));
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

        private void button6_Click(object sender, EventArgs e)
        {
            //PCFExport pcfExporter = new PCFExport();
            //Result result = Result.Failed;

            if (iv.ExportAllOneFile || iv.ExportSpecificPipeLine || iv.ExportSelection)
            {
                //result = pcfExporter.ExecuteMyCommand(_uiapp, ref _message);
            }
            else if (iv.ExportAllSepFiles)
            {
                foreach (string name in pipeLinesAbbreviations)
                {
                    iv.SysAbbr = name;
                    //result = pcfExporter.ExecuteMyCommand(_uiapp, ref _message);
                }
            }

            //if (result == Result.Succeeded) Util.InfoMsg("PCF data exported successfully!");
            //if (result == Result.Failed) Util.InfoMsg("PCF data export failed for some reason.");
        }

        private void radioButton3_CheckedChanged(object sender, EventArgs e)
        {
            if (radioButton3.Checked)
            {
            }
        }

        private void radioButton4_CheckedChanged(object sender, EventArgs e)
        {
            if (radioButton4.Checked)
            {
            }
        }

        private void radioButton5_CheckedChanged(object sender, EventArgs e)
        {
            if (radioButton5.Checked)
            {
            }
        }

        private void radioButton6_CheckedChanged(object sender, EventArgs e)
        {
            if (radioButton6.Checked)
            {
            }
        }

        private void radioButton7_CheckedChanged(object sender, EventArgs e)
        {
            if (radioButton7.Checked)
            {
            }
        }

        private void radioButton8_CheckedChanged(object sender, EventArgs e)
        {
            if (radioButton8.Checked)
            {
            }
        }

        private void radioButton9_CheckedChanged(object sender, EventArgs e)
        {
            if (radioButton9.Checked)
            {
            }
        }

        private void radioButton10_CheckedChanged(object sender, EventArgs e)
        {
            if (radioButton10.Checked)
            {
            }
        }

        private void button8_Click(object sender, EventArgs e)
        {
            ScheduleCreator SC = new ScheduleCreator();
            var output = SC.CreateAllItemsSchedule(_uidoc);

            if (output == Result.Succeeded) Util.InfoMsg("Schedules created successfully!");
            else if (output == Result.Failed) Util.InfoMsg("Schedule creation failed for some reason.");
        }

        private void button9_Click(object sender, EventArgs e)
        {
            //ExportParameters EP = new ExportParameters();
            //var output = EP.ExecuteMyCommand(_uiapp);
            //if (output == Result.Succeeded) Util.InfoMsg("Elements exported to EXCEL successfully!");
            //else if (output == Result.Failed) Util.InfoMsg("Element export to EXCEL failed for some reason.");
        }

        private void richTextBox1_LinkClicked(object sender, LinkClickedEventArgs e)
        {
            Process.Start(e.LinkText);
        }

        private void textBox22_TextChanged(object sender, EventArgs e)
        {
            iv.DiameterLimit = double.Parse(textBox22.Text);
        }

        private void radioButton12_CheckedChanged(object sender, EventArgs e)
        {
            //if (radioButton12.Checked) iv.WriteWallThickness = true;
        }

        private void radioButton11_CheckedChanged(object sender, EventArgs e)
        {
            //if (radioButton12.Checked) iv.WriteWallThickness = false;
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            //iv.ExportToPlant3DIso = checkBox1.Checked;
        }

        private void checkBox2_CheckedChanged(object sender, EventArgs e)
        {
            //iv.ExportToCII = checkBox2.Checked;
        }

        private void comboBox2_SelectedIndexChanged(object sender, EventArgs e)
        {
            iv.SysAbbr = comboBox2.SelectedItem.ToString();
        }
    }
}