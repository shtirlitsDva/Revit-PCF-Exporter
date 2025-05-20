using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PCF_Functions;
using PCF_Parameters;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Windows;
using mySettings = PCF_Exporter.Properties.Settings;
using iv = PCF_Functions.InputVars;
using dh = Shared.DataHandler;
using Microsoft.Win32;
using System.Windows.Forms;

namespace PCF_Exporter.ViewModels
{
    public partial class PcfExporterViewModel : ObservableObject
    {
        private UIApplication? _uiapp;
        public UIApplication UIApp
        {
            get => _uiapp;
            set
            {
                _uiapp = value;
                _uidoc = value?.ActiveUIDocument;
                _doc = value?.ActiveUIDocument?.Document;
            }
        }
        private UIDocument? _uidoc;
        private Document? _doc;
        private string? _message;

        private DataTable? _elementsTable;
        private DataTable? _pipelinesTable;

        public PcfExporterViewModel()
        {
            ExcelPath = mySettings.Default.excelPath;
            if (File.Exists(ExcelPath))
            {
                var ds = dh.ReadExcelToDataSet(ExcelPath);
                _elementsTable = dh.ReadDataTable(ds, "Elements");
            }

            LDTPath = mySettings.Default.LDTPath;
            if (File.Exists(LDTPath))
            {
                var ds = dh.ReadExcelToDataSet(LDTPath);
                _pipelinesTable = dh.ReadDataTable(ds, "Pipelines");
            }

            OutputDirectory = mySettings.Default.textBox5OutputPath;
            Overwrite = mySettings.Default.radioButton15Overwrite;
            Append = mySettings.Default.radioButton16Append;
            iv.PCF_PROJECT_IDENTIFIER = mySettings.Default.TextBox11PROJECTIDENTIFIER;
        }

        [ObservableProperty]
        private string _excelPath = string.Empty;

        partial void OnExcelPathChanged(string value)
        {
            mySettings.Default.excelPath = value;
        }

        [ObservableProperty]
        private string _lDTPath = string.Empty;

        partial void OnLDTPathChanged(string value)
        {
            mySettings.Default.LDTPath = value;
        }

        [ObservableProperty]
        private string _outputDirectory = string.Empty;

        partial void OnOutputDirectoryChanged(string value)
        {
            iv.OutputDirectoryFilePath = value;
            mySettings.Default.textBox5OutputPath = value;
        }

        [ObservableProperty]
        private bool _overwrite = true;

        partial void OnOverwriteChanged(bool value)
        {
            iv.Overwrite = value;
            mySettings.Default.radioButton15Overwrite = value;
            if (value) Append = false;
        }

        [ObservableProperty]
        private bool _append;

        partial void OnAppendChanged(bool value)
        {
            mySettings.Default.radioButton16Append = value;
            if (value) Overwrite = false;
        }

        [RelayCommand]
        private void SelectExcelPath()
        {
            var dialog = new System.Windows.Forms.OpenFileDialog
            {
                Filter = "Excel Files (.xlsx;.xls)|.xlsx;.xls",
                Title = "Select Excel File for Elements"
            };
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                ExcelPath = dialog.FileName;
                var ds = dh.ReadExcelToDataSet(ExcelPath);
                _elementsTable = dh.ReadDataTable(ds, "Elements");
            }
        }

        [RelayCommand]
        private void SelectLdtPath()
        {
            var dialog = new System.Windows.Forms.OpenFileDialog
            {
                Filter = "Excel Files (.xlsx;.xls)|.xlsx;.xls",
                Title = "Select Excel File for LDT (Pipelines)"
            };
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                LDTPath = dialog.FileName;
                var ds = dh.ReadExcelToDataSet(LDTPath);
                _pipelinesTable = dh.ReadDataTable(ds, "Pipelines");
            }
        }

        [RelayCommand]
        private void SelectOutputDirectory()
        {
            using var dialog = new FolderBrowserDialog
            {
                Description = "Select Output Folder",
                UseDescriptionForTitle = true,
                ShowNewFolderButton = true
            };

            if (dialog.ShowDialog() == DialogResult.OK)
            {
                OutputDirectory = dialog.SelectedPath;
            }
        }

        [RelayCommand]
        private void ImportParameters()
        {
            CreateParameterBindings cpb = new CreateParameterBindings();
            cpb.CreateElementBindings(_uiapp, ref _message);
            cpb.CreatePipelineBindings(_uiapp, ref _message);
        }

        [RelayCommand]
        private void DeleteParameters()
        {
            DeleteParameters dp = new DeleteParameters();
            dp.ExecuteMyCommand(_uiapp, ref _message);
        }

        [RelayCommand]
        private void PopulateElements()
        {
            if (_elementsTable == null)
            {
                Debug.WriteLine("Elements table null");
                return;
            }
            PopulateParameters pp = new PopulateParameters();
            pp.PopulateElementData(_uiapp, ref _message, _elementsTable);
        }

        [RelayCommand]
        private void PopulatePipelines()
        {
            if (_pipelinesTable == null)
            {
                Debug.WriteLine("Pipelines table null");
                return;
            }
            PopulateParameters pp = new PopulateParameters();
            pp.PopulatePipelineData(_uiapp, ref _message, _pipelinesTable);
        }

        [RelayCommand]
        private void Export()
        {
            PCFExport exporter = new PCFExport();
            exporter.ExecuteMyCommand(_uiapp, ref _message);
        }
    }
}