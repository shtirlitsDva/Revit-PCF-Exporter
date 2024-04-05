using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Plumbing;
using Autodesk.Revit.DB.Structure;
using Autodesk.Revit.DB.Mechanical;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using MEPUtils.SharedStaging;
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

            foreach (Document document in documents)
            {
                Debug.WriteLine(document.Title);
            }

            if (documents.Size == 0 || documents.Size == 1 || documents.Size > 2)
                throw new Exception("There must be exactly 2 documents open!");

            Document destDoc = null;

            foreach (Document d in documents)
            {
                if (d.Title != doc.Title)
                {
                    destDoc = d;
                    break;
                }
            }

            using (Transaction targetTr = new Transaction(destDoc, "Copy selected elements!"))
            {
                targetTr.Start();
                try
                {
                    Selection selection = uiApp.ActiveUIDocument.Selection;
                    ICollection<ElementId> elemIds = selection.GetElementIds();
                    if (elemIds == null) throw new Exception("Getting element from selection failed!");
                    if (elemIds.Count == 0) throw new Exception("No elements selected!");

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
