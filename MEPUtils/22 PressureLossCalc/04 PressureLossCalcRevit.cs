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
using System.Globalization;

namespace MEPUtils.PressureLossCalc
{
    public static class RevitInteraction
    {
        private static CultureInfo culture = CultureInfo.CreateSpecificCulture("da-DK");

        public static void PrintSegmentInfo(Document doc)
        {
            culture.NumberFormat.NumberDecimalDigits = 2;

            Guid parGuid = new Guid("6180df8e-5a26-41ce-94ca-4b1933f4a60e");
            SharedParameterElement spe = SharedParameterElement.Lookup(doc, parGuid);

            FilterRule frHasValue = ParameterFilterRuleFactory.CreateHasValueParameterRule(spe.Id);
            //HasValueParameterRule only returns true if no value has ever been assigned
            //to the element. If a value has been assigned once, but since
            //changed to eg. "" then this flag will still be true.
            //So we have to filter for ""
            ElementParameterFilter epfHasValue = new ElementParameterFilter(frHasValue);

            //Create rule for ""
            FilterRule frEmptyString =
                ParameterFilterRuleFactory.CreateEqualsRule(spe.Id, "");
            ElementParameterFilter epfEmptyStringInverted =
                new ElementParameterFilter(frEmptyString, true);

            FilteredElementCollector col = new FilteredElementCollector(doc);
            List<BuiltInCategory> cats = new List<BuiltInCategory>()
            {
                BuiltInCategory.OST_PipeAccessory,
                BuiltInCategory.OST_PipeCurves,
                BuiltInCategory.OST_PipeFitting
            };
            var emf = new ElementMulticategoryFilter(cats);

            col = col.WherePasses(emf).WherePasses(epfHasValue)
                .WherePasses(epfEmptyStringInverted);

            StringBuilder sb = new StringBuilder();

            var systemGroups = col.GroupBy(
                x => x.get_Parameter(
                    BuiltInParameter.RBS_DUCT_PIPE_SYSTEM_ABBREVIATION_PARAM)
                .AsString());

            foreach (var systemGr in systemGroups)
            {
                var strGroups = systemGr.GroupBy(
                    x => x.get_Parameter(parGuid).AsString())
                    .OrderBy(x => x.Key);

                foreach (var strGroup in strGroups)
                {
                    Element el = default;
                    el = strGroup.FirstOrDefault(x => x.IsType<Pipe>());
                    if (el == default)
                        throw new Exception(
                            $"No pipe was found in {systemGr.Key} {strGroup.Key}");

                    #region Read flow
                    Cons cons = new Cons(el);

                    double flow = cons.Primary.Flow
                        .CubicFtPrSecToCubicMtrPrHour();
                    CalcPressureLoss.currentFlow = flow;
                    #endregion

                    #region Calculate pipes
                    var pipeQuery = strGroup
                        .Where(x => x.IsType<Pipe>())
                        .Cast<Pipe>()
                        .GroupBy(x => (int)x.Diameter.FtToMm().Round());

                    foreach (var pGroup in pipeQuery)
                    {
                        Pipe pipe = pGroup.FirstOrDefault();
                        double iDia = pipe
                            .get_Parameter(BuiltInParameter.RBS_PIPE_INNER_DIAM_PARAM)
                            .AsDouble().FtToMtrs();
                        CalcPressureLoss.currentInsideDiameter = iDia;
                        double pLoss = CalcPressureLoss.CalculatePressureLoss();

                        double length = pGroup.Cast<Pipe>().Sum(x => x.Length());
                        pLoss = pLoss * length;

                        sb.AppendLine(
                            $"{systemGr.Key};{strGroup.Key};{flow};{pGroup.Key};{length.ToString("0.00")};{pLoss}");
                    }
                    #endregion

                    #region Calculate fitings

                    #endregion
                }
            }
            Output.WriteDebugFile(
                @"X:\AutoCAD DRI - Revit\Addins\Pressure Loss\info.txt",
                sb);
        }
    }
}
