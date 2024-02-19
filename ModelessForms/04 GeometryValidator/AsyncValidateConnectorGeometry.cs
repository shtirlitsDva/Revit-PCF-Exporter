using System;
using System.Collections.Generic;
using System.Linq;
using MoreLinq;
using System.Text;

using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

using Shared;
using Shared.Tools;
using mp = Shared.MepUtils;
using System.Globalization;
using Autodesk.Revit.UI.Selection;

namespace ModelessForms.GeometryValidator
{
    public class AsyncValidateConnectorGeometry : IAsyncCommand
    {
        private ConnectorValidationContainer Payload;
        private const int precision = 1;
        private AsyncValidateConnectorGeometry() { }
        public AsyncValidateConnectorGeometry(ConnectorValidationContainer payload) => Payload = payload;
        public void Execute(UIApplication uiApp)
        {
            Document doc = uiApp.ActiveUIDocument.Document;
            UIDocument uidoc = uiApp.ActiveUIDocument;

            List<ConnectorValidationResult> results = new List<ConnectorValidationResult>();

            FilteredElementCollector col = new FilteredElementCollector(doc);

            HashSet<Connector> AllCons = mp.GetALLConnectorsInDocument(doc)
                .ExceptWhere(c => c.ConnectorType == ConnectorType.Curve).ToHashSet();
            AllCons = AllCons.ExceptWhere(c => c.MEPSystemAbbreviation(doc, true) == "ARGD").ToHashSet();

            if (Payload.SystemToValidate != "All")
                AllCons = AllCons.Where(
                    x => x.MEPSystemAbbreviation(doc, true) ==
                    Payload.SystemToValidate).ToHashSet();

            //Create collection with distinct connectors with a set tolerance
            double Tol = 7.0.MmToFt();
            var DistinctCons = AllCons.ToHashSet(new ConnectorXyzComparer(Tol));

            List<connectorSpatialGroup> csgList = new List<connectorSpatialGroup>();

            foreach (Connector distinctCon in DistinctCons)
            {
                csgList.Add(new connectorSpatialGroup(AllCons.Where(x => distinctCon.Equalz(x, Tol))));
                AllCons = AllCons.ExceptWhere(x => distinctCon.Equalz(x, Tol)).ToHashSet();
            }

            csgList = csgList.ExceptWhere(x => x.Connectors.Count < 2).ToList();

            foreach (var g in csgList)
            {
                g.pairs = g.Connectors
                           .SelectMany((fst, i) => g.Connectors.Skip(i + 1)
                           .Select(snd => (fst, snd, fst.Origin.DistanceTo(snd.Origin))))
                           .ToList();
                if (g.Connectors.Count > 1)
                {
                    g.longestPair = g.pairs.MaxBy(x => x.dist);
                    g.longestDist = g.longestPair.dist.FtToMm().Round(4);
                }
            }

            csgList.Sort((y, x) => x.longestDist.CompareTo(y.longestDist));

            foreach (var g in csgList)
            {
                if (g.longestDist > 0.001)
                {
                    //Element owner1 = g.longestPair.c1.Owner;
                    //Element owner2 = g.longestPair.c2.Owner;
                    //string intermediateResult = $"{owner1.Name}: {owner1.Id} - {owner2.Name}: {owner2.Id} => {g.longestDist} mm\n";
                    //results.Add(intermediateResult);

                    //This check (if(), s1, s2) is to detect wether the coordinates will display differently in exported (ntr, pcf) text which causes problems
                    //The goal is to have all geometric coordinates have same string value
                    //If the distance between connectors too small to register in the string value -> we don't care (i think)
                    bool coordinatesDiffer = false;

                    foreach (var pair in g.pairs)
                    {
                        string s1 = PointStringMm(pair.c1.Origin, precision);
                        string s2 = PointStringMm(pair.c2.Origin, precision);
                        if (s1 != s2) coordinatesDiffer = true;
                    }
                    if (coordinatesDiffer)
                    {
                        ConnectorValidationResult result = new ConnectorValidationResult();

                        result.LongestDist = g.longestDist;
                        foreach (var c in g.Connectors)
                        {
                            string s = PointStringMm(c.Origin, precision);
                            result.Data.Add((s, c.Owner.Id));
                        }
                        results.Add(result);
                    }
                }
            }

            foreach (var item in results)
                Payload.ValidationResult.Add(item);

            RaiseValidationOperationComplete();
        }
        public event EventHandler ValidationOperationComplete;
        public void RaiseValidationOperationComplete()
            => ValidationOperationComplete.Raise(this, new MyEventArgs("Operation complete!"));
        internal static string PointStringMm(XYZ p, int precision)
        {
            return string.Concat(
                Math.Round(p.X.FtToMm(), precision, MidpointRounding.AwayFromZero).ToString("#." + new string('0', precision), CultureInfo.GetCultureInfo("en-GB")), " ",
                Math.Round(p.Y.FtToMm(), precision, MidpointRounding.AwayFromZero).ToString("#." + new string('0', precision), CultureInfo.GetCultureInfo("en-GB")), " ",
                Math.Round(p.Z.FtToMm(), precision, MidpointRounding.AwayFromZero).ToString("#." + new string('0', precision), CultureInfo.GetCultureInfo("en-GB")));
        }
    }
    public class AsyncGetSystemAbbreviations : IAsyncCommand
    {
        public List<string> SysAbbrs { get; set; }
        public AsyncGetSystemAbbreviations() { }

        public void Execute(UIApplication uiApp)
        {
            Document doc = uiApp.ActiveUIDocument.Document;

            HashSet<Connector> AllCons = mp.GetALLConnectorsInDocument(doc)
                .ExceptWhere(c => c.ConnectorType == ConnectorType.Curve).ToHashSet();
            AllCons = AllCons.ExceptWhere(c => c.MEPSystemAbbreviation(doc, true) == "ARGD").ToHashSet();

            SysAbbrs.Clear();

            var localSysAbbrs = AllCons
                    .Select(x => x.MEPSystemAbbreviation(doc, true))
                    .Distinct()
                    .OrderBy(x => x)
                    .ToList();

            foreach (var x in localSysAbbrs) SysAbbrs.Add(x);

            RaiseValidationOperationComplete();
        }

        public event EventHandler CollectionOfSysAbbrsComplete;
        public void RaiseValidationOperationComplete()
            => CollectionOfSysAbbrsComplete.Raise(this, new MyEventArgs("Operation complete!"));
    }
    class AsyncSelectElements : IAsyncCommand
    {
        public List<ElementId> ElementIdList { get; private set; }
        private AsyncSelectElements() { }
        public AsyncSelectElements(List<ElementId> elementIdList)
        {
            ElementIdList = elementIdList;
        }

        public void Execute(UIApplication uiApp)
        {
            Document doc = uiApp.ActiveUIDocument.Document;
            UIDocument uidoc = uiApp.ActiveUIDocument;
            Selection selection = uidoc.Selection;

            selection.SetElementIds(ElementIdList);
            uiApp.ActiveUIDocument.ShowElements(ElementIdList);
        }
    }
}
