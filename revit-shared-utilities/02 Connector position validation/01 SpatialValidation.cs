using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Plumbing;
using Autodesk.Revit.DB.Structure;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
//using MoreLinq;
using static MoreLinq.Extensions.MaxByExtension;
using Shared;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.Serialization;
using System.Windows.Input;
using System.Runtime.Serialization.Json;
using dbg = Shared.Dbg;
using fi = Shared.Filter;
using mp = Shared.MepUtils;
using tr = Shared.Transformation;

namespace Shared.Tools
{
    class SpatialValidation
    {
        private const int precision = 1;

        public static Result ValidateConnectorsSpatially(ExternalCommandData cData)
        {
            bool ctrl = false;
            //bool shft = false;
            if ((int)Keyboard.Modifiers == 2) ctrl = true;
            //if ((int)Keyboard.Modifiers == 4) shft = true;

            UIApplication uiApp = cData.Application;
            Document doc = cData.Application.ActiveUIDocument.Document;
            UIDocument uidoc = uiApp.ActiveUIDocument;

            //Gather all connectors from the document
            //Filter also out all "Curve" connectors, which are olet ends at pipe cntr.
            HashSet<Connector> AllCons = mp.GetALLConnectorsInDocument(doc)
                .ExceptWhere(c => c.ConnectorType == ConnectorType.Curve).ToHashSet();
            //if (ctrl)
            AllCons = AllCons.ExceptWhere(c => c.MEPSystemAbbreviation(doc, true) == "ARGD").ToHashSet();

            //Create collection with distinct connectors with a set tolerance
            double Tol = 3.0.MmToFt();
            var DistinctCons = AllCons.ToHashSet(new ConnectorXyzComparer(Tol));

            List<connectorSpatialGroup> csgList = new List<connectorSpatialGroup>();

            foreach (Connector distinctCon in DistinctCons)
            {
                csgList.Add(new connectorSpatialGroup(AllCons.Where(x => distinctCon.Equalz(x, Tol))));
                AllCons = AllCons.ExceptWhere(x => distinctCon.Equalz(x, Tol)).ToHashSet();
            }

            foreach (var g in csgList)
            {
                g.pairs = g.Connectors
                           .SelectMany((fst, i) => g.Connectors.Skip(i + 1)
                           .Select(snd => (fst, snd, fst.Origin.DistanceTo(snd.Origin))))
                           .ToList();
                g.longestPair = g.pairs.MaxBy(x => x.dist).FirstOrDefault();

                g.longestDist = g.longestPair.dist.FtToMm().Round(4);
            }

            csgList.Sort((y, x) => x.longestDist.CompareTo(y.longestDist));

            List<string> results = new List<string>();

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
                        results.Add($"{g.longestDist}\n");
                        foreach (var c in g.Connectors)
                        {
                            string s = PointStringMm(c.Origin, precision);
                            results.Add($"{s} {c.Owner.Id.ToString()}\n");
                        }
                        results.Add("\n");
                    }
                }
            }

            Shared.BuildingCoder.BuildingCoderUtilities.InfoMsg(string.Join(string.Empty, results));

            return Result.Succeeded;
        }

        internal static string PointStringMm(XYZ p, int precision)
        {
            return string.Concat(
                Math.Round(p.X.FtToMm(), precision, MidpointRounding.AwayFromZero).ToString("#." + new string('0', precision), CultureInfo.GetCultureInfo("en-GB")), " ",
                Math.Round(p.Y.FtToMm(), precision, MidpointRounding.AwayFromZero).ToString("#." + new string('0', precision), CultureInfo.GetCultureInfo("en-GB")), " ",
                Math.Round(p.Z.FtToMm(), precision, MidpointRounding.AwayFromZero).ToString("#." + new string('0', precision), CultureInfo.GetCultureInfo("en-GB")));
        }
    }

    [DataContract]
    internal class connectorSpatialGroup
    {
        public List<Connector> Connectors = new List<Connector>();
        [DataMember]
        public int nrOfCons = 0;
        [DataMember] //More of a debug property, maybe should be removed later on
        public List<string> SpecList = new List<string>();
        [DataMember] //More of a debug property
        List<string> ListOfIds = null;
        [DataMember]
        public double longestDist = 0;
        public List<(Connector c1, Connector c2, double dist)> pairs;
        public (Connector c1, Connector c2, double dist) longestPair;

        internal connectorSpatialGroup(IEnumerable<Connector> collection)
        {
            Connectors = collection.ToList();
            nrOfCons = Connectors.Count();
            ListOfIds = new List<string>(nrOfCons);
            foreach (Connector con in collection)
            {
                Element owner = con.Owner;
                Parameter par = owner.get_Parameter(new Guid("90be8246-25f7-487d-b352-554f810fcaa7")); //PCF_ELEM_SPEC parameter
                SpecList.Add(par.AsString());
                ListOfIds.Add(owner.Id.ToString());
            }
        }
    }
}
