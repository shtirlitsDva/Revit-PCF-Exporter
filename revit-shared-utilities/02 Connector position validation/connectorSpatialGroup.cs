using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

using Autodesk.Revit.DB;

namespace Shared.Tools
{
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
                if (par != null) { SpecList.Add(par.AsString()); }
                ListOfIds.Add(owner.Id.ToString());
            }
        }
    }
}
