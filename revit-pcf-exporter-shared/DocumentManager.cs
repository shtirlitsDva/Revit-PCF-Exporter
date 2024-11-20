using System;
using System.Collections.Generic;
using System.Text;

using Autodesk.Revit.DB;

namespace PCF_Exporter
{
    public sealed class DocumentManager
    {
        private static readonly Lazy<DocumentManager> lazy = new Lazy<DocumentManager>(() => new DocumentManager());
        public static DocumentManager Instance { get { return lazy.Value; } }
        public Document Doc { get; private set; }
        private DocumentManager() { }
        public void Initialize(Document document)
        {
            if (Doc == null)
            {
                Doc = document;
            }
        }
    }
}
