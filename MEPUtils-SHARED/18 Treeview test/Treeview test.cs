using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Plumbing;
using Autodesk.Revit.DB.Structure;
using Autodesk.Revit.DB.Mechanical;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MEPUtils.Treeview_test
{
    public partial class Treeview_testForm : System.Windows.Forms.Form
    {
        private HashSet<Autodesk.Revit.DB.Element> Elements;
        private PropertiesInformation[] PropertiesList;

        UIApplication uiApp;
        Document doc;
        UIDocument uidoc;
        Selection selection;

        public Treeview_testForm()
        {
            InitializeComponent();
            treeView1.PathSeparator = ".";
        }

        public Treeview_testForm(HashSet<Autodesk.Revit.DB.Element>
            elements, ExternalCommandData commandData) : this()
        {
            this.Elements = elements;
            //this.PropertiesList = propertiesList;

            uiApp = commandData.Application;
            doc = commandData.Application.ActiveUIDocument.Document;
            uidoc = uiApp.ActiveUIDocument;
            selection = uidoc.Selection;


            //PopulateTreeview();
        }

        public void PopulateTreeview()
        {
            //Manually add root node
            treeView1.Nodes.Add("All");

            foreach (Element e in Elements)
            {
                //Declare array to hold the names of all nodes in path to the element
                string[] pathParts = new string[PropertiesList.Length + 2];

                //The name of root node
                pathParts[0] = "All";
                //Populate the path parts with values from elements
                for (int i = 0; i < PropertiesList.Length; i++)
                {
                    pathParts[i + 1] = PropertiesList[i].getBipValue(e);
                }
                //Finish the list with the name of the element (id currently)
                pathParts[pathParts.Length - 1] = e.Id.IntegerValue.ToString();

                //Create an array of all full paths from root node to the element
                string[] fullPaths = new string[PropertiesList.Length + 2];
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
        }

        private void treeView1_AfterSelect(object sender, TreeViewEventArgs e)
        {
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

                    var res = getChildren(e.Node).Select(x => new ElementId(int.Parse(x.Text))).ToList();

                    selection.SetElementIds(res);
                }
                else
                {
                    selection.SetElementIds(new List<ElementId>() { new ElementId(int.Parse(e.Node.Text)) });
                }
            }
        }
    }

    public static class TreeNodeCollectionUtils
    {
        public static TreeNode FindTreeNodeByFullPath(this TreeNodeCollection collection, string fullPath, StringComparison comparison = StringComparison.InvariantCultureIgnoreCase)
        {
            var foundNode = collection.Cast<TreeNode>().FirstOrDefault(tn => string.Equals(tn.FullPath, fullPath, comparison));
            if (null == foundNode)
            {
                foreach (var childNode in collection.Cast<TreeNode>())
                {
                    var foundChildNode = FindTreeNodeByFullPath(childNode.Nodes, fullPath, comparison);
                    if (null != foundChildNode)
                    {
                        return foundChildNode;
                    }
                }
            }
            return foundNode;
        }
    }
}
