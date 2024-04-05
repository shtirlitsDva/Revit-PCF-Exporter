using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ModelessForms.SearchAndSelect
{
    public static class TreeViewUtils
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
