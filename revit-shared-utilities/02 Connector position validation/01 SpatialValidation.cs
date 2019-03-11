using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using MoreLinq;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Plumbing;
using Autodesk.Revit.DB.Structure;
using Shared;
using fi = Shared.Filter;
using tr = Shared.Transformation;
using mp = Shared.MepUtils;
using dbg = Shared.Dbg;

namespace Shared.Tools
{
    class SpatialValidation
    {
        public static Result ValidateConnectorsSpatially(ExternalCommandData cData)
        {
            UIApplication uiApp = cData.Application;
            Document doc = cData.Application.ActiveUIDocument.Document;
            UIDocument uidoc = uiApp.ActiveUIDocument;

            //Gather all connectors from the document
            HashSet<Connector> AllCons = mp.GetALLConnectorsInDocument(doc);

            //Create collection with distinct connectors
            var DistinctCons = AllCons.ToHashSet(new ConnectorXyzComparer());

            List<connectorSpatialGroup> csgList = new List<connectorSpatialGroup>();

            foreach (Connector distinctCon in DistinctCons)
            {
                csgList.Add(new connectorSpatialGroup(AllCons.Where(x => distinctCon.Equalz(x, Shared.Extensions._1mmTol))));
                AllCons = AllCons.ExceptWhere(x => distinctCon.Equalz(x, Shared.Extensions._1mmTol)).ToHashSet();
            }

            List<string> results = new List<string>();

            foreach (var g in csgList)
            {
                List<(Connector c1, Connector c2, double dist)> pairs = g.Connectors
                                        .SelectMany((fst, i) => g.Connectors.Skip(i + 1)
                                        .Select(snd => (fst, snd, fst.Origin.DistanceTo(snd.Origin))))
                                        .ToList();
                var longest = pairs.MaxBy(x => x.dist).FirstOrDefault();

                double longestDist = longest.dist.FtToMm();

                if (longestDist > 0.1)
                {
                    Element owner1 = longest.c1.Owner;
                    Element owner2 = longest.c2.Owner;
                    string intermediateResult = string.Concat(owner1.Name, ": ", owner1.Id, " => ", longestDist, " mm\n");
                    results.Add(intermediateResult);
                }
            }

            Shared.BuildingCoder.BuildingCoderUtilities.InfoMsg(string.Join(string.Empty, results));

            return Result.Succeeded;
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
