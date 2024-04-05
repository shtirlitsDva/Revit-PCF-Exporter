using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.IO;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.WindowsAPICodePack.Dialogs;
using Shared;
using Autodesk.Revit.UI;
using Autodesk.Revit.DB;

namespace MEPUtils.SupportTools
{
    public partial class SupportUpdater : System.Windows.Forms.Form
    {
        public string pathToTypeTable = string.Empty;
        public string pathToLoadTable = string.Empty;
        private string typeTableFileName = string.Empty;
        private string loadTableFileName = string.Empty;
        BindingList<FileToProcess> filesToProcessCollection = new BindingList<FileToProcess>();
        BindingSource bindingSource = new BindingSource();
        int linesToRemove = 0;
        UIApplication UiApp;

        public SupportUpdater(UIApplication uiApp)
        {
            InitializeComponent();
            UiApp = uiApp;
            pathToTypeTable = Properties.Settings.Default.SU_TypeTablePath;
            pathToLoadTable = Properties.Settings.Default.SU_LoadTablePath;
            typeTableFileName = Properties.Settings.Default.SU_TypeTableFileName;
            loadTableFileName = Properties.Settings.Default.SU_LoadTableFileName;

            if (!pathToTypeTable.IsNullOrEmpty() && File.Exists(pathToTypeTable))
                filesToProcessCollection.Add(new FileToProcess(pathToTypeTable, FileType.Type));
            if (!pathToLoadTable.IsNullOrEmpty() && File.Exists(pathToLoadTable))
                filesToProcessCollection.Add(new FileToProcess(pathToLoadTable, FileType.Load));

            bindingSource.DataSource = filesToProcessCollection;
            comboBox1.DisplayMember = "FileName";
            comboBox1.ValueMember = "Id";
            comboBox1.DataSource = bindingSource;
        }
        /// <summary>
        /// Select type file
        /// </summary>
        private void button1_Click(object sender, EventArgs e)
        {
            CommonOpenFileDialog dialog = new CommonOpenFileDialog();
            if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
            {
                pathToTypeTable = dialog.FileName;
                Properties.Settings.Default.SU_TypeTablePath = pathToTypeTable;

                if (filesToProcessCollection.Any(x => x.Type == FileType.Type))
                {
                    filesToProcessCollection.Remove(
                        filesToProcessCollection.First(x => x.Type == FileType.Type));
                }

                filesToProcessCollection.Add(new FileToProcess(dialog.FileName, FileType.Type));
                //Update the richtextbox
                comboBox1_SelectedIndexChanged(null, null);
            }
        }
        /// <summary>
        /// Select load file
        /// </summary>
        private void button2_Click(object sender, EventArgs e)
        {
            CommonOpenFileDialog dialog = new CommonOpenFileDialog();
            if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
            {
                pathToLoadTable = dialog.FileName;
                Properties.Settings.Default.SU_LoadTablePath = pathToLoadTable;

                if (filesToProcessCollection.Any(x => x.Type == FileType.Load))
                {
                    filesToProcessCollection.Remove(
                        filesToProcessCollection.First(x => x.Type == FileType.Load));
                }

                filesToProcessCollection.Add(new FileToProcess(dialog.FileName, FileType.Load));
                //Update the richtextbox
                comboBox1_SelectedIndexChanged(null, null);
            }
        }
        /// <summary>
        /// Remove lines
        /// </summary>
        private void button3_Click(object sender, EventArgs e)
        {
            FileToProcess ftp = (FileToProcess)comboBox1.SelectedItem;

            for (int i = 0; i < linesToRemove; i++)
            {
                File_DeleteLine(1, ftp.Path);
            }

            //Update the richtextbox
            comboBox1_SelectedIndexChanged(null, null);
        }
        /// <summary>
        /// Compare data
        /// </summary>
        private void button4_Click_1(object sender, EventArgs e)
        {
            if (filesToProcessCollection.Any(x => x.Type == FileType.Type &&
                filesToProcessCollection.Any(x => x.Type == FileType.Load)))
            {
                FileToProcess ftpType = filesToProcessCollection.First(x => x.Type == FileType.Type);
                FileToProcess ftpLoad = filesToProcessCollection.First(x => x.Type == FileType.Load);
                MEPUtils.SupportTools.CompareDataWithR2.Compare(
                    UiApp, ftpType.Path, ftpLoad.Path);
            }
        }
        /// <summary>
        /// Update load data
        /// </summary>
        private void button4_Click(object sender, EventArgs e)
        {
            MEPUtils.SupportTools.UpdateDataWithR2.Update(UiApp, pathToTypeTable, pathToLoadTable);
        }

