using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

using Autodesk.Revit.DB;

using mp = Shared.MepUtils;

using stgs = Shared.Properties.Settings;

namespace Shared.Tools
{
    public partial class ValidationTypeSelector : System.Windows.Forms.Form
    {
        private Document _doc;
        private List<string> sysAbbrs;
        public HashSet<Connector> Connectors;

        public ValidationTypeSelector(Document doc)
        {
            InitializeComponent();

            _doc = doc;

            radioButton_allSystems.Checked = stgs.Default.radioButton_allSystems;

            comboBox_systemList.Visible = !radioButton_allSystems.Checked;

            //Gather all connectors from the document
            //Filter also out all "Curve" connectors, which are olet ends at pipe cntr.
            HashSet<Connector> AllCons = mp.GetALLConnectorsInDocument(_doc)
                .ExceptWhere(c => c.ConnectorType == ConnectorType.Curve).ToHashSet();
            Connectors = AllCons.ExceptWhere(c => c.MEPSystemAbbreviation(_doc, true) == "ARGD").ToHashSet();
        }

        private void ValidationTypeSelector_FormClosing(object sender, FormClosingEventArgs e)
        {
            stgs.Default.Save();
        }

        private void radioButton_allSystems_CheckedChanged(object sender, EventArgs e)
        {
        }

        private void radioButton_selectedSystem_CheckedChanged(object sender, EventArgs e)
        {
            comboBox_systemList.Visible = radioButton_selectedSystem.Checked;

            if (radioButton_selectedSystem.Checked)
            {
                sysAbbrs = Connectors
                    .Select(x => x.MEPSystemAbbreviation(_doc, true))
                    .Distinct()
                    .OrderBy(x => x)
                    .ToList();

                comboBox_systemList.DataSource = sysAbbrs;
            }
        }

        private void button_validate_Click(object sender, EventArgs e)
        {
            if (!radioButton_allSystems.Checked) 
            {
                string sysAbbr = sysAbbrs[comboBox_systemList.SelectedIndex];

                Connectors = Connectors
                    .Where(x => x.MEPSystemAbbreviation(_doc, true) == sysAbbr)
                    .ToHashSet();
            }

            this.Close();
        }
    }
}
