using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Plumbing;
using Autodesk.Revit.UI;
using MoreLinq;
using NTR_Functions;
using NTR_Output;
using Shared;
using Shared.BuildingCoder;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Shared
{
    public class SharedStaging
    {
        /// <summary>
        /// Return a 3D view from the given document.
        /// </summary>
        public static View3D Get3DView(Document doc)
        {
            FilteredElementCollector collector = new FilteredElementCollector(doc);

            collector.OfClass(typeof(View3D));

            foreach (View3D v in collector)
            {
                // skip view templates here because they
                // are invisible in project browsers:

                if (v != null && !v.IsTemplate && v.Name == "{3D}")
                {
                    return v;
                }
            }
            return null;
        }
    }

    /// <summary>
    /// Comparer for XYZ points based on their geometric location within a set tolerance.
    /// Tolerance must be in feet.
    /// </summary>
    public class XyzComparer : IEqualityComparer<XYZ>
    {
        double Tol = 1e-6.MmToFt();

        public XyzComparer() {}

        public XyzComparer(double tol) => Tol = tol;

        public bool Equals(XYZ x, XYZ y) => null != x && null != y && x.Equalz(y, Tol);

        public int GetHashCode(XYZ x) => Tuple.Create(
            Math.Round(x.X, 6), Math.Round(x.Y, 6), Math.Round(x.Z, 6)).GetHashCode();
    }
}
