using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Plumbing;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using NLog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using MoreLinq;
using System.Windows.Forms;
using WinForms = System.Windows.Forms;
using Shared;
using Data;

namespace ModelessForms.SearchAndSelect
{
    class AsyncSelectByFilters : IAsyncCommand
    {
        SelectionInformationContainer Payload;
        private AsyncSelectByFilters() { }
        public AsyncSelectByFilters(SelectionInformationContainer payload)
        {
            Payload = payload;
        }

        private static readonly NLog.Logger log = NLog.LogManager.GetCurrentClassLogger();

        public void Execute(UIApplication uiApp)
        {
            #region LoggerSetup
            //Nlog configuration
            var nlogConfig = new NLog.Config.LoggingConfiguration();
            //Targets
            var logfile = new NLog.Targets.FileTarget("logfile") { FileName = "X:\\GitHub\\log.txt", DeleteOldFileOnStartup = true };
            //Rules
            nlogConfig.AddRule(LogLevel.Info, LogLevel.Fatal, logfile);
            //Apply config
            NLog.LogManager.Configuration = nlogConfig;
            //DISABLE LOGGING
            NLog.LogManager.DisableLogging();
            #endregion

            Document doc = uiApp.ActiveUIDocument.Document;
            UIDocument uidoc = uiApp.ActiveUIDocument;

            Selection selection = uidoc.Selection;

            FilteredElementCollector col = new FilteredElementCollector(doc);

            //Test to see if catfilter is populated
            if (Payload.CategoriesToSearch.Count < 1) return;

            List<ElementFilter> catFilter = new List<ElementFilter>();
            if (Payload.CategoriesToSearch.Contains("Pipe Fittings"))
                catFilter.Add(new ElementCategoryFilter(BuiltInCategory.OST_PipeFitting));
            if (Payload.CategoriesToSearch.Contains("Pipe Accessories"))
                catFilter.Add(new ElementCategoryFilter(BuiltInCategory.OST_PipeAccessory));
            if (Payload.CategoriesToSearch.Contains("Pipes"))
                catFilter.Add(new ElementCategoryFilter(BuiltInCategory.OST_PipeCurves));

            col.WherePasses(new LogicalAndFilter(new List<ElementFilter>
            {new LogicalOrFilter(catFilter),
                new LogicalOrFilter(new List<ElementFilter>
                {
                    new ElementClassFilter(typeof(Pipe)),
                    new ElementClassFilter(typeof(FamilyInstance))
                })}));

            //selection.SetElementIds(col.ToElementIds());

            Payload.ElementsInSelection = new HashSet<ElementImpression>
                (col.Select(x => new ElementImpression(x, Payload.Grouping)));
            Payload.RaiseSnSOperationComplete();
        }
    }

    class AsyncGatherParameterData : IAsyncCommand
    {
        SelectionInformationContainer Payload;
        private AsyncGatherParameterData() { }
        public AsyncGatherParameterData(SelectionInformationContainer payload) => Payload = payload;
        public void Execute(UIApplication uiApp)
        {
            Document doc = uiApp.ActiveUIDocument.Document;
            UIDocument uidoc = uiApp.ActiveUIDocument;

            FilteredElementCollector col = new FilteredElementCollector(doc);

            if (Payload.CategoriesToSearch == null) return;
            //Test to see if catfilter is populated
            if (Payload.CategoriesToSearch.Count < 1) return;

            HashSet<ParameterImpression> allParameters = new HashSet<ParameterImpression>(new ParameterImpressionComparer());

            foreach (string catName in Payload.CategoriesToSearch)
            {
                FilteredElementCollector col1 = new FilteredElementCollector(doc);
                col1.OfCategory(GetCategoryByName(catName)).WhereElementIsNotElementType().WhereElementIsViewIndependent();
                HashSet<Element> elements = new HashSet<Element>(col1.ToElements().DistinctBy(x => x.FamilyName()));
                HashSet<Element> types = new HashSet<Element>(elements.Select(x => doc.GetElement(x.GetTypeId())));

                CreateParameterImpressions(elements, allParameters);
                CreateParameterImpressions(types, allParameters);

                //Local function to create parameter impressions
                void CreateParameterImpressions(HashSet<Element> setToProcess, HashSet<ParameterImpression> setToAdd)
                {
                    if (setToProcess != null && setToAdd != null && setToProcess.Count > 0)
                    {
                        foreach (Element e in setToProcess)
                        {
                            var pfep = e.GetOrderedParameters();
                            foreach (Parameter p in pfep)
                            {
                                setToAdd.Add(new ParameterImpression(p));
                            }
                        }
                    }
                }

                //Local function to return correct category
                BuiltInCategory GetCategoryByName(string Name)
                {
                    switch (Name)
                    {
                        case "Pipe Fittings":
                            return BuiltInCategory.OST_PipeFitting;
                        case "Pipe Accessories":
                            return BuiltInCategory.OST_PipeAccessory;
                        case "Pipes":
                            return BuiltInCategory.OST_PipeCurves;
                        default:
                            return BuiltInCategory.INVALID;
                    }
                }
            }

            Payload.AllParameterImpressions = allParameters;
            Payload.RaiseGetParameterDataOperationComplete();
        }
    }

    class AsyncSelectElements : IAsyncCommand
    {
        public List<int> ElementIdList { get; private set; }
        private AsyncSelectElements() { }
        public AsyncSelectElements(List<int> elementIdList)
        {
            ElementIdList = elementIdList;
        }

        public void Execute(UIApplication uiApp)
        {
            Document doc = uiApp.ActiveUIDocument.Document;
            UIDocument uidoc = uiApp.ActiveUIDocument;
            Selection selection = uidoc.Selection;

            selection.SetElementIds(ElementIdList.Select(x => new ElementId(x)).ToList());
        }
    }
}
