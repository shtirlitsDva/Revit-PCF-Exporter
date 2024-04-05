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
using System.Data;
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
using NLog;

namespace MEPUtils.Treeview_test
{
    [Transaction(TransactionMode.Manual)]
    public class TreeviewTest : IExternalCommand
    {
        private static readonly NLog.Logger log = NLog.LogManager.GetCurrentClassLogger();

        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIApplication uiApp = commandData.Application;
            Document doc = commandData.Application.ActiveUIDocument.Document;
            UIDocument uidoc = uiApp.ActiveUIDocument;
            Selection selection = uidoc.Selection;

            //LogManager.Configuration = new NLog.Config.XmlLoggingConfiguration("G:\\Github\\shtirlitsDva\\MyRevitAddins\\MyRevitAddins\\SetTagsModeless\\NLog.config");

            //FilteredElementCollector col = new FilteredElementCollector(doc);

            //List<ElementFilter> catFilter = new List<ElementFilter>();
            //catFilter.Add(new ElementCategoryFilter(BuiltInCategory.OST_PipeFitting));
            //catFilter.Add(new ElementCategoryFilter(BuiltInCategory.OST_PipeAccessory));
            //catFilter.Add(new ElementCategoryFilter(BuiltInCategory.OST_PipeCurves));

            //List<BuiltInCategory> cats = new List<BuiltInCategory>()
            //{
            //    BuiltInCategory.OST_PipeFitting,
            //    BuiltInCategory.OST_PipeAccessory,
            //    BuiltInCategory.OST_PipeCurves
            //};

            //List<ElementFilter> a = new List<ElementFilter>(cats.Count);

            //foreach (BuiltInCategory bic in cats) a.Add(new ElementCategoryFilter(bic));

            //LogicalOrFilter catFilter = new LogicalOrFilter(a);

            //col.WhereElementIsNotElementType().WhereElementIsViewIndependent().WherePasses(catFilter);

            //col.WherePasses(new LogicalAndFilter(new List<ElementFilter>
            //                                        {new LogicalOrFilter(catFilter),
            //                                            new LogicalOrFilter(new List<ElementFilter>
            //                                            {
            //                                                new ElementClassFilter(typeof(Pipe)),
            //                                                new ElementClassFilter(typeof(FamilyInstance))
            //                                            })}));



            //HashSet<Element> els = new HashSet<Element>(col.ToElements());

            //PropertiesInformation[] PropsList = new PropertiesInformation[]
            //                { new PropertiesInformation(true, "System Abbreviation", BuiltInParameter.RBS_DUCT_PIPE_SYSTEM_ABBREVIATION_PARAM),
            //                  new PropertiesInformation(true, "System Name", BuiltInParameter.RBS_SYSTEM_NAME_PARAM),
            //                  new PropertiesInformation(true, "Category Name", BuiltInParameter.ELEM_CATEGORY_PARAM),
            //                  new PropertiesInformation(true, "Family and Type Name", BuiltInParameter.ELEM_FAMILY_AND_TYPE_PARAM) };

            HashSet<Element> els = new HashSet<Element>(
                Shared.Filter.GetElements<Element, BuiltInCategory>(doc, BuiltInCategory.OST_PipeAccessory));

            Element el = els.First();

            var parList = el.GetOrderedParameters();

            Treeview_testForm tvtest = new Treeview_testForm(els, commandData);

            tvtest.ShowDialog();

            return Result.Succeeded;
        }
    }

    public class PropertiesInformation
    {
        public bool IsBuiltIn { get; private set; } = true;
        public string Name { get; private set; }
        public BuiltInParameter Bip { get; private set; }
        public string getBipValue(Element e) => e.get_Parameter(Bip).ToValueString2();
        public PropertiesInformation(bool isBuiltIn, string name, BuiltInParameter bip)
        {
            IsBuiltIn = isBuiltIn; Name = name; Bip = bip;
        }
    }
}
