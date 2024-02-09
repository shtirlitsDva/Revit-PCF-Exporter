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
    class AddAllPipingSystemTypesFiltersToView
    {
        public Result Execute(UIApplication uiApp)
        {
            Document doc = uiApp.ActiveUIDocument.Document;

            using (Transaction tr = new Transaction(doc, "Add filters to view!"))
            {
                tr.Start();
                try
                {
                    View view = doc.ActiveView;

                    var elsInView = fi.GetElementsWithConnectors(doc, view.Id);

                    var pipingSystemTypes =
                        elsInView.Select(x => (PipingSystemType)doc.GetElement(
                            x.get_Parameter(BuiltInParameter.RBS_PIPING_SYSTEM_TYPE_PARAM)
                            .AsElementId()))
                        .Where(x => x != null)
                        .DistinctBy(x => x.Name)
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

                    foreach (var pst in pipingSystemTypes.OrderBy(x => x.Key))
                    {
                        //Name of the filter
                        string filterName = StaticVariables.filterNamePrefix + pst.Key;

                        if (!existingFilters.ContainsKey(filterName))
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

                            var viewFilter = ParameterFilterElement.Create(doc, filterName, cats, epf);

                            Debug.WriteLine($"Created FilterRule {viewFilter.Name}!");

                            view.AddFilter(viewFilter.Id);
                        }
                        else
                        {
                            if (!view.IsFilterApplied(existingFilters[filterName].Id))
                                view.AddFilter(existingFilters[filterName].Id);
                        }
                    }
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

/*
 * Example of creating rules and nested logical group rules
 * 
 * //General setup
    parameter_ids = {
        'mark': DB.ElementId(DB.BuiltInParameter.DOOR_NUMBER),
    }

    mark_ends_with_rule = DB.ParameterFilterRuleFactory.CreateEndsWithRule(
        parameter_ids['mark'],
        'D',
        False
    )

    mark_contains_rule = DB.ParameterFilterRuleFactory.CreateContainsRule(
        parameter_ids['mark'],
        '20 Minute Rated',
        False
    )

    rules = List[DB.FilterRule]()
    rules.Add(mark_ends_with_rule)
    rules.Add(mark_contains_rule)

    element_filter = DB.ElementParameterFilter(rules)

    rule_set_or = DB.LogicalOrFilter(
        DB.ElementParameterFilter(mark_ends_with_rule),
        DB.ElementParameterFilter(mark_contains_rule)
    )

    //Nested groups
    filters_and = List[DB.ElementFilter]()
    filters_and.Add(DB.ElementParameterFilter(width_less_rule))
    filters_and.Add(DB.ElementParameterFilter(height_greater_rule))
    filters_and.Add(rule_set_or)

    rule_set_and = DB.LogicalAndFilter(filters_and)
 */
