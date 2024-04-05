using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using MoreLinq;
using System.IO;
using Autodesk.Revit.UI;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Plumbing;
using Autodesk.Revit.DB.Structure;
using Shared;
using Color = System.Drawing.Color;
using fi = Shared.Filter;
using op = Shared.Output;
using tr = Shared.Transformation;
using mp = Shared.MepUtils;

namespace MEPUtils
{
    public partial class InsulationSettingsWindow : System.Windows.Forms.Form
    {
        public string PathToSettingsXml { get; }
        public DataTable Settings { get; }

        public InsulationSettingsWindow(UIApplication uiApp)
        {
            InitializeComponent();

            this.Height = 800;
            this.Width = 475;

            Document doc = uiApp.ActiveUIDocument.Document;

            //Test if settings file exist
            string pn = doc.ProjectInformation.Name;
            PathToSettingsXml =
                Environment.ExpandEnvironmentVariables($"%AppData%\\MyRevitAddins\\MEPUtils\\Settings.{pn}.Insulation.xml"); //Magic text?
            bool settingsExist = File.Exists(PathToSettingsXml);

            //Initialize an empty datatable
            Settings = new DataTable("InsulationSettings");

            if (settingsExist) //Read file if exists
            {
                using (Stream stream = new FileStream(PathToSettingsXml, FileMode.Open, FileAccess.Read))
                {
                    DataSet ds = new DataSet();
                    ds.ReadXml(stream);
                    Settings = ds.Tables[0];
                }
            }
            else //If it doesn't exist -- create columns for data storage
            {
                Settings.Columns.Add("FamilyAndType", typeof(string));
                Settings.Columns.Add("AddInsulation", typeof(bool));
            }

            //Collect all pipe fittings and accessories names and types to determine number of rows
            var fittings = fi.GetElements<Element, BuiltInCategory>(doc, BuiltInCategory.OST_PipeFitting)
                .DistinctBy(d => d.get_Parameter(BuiltInParameter.ELEM_FAMILY_AND_TYPE_PARAM).AsValueString())
                .OrderBy(o => o.get_Parameter(BuiltInParameter.ELEM_FAMILY_AND_TYPE_PARAM).AsValueString())
                .ToList();

            var accessories = fi.GetElements<Element, BuiltInCategory>(doc, BuiltInCategory.OST_PipeAccessory)
                .DistinctBy(d => d.get_Parameter(BuiltInParameter.ELEM_FAMILY_AND_TYPE_PARAM).AsValueString())
                .OrderBy(o => o.get_Parameter(BuiltInParameter.ELEM_FAMILY_AND_TYPE_PARAM).AsValueString())
                .ToList();

            var elements = fittings.Concat(accessories).ToList();

            tableLayoutPanel1.ColumnCount = 2;
            tableLayoutPanel1.RowCount = elements.Count;

            for (int i = 0; i < elements.Count; i++)
            {
                var tb = new System.Windows.Forms.Label()
                {
                    Text = elements[i].get_Parameter(BuiltInParameter.ELEM_FAMILY_AND_TYPE_PARAM).AsValueString(),
                    Name = $"Label_{i}",
                    AutoSize = false,
                    Dock = DockStyle.Fill,
                    TextAlign = ContentAlignment.MiddleRight,
                    TabStop = false,
                };

                tableLayoutPanel1.Controls.Add(tb, 0, i);

                var cb = new CheckBox()
                {
                    Name = $"CheckBox_{i}",
                    Anchor = AnchorStyles.Left,
                    Dock = DockStyle.Fill,
                    Width = 15,
                    Checked = false,
                };

                cb.Enter += Cb_Enter;
                cb.Leave += Cb_Leave;
                cb.CheckedChanged += Cb_CheckedChanged;

                tableLayoutPanel1.Controls.Add(cb, 1, i);

                //Manage settings

                //Local function to create a setting for an element, used in the if below to avoid duplicate code
                void CreateSetting()
                {
                    DataRow row = Settings.NewRow();
                    row["FamilyAndType"] = tb.Text;
                    row["AddInsulation"] = cb.Checked;
                    Settings.Rows.Add(row);
                }

                if (settingsExist)
                {
                    if (Settings.AsEnumerable().Any(row => row.Field<string>("FamilyAndType") == tb.Text))
                    {
                        // If true
                        //Apply read settings from file to processed checkbox
                        var query = Settings.AsEnumerable()
                            .Where(row => row.Field<string>("FamilyAndType") == tb.Text)
                            .Select(row => row.Field<string>("AddInsulation"));
                        bool value = bool.Parse(query.FirstOrDefault());
                        cb.Checked = value;
                    }
                    else
                    {
                        CreateSetting();
                    }
                }
                else
                {
                    //If false
                    //Create a corresponding row in the settings datatable
                    CreateSetting();
                }
            }
        }

        /// <summary>
        /// Method to update datatable when a checkbox changes checked.
        /// </summary>
        /// <param name="sender">The checkbox.</param>
        private void Cb_CheckedChanged(object sender, EventArgs e)
        {
            var c = sender as CheckBox;
            var position = tableLayoutPanel1.GetPositionFromControl(c);
            var tb = tableLayoutPanel1.GetControlFromPosition(0, position.Row);
            var query = from r in Settings.AsEnumerable()
                        where r.Field<string>("FamilyAndType") == tb.Text
                        select r;
            var row = query.FirstOrDefault();
            row["AddInsulation"] = c.Checked;
        }

        private void Cb_Leave(object sender, EventArgs e)
        {
            var c = sender as CheckBox;
            c.BackColor = DefaultBackColor;
        }

        private void Cb_Enter(object sender, EventArgs e)
        {
            var c = sender as CheckBox;
            c.BackColor = Color.DarkGray;
        }
    }
}
