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

namespace MEPUtils.CopyPSTParameterValuesToElements
{
    [Transaction(TransactionMode.Manual)]
    class CopyPSTParameterValuesToElements : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIApplication uiApp = commandData.Application;
            Document doc = commandData.Application.ActiveUIDocument.Document;
            UIDocument uidoc = uiApp.ActiveUIDocument;

            using (Transaction tx = new Transaction(doc, "Copy PST parameter values to elements!"))
            {
                tx.Start();
                try
                {
                    IList<BuiltInCategory> bics = new List<BuiltInCategory>(2)
                    {
                        BuiltInCategory.OST_PipeCurves,
                        BuiltInCategory.OST_PipeFitting,
                    };
                    IList<ElementFilter> a = new List<ElementFilter>(bics.Count());
                    foreach (BuiltInCategory bic in bics) a.Add(new ElementCategoryFilter(bic));
                    LogicalOrFilter categoryFilter = new LogicalOrFilter(a);
                    LogicalAndFilter familyInstanceFilter = new LogicalAndFilter(
                        categoryFilter, new ElementClassFilter(typeof(FamilyInstance)));
                    IList<ElementFilter> b = new List<ElementFilter>
                    {
                        new ElementClassFilter(typeof(Pipe)),
                        familyInstanceFilter
                    };
                    LogicalOrFilter classFilter = new LogicalOrFilter(b);
                    FilteredElementCollector collector = new FilteredElementCollector(doc);
                    collector.WherePasses(classFilter);

                    foreach (Element e in collector)
                    {
                        //var parPST = e.get_Parameter(
                        //    BuiltInParameter.RBS_PIPING_SYSTEM_TYPE_PARAM);
                        //if (parPST == null) continue;

                        //var pst = doc.GetElement(parPST.AsElementId()) as PipingSystemType;
                        //if (pst == null) continue;

                        #region Comments pars
                        List<string> names = new List<string>()
                        {
                            "Comments1_CW",
                            "Comments2_CW",
                            "Comments3_CW",
                            "Comments4_CW",
                        };

                        string[] values = new string[names.Count];

                        for (int i = 0; i < names.Count; i++)
                        {
                            values[i] = e.LookupParameter(names[i]).AsString();
                        }

                        //string value = pst.LookupParameter(name).AsString();
                        
                        var par = e.LookupParameter("Comments5_CW");
                        if (par == null) continue;
                        par.Set(string.Join("_", values));

                        #endregion

                        #region Spec and pn
                        //string spec = pst.LookupParameter("PCF_PIPL_SPEC").AsString();
                        //string pn = pst.LookupParameter("PCF_NOMCLASS").AsString();

                        //string value = "";

                        //if (spec != null && pn != null)
                        //{
                        //    value = spec + " " + pn;
                        //}
                        //else value = spec;

                        //var par = e.LookupParameter("PCF_ELEM_SPEC");
                        //par.Set(value); 
                        #endregion
                    }
                }
                catch (Exception ex)
                {
                    tx.RollBack();
                    Debug.WriteLine(ex.ToString());
                    return Result.Failed;
                }
                tx.Commit();
            }

            return Result.Succeeded;
        }
    }
}
