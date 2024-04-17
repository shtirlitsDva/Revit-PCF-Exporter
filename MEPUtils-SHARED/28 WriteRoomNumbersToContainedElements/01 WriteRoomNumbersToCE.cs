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

namespace MEPUtils.WriteRoomNumbersToContainedElements
{
    [Transaction(TransactionMode.Manual)]
    class WriteRoomNumbersToContainedElements : IExternalCommand
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
                if (document.IsLinked && document.Title != doc.Title) dict.Add(document.Title, document);
            }

            if (dict.Count == 0) { Debug.WriteLine("No other documents found!"); return Result.Cancelled; }

            Debug.WriteLine($"Found {dict.Count} other documents.");

            BaseFormTableLayoutPanel_Basic ds = new BaseFormTableLayoutPanel_Basic(
                System.Windows.Forms.Cursor.Position.X,
                System.Windows.Forms.Cursor.Position.Y,
                dict.Select(x => x.Key).OrderBy(x => x).ToList());
            ds.ShowDialog();
            string docTitle = ds.strTR;
            if (docTitle == null) { Debug.WriteLine("Cancelled!"); return Result.Cancelled; }

            Document roomDoc = dict[docTitle];
            if (roomDoc == null) { Debug.WriteLine("Room source document not found!"); return Result.Cancelled; }

            //var roomFC = new FilteredElementCollector

            //using (Transaction tx = new Transaction(doc, "Determine room number!"))
            //{
            //    tx.Start();
            //    try
            //    {
                    
            //    }
            //    catch (Exception ex)
            //    {
            //        tx.RollBack();
            //        Debug.WriteLine(ex.ToString());
            //        throw;
            //    }
            //    tx.Commit();
            //}

            return Result.Succeeded;
        }
    }
}
