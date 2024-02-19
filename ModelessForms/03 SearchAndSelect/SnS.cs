using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using mySettings = ModelessForms.Properties.Settings;
using NLog;
using Data;

namespace ModelessForms.SearchAndSelect
{
    public partial class SnS : Form
    {
        //Log
        private static readonly NLog.Logger log = NLog.LogManager.GetCurrentClassLogger();

        //Modeless stuff
        private Autodesk.Revit.UI.ExternalEvent m_ExEvent;
        private ExternalEventHandler m_Handler;
        Application ThisApp;

        //data stuff
        SelectionInformationContainer Payload = new SelectionInformationContainer();
        List<string> SelectedCategories = new List<string>();

        public SnS(Autodesk.Revit.UI.ExternalEvent exEvent,
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
            LogManager.Configuration = new NLog.Config.XmlLoggingConfiguration(
                "X:\\Github\\shtirlitsDva\\MyRevitAddins\\MyRevitAddins\\SetTagsModeless\\NLog.config");

            //Setup a rudimentary list with categories
            string[] cats = { "Pipes", "Pipe Fittings", "Pipe Accessories", "Generic Models" };
            checkedListBox2.Items.Clear();
            checkedListBox2.Items.AddRange(cats);

            Grouping grouping = mySettings.Default.GroupingSettings;
            if (grouping != null) Payload.Grouping = grouping;

            //Initialize settings for categories
            if (mySettings.Default.SelectedCategories != null)
            {
                SelectedCategories = mySettings.Default.SelectedCategories;
                if (SelectedCategories.Count != 0)
                {
                    foreach (string cat in SelectedCategories)
                    {
                        SetItemChecked(cat, checkedListBox2);
                    }
                }
                Payload.CategoriesToSearch = SelectedCategories;

                //Request Revit for parameter information
                AsyncGatherParameterData asGPD = new AsyncGatherParameterData(Payload);
                ThisApp.asyncCommand = asGPD;
                m_ExEvent.Raise();
                Payload.GetParameterDataOperationComplete += GetParameterDataOperationComplete;
                button2.Text = "Loading parameter data...";
            }
        }
        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBox1.Checked)
            {
                for (int i = 0; i < checkedListBox2.Items.Count; i++)
                {
                    checkedListBox2.SetItemChecked(i, true);
                }
            }
            else
            {
                for (int i = 0; i < checkedListBox2.Items.Count; i++)
                {
                    checkedListBox2.SetItemChecked(i, false);
                }
            }
        }
        private bool subscribedToSnSOperationComplete = false;
        /// <summary>
        /// Select button pushed.
        /// </summary>
        private void button1_Click(object sender, EventArgs e)
        {
            Grouping grouping = mySettings.Default.GroupingSettings;

            if (grouping == null)
            {
                EditGroupingForm egf = new EditGroupingForm(Payload.AllParameterImpressions, null);
                egf.ShowDialog();
                Payload.Grouping = egf.Grouping;
            }

            Payload.CategoriesToSearch = checkedListBox2.CheckedItems.OfType<string>().ToList();
            if (!subscribedToSnSOperationComplete)
            {
                Payload.SnSOperationComplete += UpdateTreeView;
                subscribedToSnSOperationComplete = true;
            }

            AsyncSelectByFilters asSBF = new AsyncSelectByFilters(Payload);
            ThisApp.asyncCommand = asSBF;
            m_ExEvent.Raise();
        }

        private void GetParameterDataOperationComplete(object sender, MyEventArgs e)
        {
            button2.Text = "Edit grouping (Ready)";
            //foreach (ParameterImpression pi in Payload.AllParameterImpressions)
            //{
            //    log.Debug(pi.Name);
            //}
        }

        private void UpdateTreeView(object sender, MyEventArgs e)
        {
            treeView1.BeginUpdate();
            treeView1.Nodes.Clear();

            //Manually add root node
            treeView1.Nodes.Add("All");

            foreach (ElementImpression ei in Payload.ElementsInSelection)
            {
                //Declare array to hold the names of all nodes in path to the element
                string[] pathParts = new string[Payload.Grouping.ParameterList.Count + 2];

                //The name of root node
                pathParts[0] = "All";
                //Populate the path parts with values from elements
                for (int i = 0; i < Payload.Grouping.ParameterList.Count; i++)
                {
                    pathParts[i + 1] = ei.Values[i];
                }
                //Finish the list with the name of the element (id currently)
                pathParts[pathParts.Length - 1] = ei.ElementId.ToString();

                //Create an array of all full paths from root node to the element
                string[] fullPaths = new string[pathParts.Length];
                for (int i = 0; i < fullPaths.Length; i++)
                {
                    if (i == 0) fullPaths[i] = pathParts[i];
                    else fullPaths[i] = fullPaths[i - 1] + "." + pathParts[i];
                }

                //Iterate through the fullPaths to determine, if node exists, if not -> create it
                TreeNode previousNode = null;
                for (int i = 0; i < fullPaths.Length; i++)
                {
                    TreeNode foundNode = treeView1.Nodes.FindTreeNodeByFullPath(fullPaths[i]);
                    if (foundNode == null)
                    {
                        if (previousNode != null) previousNode = previousNode.Nodes.Add(pathParts[i]);
                    }
                    else
                    {
                        previousNode = foundNode;
                        continue;
                    }
                }
            }
            treeView1.EndUpdate();
        }

        private void treeView1_AfterSelect(object sender, TreeViewEventArgs e)
        {
            List<int> res = null;

            if (e.Node.IsSelected)
            {
                if (e.Node.Nodes.Count != 0)
                {
                    //From here:
                    //https://stackoverflow.com/a/14141963/6073998
                    Func<TreeNode, IEnumerable<TreeNode>> getChildren = null;
                    getChildren = n =>
                    {
                        if (n.Nodes.Count != 0)
                        {
                            var list = new List<TreeNode>(n.Nodes.Cast<TreeNode>().Where(c => c.Nodes.Count == 0));
                            foreach (TreeNode c in n.Nodes)
                            {
                                // Note the recursive call below:
                                list.AddRange(getChildren(c));
                            }
                            return list;
                        }
                        else
                        {
                            return new TreeNode[0];
                        }
                    };

                    res = getChildren(e.Node).Select(x => int.Parse(x.Text)).ToList();
                }
                else
                {
                    res = new List<int>(1) { int.Parse(e.Node.Text) };
                }
            }

            if (res != null)
            {
                AsyncSelectElements asSE = new AsyncSelectElements(res);
                ThisApp.asyncCommand = asSE;
                m_ExEvent.Raise();
            }
        }

        /// <summary>
        /// "Edit Grouping" button pushed.
        /// </summary>
        private void button2_Click(object sender, EventArgs e)
        {
            if (Payload.AllParameterImpressions != null)
            {
                EditGroupingForm egf = new EditGroupingForm(
                    Payload.AllParameterImpressions, Payload.Grouping);
                egf.ShowDialog();
                Payload.Grouping = egf.Grouping;
            }
            else
            {
                //Request Revit for parameter information
                AsyncGatherParameterData asGPD = new AsyncGatherParameterData(Payload);
                ThisApp.asyncCommand = asGPD;
                m_ExEvent.Raise();
                Payload.GetParameterDataOperationComplete += GetParameterDataOperationComplete;
                button2.Text = "Loading parameter data...";
            }
        }

        private void SnS_FormClosing(object sender, FormClosingEventArgs e)
        {
            mySettings.Default.SelectedCategories = checkedListBox2.CheckedItems.OfType<string>().ToList();
            mySettings.Default.Save();
        }

        private void SetItemChecked(string item, CheckedListBox myCheckedListBox)
        {
            int index = GetItemIndex(item, myCheckedListBox);

            if (index < 0) return;

            myCheckedListBox.SetItemChecked(index, true);
        }

        private int GetItemIndex(string item, CheckedListBox myCheckedListBox)
        {
            int index = 0;

            foreach (object o in myCheckedListBox.Items)
            {
                if (item == o.ToString())
                {
                    return index;
                }

                index++;
            }

            return -1;
        }
    }
}
