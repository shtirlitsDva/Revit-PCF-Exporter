using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Plumbing;
using Autodesk.Revit.DB.Structure;
using Autodesk.Revit.DB.Mechanical;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;

using Microsoft.WindowsAPICodePack.Dialogs;
using MoreLinq;
using Shared;
using Shared.Forms;
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
using Shared.BuildingCoder;

namespace MEPUtils.TaggingTools
{
    [Transaction(TransactionMode.Manual)]
    class SetParFromME : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIApplication uiApp = commandData.Application;
            Document doc = commandData.Application.ActiveUIDocument.Document;
            UIDocument uidoc = uiApp.ActiveUIDocument;

            while (true)
            {
                Element element =
                    BuildingCoderUtilities.SelectSingleElement(
                        uidoc, "ME to read parameter value: ");

                if (element == null) { return Result.Succeeded; }

                using (Transaction t = new Transaction(doc, "Set parameter value"))
                {
                    t.Start();

                    try
                    {
                        Parameter par = element.LookupParameter("TAG 2");
                        if (par == null)
                        {
                            BuildingCoderUtilities.ErrorMsg("Parameter not found.");
                            t.RollBack();
                            return Result.Cancelled;
                        }

                        string parValue = par.AsString();
                        string ejTag1 = parValue.Replace("DRC", "EJ");
                        string ejTag2 = ejTag1.Substring(0, ejTag1.Length - 1) + "2";

                        //First expansion joint 1
                        Element ej =
                            BuildingCoderUtilities.SelectSingleElement(
                                uidoc, "first expansion joint: ");

                        if (ej == null) { t.RollBack(); return Result.Succeeded; }

                        Parameter ejPar = ej.LookupParameter("TAG 2");
                        if (ejPar == null)
                        {
                            BuildingCoderUtilities.ErrorMsg("Parameter not found.");
                            t.RollBack();
                            return Result.Cancelled;
                        }
                        ejPar.Set(ejTag1);

                        //First expansion joint 2
                        ej = BuildingCoderUtilities.SelectSingleElement(
                                uidoc, "second expansion joint: ");

                        if (ej == null) { t.RollBack(); return Result.Succeeded; }

                        ejPar = ej.LookupParameter("TAG 2");
                        if (ejPar == null)
                        {
                            BuildingCoderUtilities.ErrorMsg("Parameter not found.");
                            t.RollBack();
                            return Result.Cancelled;
                        }
                        ejPar.Set(ejTag2);
                    }
                    catch (Exception)
                    {
                        t.RollBack();
                        throw;
                    }

                    t.Commit();
                }
            }
        }
    }
}