        private void SupportUpdater_FormClosing(object sender, FormClosingEventArgs e)
        {
            MEPUtils.Properties.Settings.Default.Save();
        }

        public class FileToProcess
        {
            public int Id
            {
                get
                {
                    switch (Type)
                    {
                        case FileType.None:
                            return 0;
                        case FileType.Type:
                            return 1;
                        case FileType.Load:
                            return 2;
                        default:
                            return 0;
                    }
                }
            }
            public string Path { get; set; }
            public string FileName { get; set; }
            public FileType Type { get; set; }
            public FileToProcess(string path, FileType type)
            {
                Path = path;
                FileName = System.IO.Path.GetFileName(path);
                Type = type;
            }
        }

        public enum FileType
        {
            None,
            Type,
            Load
        }

        private void SupportUpdater_Load(object sender, EventArgs e)
        {
            maskedTextBox1.MaskInputRejected += new MaskInputRejectedEventHandler(maskedTextBox1_MaskInputRejected);
            maskedTextBox1.KeyDown += new KeyEventHandler(maskedTextBox1_KeyDown);
        }

        void maskedTextBox1_MaskInputRejected(object sender, MaskInputRejectedEventArgs e)
        {
            if (maskedTextBox1.MaskFull)
            {
                toolTip1.ToolTipTitle = "Input Rejected - Too Much Data";
                toolTip1.Show("You cannot enter any more data into the date field. Delete some characters in order to insert more data.", maskedTextBox1, 0, -20, 5000);
            }
            else if (e.Position == maskedTextBox1.Mask.Length)
            {
                toolTip1.ToolTipTitle = "Input Rejected - End of Field";
                toolTip1.Show("You cannot add extra characters to the end of this date field.", maskedTextBox1, 0, -20, 5000);
            }
            else
            {
                toolTip1.ToolTipTitle = "Input Rejected";
                toolTip1.Show("You can only add numeric characters (0-9) into this date field.", maskedTextBox1, 0, -20, 5000);
            }
        }

        void maskedTextBox1_KeyDown(object sender, KeyEventArgs e)
        {
            // The balloon tip is visible for five seconds; if the user types any data before it disappears, collapse it ourselves.
            toolTip1.Hide(maskedTextBox1);
        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            FileToProcess ftp = (FileToProcess)comboBox1.SelectedItem;
            string[] lines = File.ReadAllLines(ftp.Path);
            var first10 = lines.Take(10).ToArray();
            for (int i = 0; i < first10.Length; i++)
            {
                first10[i] = $"{i + 1}: {first10[i]}";
            }
            richTextBox1.Text = string.Join("\n", first10);
        }

        private void maskedTextBox1_TextChanged(object sender, EventArgs e)
        {
            int.TryParse(maskedTextBox1.Text, out linesToRemove);
        }

        #region LocalHelperFunctions
        void File_DeleteLine(int Line, string Path)
        {
            StringBuilder sb = new StringBuilder();
            using (StreamReader sr = new StreamReader(Path))
            {
                int Countup = 0;
                while (!sr.EndOfStream)
                {
                    Countup++;
                    if (Countup != Line)
                    {
                        using (StringWriter sw = new StringWriter(sb))
                        {
                            sw.WriteLine(sr.ReadLine());
                        }
                    }
                    else
                    {
                        sr.ReadLine();
                    }
                }
            }
            using (StreamWriter sw = new StreamWriter(Path))
            {
                sw.Write(sb.ToString());
            }
        }
        #endregion
    }
}
