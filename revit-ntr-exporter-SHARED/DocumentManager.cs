using System;
using System.Collections.Generic;
using System.Text;

using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace NTR_Exporter
{
    public sealed class DocumentManager
    {
        private static readonly Lazy<DocumentManager> lazy = new Lazy<DocumentManager>(() => new DocumentManager());
        public static DocumentManager Instance { get { return lazy.Value; } }
        public UIDocument UIDoc { get; private set; }
        public Document Doc { get; private set; }
        private DocumentManager() { }
        public void Initialize(UIDocument activeUIDocument, Document document)
        {
            if (UIDoc == null)
            {
                UIDoc = activeUIDocument;
            }
            if (Doc == null)
            {
                Doc = document;
            }
        }
    }
}
