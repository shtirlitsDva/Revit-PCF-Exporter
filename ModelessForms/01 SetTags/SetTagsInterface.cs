using System.Data.OleDb;
using System.Text.RegularExpressions;
using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using System.Windows.Input;
using WinForms = System.Windows.Forms;

namespace ModelessForms
{
    public partial class SetTagsInterface : System.Windows.Forms.Form
    {
        string pathToDataFile;

        DataSet dataSet;
        LinkedList<DataRow> linkedListRows = new LinkedList<DataRow>();
        LinkedListNode<DataRow> curNode;

        //Modeless stuff
        private Autodesk.Revit.UI.ExternalEvent m_ExEvent;
        private ExternalEventHandler m_Handler;
        Application ThisApp;

        public SetTagsInterface(Autodesk.Revit.UI.ExternalEvent exEvent,
                                ExternalEventHandler handler,
                                ModelessForms.Application thisApp)
        {
            InitializeComponent();

            m_ExEvent = exEvent;
            m_Handler = handler;
            ThisApp = thisApp;

            pathToDataFile = ModelessForms.Properties.Settings.Default.pathToExcel;
            textBox2.Text = pathToDataFile;
        }

        bool ctrl = false;

        /// <summary>
        /// Loads data from Excel.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button2_Click(object sender, EventArgs e)
        {
            if ((int)Keyboard.Modifiers == 2) ctrl = true;

            if (ctrl || string.IsNullOrEmpty(pathToDataFile))
            {
                System.Windows.Forms.OpenFileDialog dialog = new System.Windows.Forms.OpenFileDialog();
                //CommonOpenFileDialog dialog = new CommonOpenFileDialog();
                if (string.IsNullOrEmpty(pathToDataFile)) dialog.InitialDirectory = Environment.ExpandEnvironmentVariables("%userprofile%");
                else dialog.InitialDirectory = pathToDataFile;
                //if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
                dialog.ShowDialog();
                //{
                pathToDataFile = dialog.FileName;
                ModelessForms.Properties.Settings.Default.pathToExcel = pathToDataFile;
                textBox2.Text = pathToDataFile;
                //}
            }

            if (File.Exists(pathToDataFile))
            {
                dataSet = ImportExcelToDataSet(pathToDataFile, "Yes");
                List<string> dataTableNames = new List<string>();
                foreach (DataTable item in dataSet.Tables) dataTableNames.Add(item.TableName);
                BasicChooserForm form = new BasicChooserForm(dataTableNames);
                form.ShowDialog();
                string sheetName = form.strTR;
                if (string.IsNullOrEmpty(sheetName)) return;
                DataTable dataTable = ReadDataTable(dataSet.Tables, sheetName);
                foreach (DataRow row in dataTable.Rows) linkedListRows.AddLast(row);

                dataGridView1.ColumnCount = dataTable.Columns.Count;
                int i = 0;
                foreach (DataColumn item in dataTable.Columns)
                {
                    dataGridView1.Columns[i].Name = item.ColumnName;
                    i++;
                }
                curNode = linkedListRows.First;
                dataGridView1.Rows.Add(curNode.Value.ItemArray);
                dataGridView1.Rows.Add();

                i = 0;
                foreach (DataColumn item in dataTable.Columns)
                {
                    dataGridView1.Rows[1].Cells[i].Value = "";
                    i++;
                }
            }
            else
            {
                textBox2.Text = "File does not exist!";
                pathToDataFile = "";
            }

        }

        private void SetTagsInterface_FormClosing(object sender, System.Windows.Forms.FormClosingEventArgs e)
        {
            ModelessForms.Properties.Settings.Default.Save();
        }

        /// <summary>
        /// Previous button.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button1_Click(object sender, EventArgs e)
        {
            LinkedListNode<DataRow> prevListNode = curNode.Previous;
            if (prevListNode == null) return;
            curNode = prevListNode;
            dataGridView1.Rows[0].SetValues(curNode.Value.ItemArray);
        }

        /// <summary>
        /// Next button.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button3_Click(object sender, EventArgs e)
        {
            LinkedListNode<DataRow> nextListNode = curNode.Next;
            if (nextListNode == null) return;
            curNode = nextListNode;
            dataGridView1.Rows[0].SetValues(curNode.Value.ItemArray);
        }

