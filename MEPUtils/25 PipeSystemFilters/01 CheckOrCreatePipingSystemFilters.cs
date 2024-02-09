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

namespace MEPUtils.PipingSystemsAndFilters
{
    [Transaction(TransactionMode.Manual)]
    class CheckOrCreatePipingSystemFilters //: IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIApplication uiApp = commandData.Application;
            Document doc = commandData.Application.ActiveUIDocument.Document;
            UIDocument uidoc = uiApp.ActiveUIDocument;

            using (Transaction tr = new Transaction(doc, "Check or create filters!"))
            {
                tr.Start();
                try
                {
                    View view = doc.ActiveView;

                    var pipingSystemTypes =
                        new FilteredElementCollector(doc)
                        .OfClass(typeof(PipingSystemType))
                        .ToDictionary(x => x.Name);

                    var existingFilters =
                        new FilteredElementCollector(doc)
                        .OfClass(typeof(ParameterFilterElement))
                        .ToDictionary(x => x.Name);

                    IList<ElementId> cats = new List<ElementId>()
                    {
                        new ElementId(BuiltInCategory.OST_PipeCurves),
                        new ElementId(BuiltInCategory.OST_PipeAccessory),
                        new ElementId(BuiltInCategory.OST_PipeFitting),
                        //new ElementId(BuiltInCategory.OST_MechanicalEquipment) -> Cannot use RBS_PIPING_SYSTEM_TYPE_PARAM
                    };

                    foreach (var pst in pipingSystemTypes)
                    {
                        if (!existingFilters.ContainsKey(pst.Key))
                        {
                            Debug.WriteLine($"Filter for system type {pst.Key} not found! Creating...");

                            IList<FilterRule> rules = new List<FilterRule>();

                            Parameter par = new FilteredElementCollector(doc)
                                .OfClass(typeof(FamilyInstance))
                                .OfCategory(BuiltInCategory.OST_PipeAccessory)
                                .FirstElement()
                                .get_Parameter(BuiltInParameter.RBS_PIPING_SYSTEM_TYPE_PARAM);

                            rules.Add(ParameterFilterRuleFactory.CreateEqualsRule(
                                par.Id, pst.Value.Id));

                            var epf = new ElementParameterFilter(rules);

                            var viewFilter = ParameterFilterElement.Create(doc, pst.Key, cats, epf);

                            Debug.WriteLine($"Created FilterRule {viewFilter.Name}!");
                        }
                    }
                }
                catch (Exception ex)
                {
                    tr.RollBack();
                    throw;
                }
                tr.Commit();
            }

            return Result.Succeeded;
        }
    }
}
