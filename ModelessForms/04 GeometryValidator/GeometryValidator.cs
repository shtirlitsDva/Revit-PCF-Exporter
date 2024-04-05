using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using mySettings = ModelessForms.Properties.Settings;
using NLog;
using Autodesk.Revit.DB;
using ModelessForms.SearchAndSelect;

namespace ModelessForms.GeometryValidator
{
    public partial class GeometryValidatorForm : System.Windows.Forms.Form
    {
        //Log
        private static readonly NLog.Logger log = NLog.LogManager.GetCurrentClassLogger();

        //Modeless stuff
        private Autodesk.Revit.UI.ExternalEvent m_ExEvent;
        private ExternalEventHandler m_Handler;
        Application ThisApp;
        private ConnectorValidationContainer CVContainer;
        List<string> sysAbbrs = new List<string>();
        Regex regex = new Regex(@"(?<number>\d+):\s");

        public GeometryValidatorForm(Autodesk.Revit.UI.ExternalEvent exEvent,
                   ExternalEventHandler handler,
                   ModelessForms.Application thisApp)
        {
            InitializeComponent();

            m_ExEvent = exEvent;
            m_Handler = handler;
            ThisApp = thisApp;

            //Initialize treeview path separator
            treeView1.PathSeparator = ".";

            //Log
            LogManager.Configuration = 
                new NLog.Config.XmlLoggingConfiguration(
                    "X:\\Github\\shtirlitsDva\\MyRevitAddins\\MyRevitAddins\\SetTagsModeless\\NLog.config");

            AsyncGetSystemAbbreviations agsas = new AsyncGetSystemAbbreviations();
            agsas.SysAbbrs = sysAbbrs;
            agsas.CollectionOfSysAbbrsComplete += UpdateComboBoxWithAbbrSys;
            ThisApp.asyncCommand = agsas;
            m_ExEvent.Raise();
        }

        private void button_Update_Click(object sender, EventArgs e)
        {
            CVContainer = new ConnectorValidationContainer();
            CVContainer.SystemToValidate = sysAbbrs[comboBox_systemList.SelectedIndex];
            AsyncValidateConnectorGeometry avcg = new AsyncValidateConnectorGeometry(CVContainer);
            avcg.ValidationOperationComplete += UpdateTreeViewOnValidationComplete;
            ThisApp.asyncCommand = avcg;
            m_ExEvent.Raise();
        }

        private void UpdateComboBoxWithAbbrSys(object sender, MyEventArgs e)
        {
            sysAbbrs.Insert(0, "All");
            comboBox_systemList.Items.Clear();
            comboBox_systemList.DataSource = null;
            comboBox_systemList.DataSource = sysAbbrs;
        }

        private void UpdateTreeViewOnValidationComplete(object sender, MyEventArgs e)
        {
            treeView1.BeginUpdate();
            treeView1.Nodes.Clear();

            #region null checks
            if (CVContainer == null)
            {
                treeView1.Nodes.Add("CVContainer null!");
                treeView1.EndUpdate();
                return;
            }

            if (CVContainer.ValidationResult == null)
            {
                treeView1.Nodes.Add("CVContainer.ValidationResult null!");
                treeView1.EndUpdate();
                return;
            }

            if (CVContainer.ValidationResult.Count == 0)
            {
                treeView1.Nodes.Add("No errors detected!");
                treeView1.EndUpdate();
                return;
            } 
            #endregion

            //Begin treeview population
            treeView1.Nodes.Add("Errors");

            for (int i = 0; i < CVContainer.ValidationResult.Count; i++)
            {
                ConnectorValidationResult cvr = CVContainer.ValidationResult[i];

                for (int j = 0; j < cvr.Data.Count; j++)
                {
                    string[] pathParts = new string[3];
                    var data = cvr.Data[j];

                    //Populate path data
                    pathParts[0] = "Errors";
                    pathParts[1] =
                        $"{i}: {cvr.LongestDist.ToString("0.######")}";
                    pathParts[2] = data.id.IntegerValue.ToString();

                    //Create an array of all full paths from root node to the element
                    string[] fullPaths = new string[pathParts.Length];
                    for (int k = 0; k < fullPaths.Length; k++)
                    {
                        if (k == 0) fullPaths[k] = pathParts[k];
                        else fullPaths[k] = fullPaths[k - 1] + "." + pathParts[k];
                    }

                    //Iterate through the fullPaths to determine, if node exists, if not -> create it
                    TreeNode previousNode = null;
                    for (int l = 0; l < fullPaths.Length; l++)
                    {
                        TreeNode foundNode = treeView1.Nodes.FindTreeNodeByFullPath(fullPaths[l]);
                        if (foundNode == null)
                        {
                            if (previousNode != null) previousNode = previousNode.Nodes.Add(pathParts[l]);
                        }
                        else
                        {
                            previousNode = foundNode;
                            continue;
                        }
                    }
                }
            }
            treeView1.EndUpdate();
            treeView1.Nodes[0].Expand();
        }

        private void treeView1_AfterSelect(object sender, TreeViewEventArgs e)
        {
            List<ElementId> ids = new List<ElementId>();

            if (e.Node.IsSelected)
            {
                //Don't react to top level node
                if (e.Node.Level == 0) return;

                if (e.Node.Level == 1)
                {
                    if (regex.IsMatch(e.Node.Text))
                    {
                        int number = Convert.ToInt32(
                            regex.Match(e.Node.Text).Groups["number"].Value);

                        foreach (var item in CVContainer.ValidationResult[number].Data)
                            ids.Add(item.id);
                    }
                    else throw new Exception($"Node name {e.Node.Text} did not match regex!");
                }
            }

            if (ids.Count < 1) return;

            AsyncSelectElements asSE = new AsyncSelectElements(ids);
            ThisApp.asyncCommand = asSE;
            m_ExEvent.Raise();
        }
    }
}
