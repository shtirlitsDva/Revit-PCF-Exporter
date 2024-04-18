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
using Autodesk.Revit.DB.Architecture;
using sl = Shared.SimpleLogger;
using System.Security.Cryptography;

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

            sl.clrLog();

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

            //RoomFilter roomFilter = new RoomFilter();
            //var roomFC = new FilteredElementCollector(roomDoc)
            //    .WherePasses(roomFilter)
            //    .Cast<Room>();

            //var rooms = roomFC.ToHashSet();

            var els = fi.GetElementsWithConnectors(doc, true).ToHashSet();
            sl.log("Number of elements with connectors: " + els.Count);
            //sl.log("Number of rooms: " + rooms.Count);

            using (Transaction tx = new Transaction(doc, "Determine room number!"))
            {
                tx.Start();
                try
                {
                    Parameter par;
                    XYZ mid = default;

                    int failCount = 0;
                    int successCount = 0;

                    foreach (var el in els)
                    {
                        if (el is Pipe pipe)
                        {
                            Cons cons = new Cons(el);
                            mid = (cons.Primary.Origin + cons.Secondary.Origin) / 2;
                            sl.log(cons.Primary.Origin.ToString() + " " + cons.Secondary.Origin.ToString() + " " + mid.ToString());
                        }
                        else if (el is FamilyInstance fi)
                        {
                            mid = ((LocationPoint)fi.Location).Point;
                            sl.log(mid.ToString());
                        }

                        if (mid == null) continue;

                        Room room = roomDoc.GetRoomAtPoint(mid);
                        if (room == null) { failCount++; continue; }
                        else successCount++;

                        //par = el.LookupParameter("MC System Code");
                        //if (par == null) continue;

                        //par.Set(room.Number);
                    }

                    sl.log("Success: " + successCount);
                    sl.log("Fails: " + failCount);
                }
                catch (Exception ex)
                {
                    tx.RollBack();
                    Debug.WriteLine(ex.ToString());
                    throw;
                }
                tx.Commit();
            }

            return Result.Succeeded;
        }
    }
}
