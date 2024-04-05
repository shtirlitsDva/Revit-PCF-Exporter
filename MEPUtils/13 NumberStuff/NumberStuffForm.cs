using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Form = System.Windows.Forms.Form;
using TextBox = System.Windows.Forms.TextBox;

namespace MEPUtils
{
    public partial class NumberStuffForm : Form
    {
        private TableLayoutPanel mainPanel;
        public DataTable Settings;
        public Result Result = Result.Cancelled;
        private Document Doc;

        public NumberStuffForm(Document doc, DataTable settings)
        {
            InitializeComponent();
            Doc = doc;
            Settings = settings;
            this.mainPanel = new TableLayoutPanel();
            this.Controls.Add(this.mainPanel); //<- this is crucial for Form to show the control -- didn't know that!

            mainPanel.CellBorderStyle = TableLayoutPanelCellBorderStyle.Single;
            mainPanel.Dock = DockStyle.Fill;
            mainPanel.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;

            mainPanel.ColumnCount = 4;
            mainPanel.RowCount = 2;

            for (int x = 0; x < mainPanel.ColumnCount; x++)
            {
                //First add a column
                if (x == 0) mainPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 350F));
                else mainPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

                for (int y = 0; y < mainPanel.RowCount; y++)
                {
                    //Next, add a row.  Only do this when once, when creating the first column
                    if (x == 0)
                    {
                        mainPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
                    }
                }
            }

            Button btnCancel;
            Button btnNumber;

            mainPanel.Controls.Add(btnCancel = new Button { Text = "Cancel" }, 2, 0);
            mainPanel.Controls.Add(btnNumber = new Button { Text = "(Re-)Number" }, 3, 0);

            btnCancel.Click += new EventHandler(Btn_Cancel);
            btnNumber.Click += new EventHandler(Btn_Number);

            mainPanel.Controls.Add(new Label { Text = "Family" }, 0, 1);
            mainPanel.Controls.Add(new Label { Text = "TAG 1 (Prefix)" }, 1, 1);
            mainPanel.Controls.Add(new Label { Text = "TAG 2 (Start nr.)" }, 2, 1);
            mainPanel.Controls.Add(new Label { Text = "Nr. of digits" }, 3, 1);

            //See if element is not allowed to be insulated
            var query = Settings.AsEnumerable()
                .Where(row => bool.Parse(row.Field<string>("Number")) == true);

            foreach (var row in query)
            {
                AddRow(row.Field<string>("Family"));
            }
        }

        private void Btn_Cancel(object sender, EventArgs e) { this.Close(); }

        private void Btn_Number(object sender, EventArgs e)
        {
            Result = Result.Succeeded;
            this.Close();
        }

        private void Tb_TextChanged(object sender, EventArgs e)
        {
            var c = sender as TextBox;
            var position = mainPanel.GetPositionFromControl(c);
            var tb = mainPanel.GetControlFromPosition(0, position.Row);
            var query = from r in Settings.AsEnumerable()
                        where r.Field<string>("Family") == tb.Text
                        select r;
            var row = query.FirstOrDefault();

            switch (position.Column)
            {
                case 0:
                    break;
                case 1:
                    row["Prefix"] = c.Text;
                    break;
                case 2:
                    row["StartNumber"] = int.Parse(c.Text);
                    break;
                case 3:
                    row["Digits"] = int.Parse(c.Text);
                    break;
                default:
                    break;
            }
        }

        public void AddRow(string familyName)
        {
            string prefix = Settings.AsEnumerable()
                .Where(row => row.Field<string>("Family") == familyName)
                .Select(row => row.Field<string>("Prefix")).FirstOrDefault()
                ?? "NA";
            string startNr = Settings.AsEnumerable()
                .Where(row => row.Field<string>("Family") == familyName)
                .Select(row => row.Field<string>("StartNumber")).FirstOrDefault()
                ?? "1";
            string digits = Settings.AsEnumerable()
                .Where(row => row.Field<string>("Family") == familyName)
                .Select(row => row.Field<string>("Digits")).FirstOrDefault()
                ?? "2";

            //I have to declare temp vars for buttons
            //To be able to get a handle on their events
            //Couldn't find a way to assign events
            //in the constructor
            TextBox tbPrefix;
            TextBox tbStartNr;
            TextBox tbDigits;

            mainPanel.RowCount++;
            mainPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            mainPanel.Controls.Add(new TextBox()
            {
                Text = familyName,
                BorderStyle = BorderStyle.None,
                BackColor = SystemColors.Control,
                ReadOnly = true,
                TabStop = false,
                Width = 300
            },
            0, mainPanel.RowCount - 1);
            mainPanel.Controls.Add(tbPrefix = new TextBox() { Text = prefix }, 1, mainPanel.RowCount - 1);
            mainPanel.Controls.Add(tbStartNr = new TextBox() { Text = startNr }, 2, mainPanel.RowCount - 1);
            mainPanel.Controls.Add(tbDigits = new TextBox() { Text = digits }, 3, mainPanel.RowCount - 1);

            tbPrefix.TextChanged += new System.EventHandler(Tb_TextChanged);
            tbStartNr.TextChanged += new System.EventHandler(Tb_TextChanged);
            tbDigits.TextChanged += new System.EventHandler(Tb_TextChanged);
        }

        private void NumberStuffForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (Result == Result.Succeeded)
            {
                string pn = Doc.ProjectInformation.Name;
                string pathToSettingsXml =
                    Environment.ExpandEnvironmentVariables(
                        $"%AppData%\\MyRevitAddins\\MEPUtils\\Settings.{pn}.NumberStuff.xml"); //Magic text?

                using (Stream stream = new FileStream(pathToSettingsXml, FileMode.Create, FileAccess.Write))
                {
                    Settings.WriteXml(stream);
                } 
            }
        }
    }
}