        /// <summary>
        /// Update click button event.
        /// </summary>
        private void button4_Click(object sender, EventArgs e)
        {
            AsyncUpdateParameterValues asUPV = new AsyncUpdateParameterValues(dataGridView1);
            ThisApp.asyncCommand = asUPV;
            m_ExEvent.Raise();
        }

        /// <summary>
        /// Find/select button click.
        /// </summary>
        private void Button5_Click(object sender, EventArgs e)
        {
            AsyncFindSelectElement asFSE = new AsyncFindSelectElement(dataGridView1);
            ThisApp.asyncCommand = asFSE;
            m_ExEvent.Raise();
        }

        private void button6_Click(object sender, EventArgs e)
        {
            AsyncFindOldElement asFOE = new AsyncFindOldElement(dataGridView1);
            ThisApp.asyncCommand = asFOE;
            m_ExEvent.Raise();
        }

        public static DataSet ImportExcelToDataSet(string fileName, string dataHasHeaders)
        {
            //On connection strings http://www.connectionstrings.com/excel/#p84
            string connectionString =
                string.Format(
                    "provider=Microsoft.ACE.OLEDB.12.0;Data Source={0};Extended Properties=\"Excel 12.0;HDR={1};IMEX=1\"",
                    fileName, dataHasHeaders);

            DataSet data = new DataSet();

            foreach (string sheetName in GetExcelSheetNames(connectionString))
            {
                using (OleDbConnection con = new OleDbConnection(connectionString))
                {
                    var dataTable = new DataTable();
                    string query = string.Format("SELECT * FROM [{0}]", sheetName);
                    con.Open();
                    OleDbDataAdapter adapter = new OleDbDataAdapter(query, con);
                    adapter.Fill(dataTable);

                    //Remove ' and $ from sheetName
                    Regex rgx = new Regex("[^a-zA-Z0-9 _-]");
                    string tableName = rgx.Replace(sheetName, "");

                    dataTable.TableName = tableName;
                    data.Tables.Add(dataTable);
                }
            }

            if (data == null) ErrorMsg("Data set is null");
            if (data.Tables.Count < 1) ErrorMsg("Table count in DataSet is 0");

            return data;
        }

        static string[] GetExcelSheetNames(string connectionString)
        {
            //OleDbConnection con = null;
            DataTable dt = null;
            using (OleDbConnection con = new OleDbConnection(connectionString))
            {
                con.Open();
                dt = con.GetOleDbSchemaTable(OleDbSchemaGuid.Tables, null);
            }

            if (dt == null) return null;

            string[] excelSheetNames = new string[dt.Rows.Count];
            int i = 0;

            foreach (DataRow row in dt.Rows)
            {
                excelSheetNames[i] = row["TABLE_NAME"].ToString();
                i++;
            }

            return excelSheetNames;
        }

        public static DataTable ReadDataTable(DataTableCollection dataTableCollection, string tableName)
        {
            return (from DataTable dtbl in dataTableCollection where dtbl.TableName == tableName select dtbl).FirstOrDefault();
        }

        private void SetTagsInterface_FormClosed(object sender, FormClosedEventArgs e)
        {
            // we own both the event and the handler
            // we should dispose it before we are closed
            m_ExEvent.Dispose();
            //m_ExEvent = null;
            //m_Handler = null;

            // do not forget to call the base class
            //base.OnFormClosed(e);
        }

        public static void ErrorMsg(string msg)
        {
            Debug.WriteLine(msg);
            WinForms.MessageBox.Show(msg,
              "Error",
              WinForms.MessageBoxButtons.OK,
              WinForms.MessageBoxIcon.Error);
        }

        private void SetTagsInterface_Shown(object sender, EventArgs e)
        {
            this.TopMost = true;
        }

        ////private void textBox1_TextChanged(object sender, EventArgs e) => DistanceToKeep = textBox1.Text;

        //private void InputBoxBasic_FormClosing(object sender, FormClosingEventArgs e)
        //{
        //    ValueToSet = textBox1.Text;
        //}

        //private void textBox1_KeyDown(object sender, System.Windows.Forms.KeyEventArgs e)
        //{
        //    if (e.KeyCode == Keys.Enter) this.Close();
        //}

        //private void InputBoxBasic_Shown(object sender, EventArgs e)
        //{
        //    textBox1.Text = ValueToSet;
        //    textBox1.SelectionStart = 0;
        //    textBox1.SelectionLength = textBox1.Text.Length;
        //}
    }
}
