using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Plumbing;
using Autodesk.Revit.UI;
using MoreLinq;
using PCF_Functions;
using PCF_Output;
using Shared;
using Shared.BuildingCoder;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Shared
{
    public static class SharedStagingArea
    {
        public static string FamilyName(this Element e) => e.get_Parameter(BuiltInParameter.ELEM_FAMILY_PARAM).AsValueString();

        /// <summary>
        /// Return a hash string for a real number formatted to nine decimal places.
        /// </summary>
        public static string HashString(double a) => a.ToString("0.#########");

        /// <summary>
        /// Return a hash string for an XYZ point or vector with its coordinates
        /// formatted to nine decimal places.
        /// </summary>
        public static string HashString(XYZ p)
        {
            return string.Format("({0},{1},{2})", HashString(p.X), HashString(p.Y), HashString(p.Z));
        }

        public static HashSet<Connector> GetALLConnectorsFromElements(HashSet<Element> elements, IEqualityComparer<Connector> comparer)
        {
            return (from e in elements from Connector c in Shared.MepUtils.GetConnectorSet(e) select c).ToHashSet(comparer);
        }
    }

    public class ConnectorXyzComparer : IEqualityComparer<Connector>
    {
        public bool Equals(Connector x, Connector y)
        {
            return null != x && null != y && Shared.Extensions.IsEqual(x.Origin, y.Origin);
        }

        public int GetHashCode(Connector x) => SharedStagingArea.HashString(x.Origin).GetHashCode();
    }



}
