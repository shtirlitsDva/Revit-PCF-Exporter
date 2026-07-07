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

namespace MEPUtils.PipingSystemsAndFilters
{
    [Shared.DevReloadButton(Text = "Update abbreviation", Tooltip = "Set each PipingSystemType's Abbreviation to its Name.", Group = "Piping Systems", Panel = "MEP", Order = 145, Icon16 = "ImgMUUpdateAbbrev16.png", Icon32 = "ImgMUUpdateAbbrev32.png")]
    [Transaction(TransactionMode.Manual)]
    public class UpdateAbbreviationFromName : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIApplication uiApp = commandData.Application;
            Document doc = uiApp.ActiveUIDocument.Document;

            using (Transaction tr = new Transaction(doc, "Update Abbreviation"))
            {
                tr.Start();
                try
                {
                    var psts =
                        new FilteredElementCollector(doc)
                        .OfClass(typeof(PipingSystemType));

                    foreach (PipingSystemType pst in psts)
                        if (pst.Abbreviation != pst.Name) pst.Abbreviation = pst.Name;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex.ToString());
                    tr.RollBack();
                    throw;
                }
                tr.Commit();
            }

            return Result.Succeeded;
        }
    }
}