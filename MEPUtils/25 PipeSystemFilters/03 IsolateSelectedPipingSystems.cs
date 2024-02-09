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
using Shared.BuildingCoder;

namespace MEPUtils.PipingSystemsAndFilters
{
    class IsolatePipingSystemsOfSelectedElements
    {
        public Result Execute(UIApplication uiApp)
        {
            Document doc = uiApp.ActiveUIDocument.Document;
            string prefix = StaticVariables.filterNamePrefix;

            //Apply filters to current view
            new AddAllPipingSystemTypesFiltersToView().Execute(uiApp);

            using (Transaction tr = new Transaction(doc, "Isolate piping systems!"))
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

                    #region Get PSTs from selection
                    Selection selection = uiApp.ActiveUIDocument.Selection;
                    var elemIds = selection.GetElementIds();
                    if (elemIds.Count < 1)
                    {
                        BuildingCoderUtilities.ErrorMsg("No elements selected!");
                        tr.RollBack();
                        return Result.Failed;
                    }

                    var elems = elemIds.Select(x => doc.GetElement(x));

                    var selectedSystemTypes =
                        elems.Select(x => (PipingSystemType)doc.GetElement(
                            x.get_Parameter(BuiltInParameter.RBS_PIPING_SYSTEM_TYPE_PARAM)
                            .AsElementId()))
                        .Where(x => x != null);

                    if (selectedSystemTypes.Count() < 1)
                    {
                        BuildingCoderUtilities.ErrorMsg("No Piping System Types found!");
                        tr.RollBack();
                        return Result.Failed;
                    }

                    var selectedPSDict = selectedSystemTypes
                    .DistinctBy(x => x.Name)
                    .ToDictionary(x => x.Name);
                    #endregion

                    var viewFilters = view.GetFilters()
                        .Select(doc.GetElement)
                        .Cast<ParameterFilterElement>()
                        .ToDictionary(x => x.Name);

                    foreach (var filter in viewFilters)
                    {
                        if (selectedPSDict.ContainsKey(filter.Key.Replace(prefix, "")))
                            view.SetFilterVisibility(filter.Value.Id, true);
                        else
                        {
                            if (filter.Value.Name.StartsWith(prefix))
                                view.SetFilterVisibility(filter.Value.Id, false);
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
