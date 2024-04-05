using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Plumbing;
using Autodesk.Revit.DB.Structure;
using Autodesk.Revit.DB.Mechanical;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;

using Microsoft.WindowsAPICodePack.Dialogs;
using MoreLinq;
using Shared;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Windows.Input;
using dbg = Shared.Dbg;
using fi = Shared.Filter;
using lad = MEPUtils.CreateInstrumentation.ListsAndDicts;
using mp = Shared.MepUtils;
using tr = Shared.Transformation;
using Autodesk.Revit.Attributes;
using System.Diagnostics;

namespace MEPUtils.CopyElementsToAnotherDoc
{
    [Transaction(TransactionMode.Manual)]
    class CopyElementsToAnotherDoc : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIApplication uiApp = commandData.Application;
            Document doc = commandData.Application.ActiveUIDocument.Document;
            UIDocument uidoc = uiApp.ActiveUIDocument;

            DocumentSet documents = uiApp.Application.Documents;

            Dictionary<string, Document> dict = new Dictionary<string, Document>();

            foreach (Document document in documents)
            {
                //Debug.WriteLine($"{document.Title} is Linked: {document.IsLinked}.");
                if (!document.IsLinked && document.Title != doc.Title)
                    dict.Add(document.Title, document);
            }

            if (dict.Count == 0) { Debug.WriteLine("No other documents found!"); return Result.Cancelled; }

            //Select destination document
            BaseFormTableLayoutPanel_Basic ds = new BaseFormTableLayoutPanel_Basic(
                System.Windows.Forms.Cursor.Position.X,
                System.Windows.Forms.Cursor.Position.Y,
                dict.Select(x => x.Key).OrderBy(x => x).ToList());
            ds.ShowDialog();
            string docTitle = ds.strTR;
            if (docTitle == null) { Debug.WriteLine("Cancelled!"); return Result.Cancelled; }

            Document destDoc = dict[docTitle];
            if (destDoc == null) { Debug.WriteLine("Destination document not found!"); return Result.Cancelled; }

            using (Transaction targetTr = new Transaction(destDoc, "Copy selected elements!"))
            {
                targetTr.Start();
                try
                {
                    Selection selection = uiApp.ActiveUIDocument.Selection;
                    ICollection<ElementId> elemIds = selection.GetElementIds();
                    if (elemIds == null) throw new Exception("Getting element from selection failed!");
                    if (elemIds.Count == 0) throw new Exception("No elements selected!");

                    Debug.WriteLine($"Copying {elemIds.Count} element(s) to {destDoc.Title}.");
                    //CopyPasteOptions options = new CopyPasteOptions();

                    ElementTransformUtils.CopyElements(
                        doc, elemIds, destDoc,
                        Transform.Identity, null);
                }
                catch (Exception ex)
                {
                    targetTr.RollBack();
                    Debug.WriteLine(ex.ToString());
                    throw;
                }
                targetTr.Commit();
            }

            return Result.Succeeded;
        }
    }
}
