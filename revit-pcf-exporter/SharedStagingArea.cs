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

        /// <summary>
        /// Method is taken from here:
        /// https://spiderinnet.typepad.com/blog/2011/08/revit-parameter-api-asvaluestring-tostring-tovaluestring-and-tovaluedisplaystring.html
        /// </summary>
        /// <param name="p">Revit parameter</param>
        /// <returns>Stringified contents of the parameter</returns>
        internal static string ToValueString(this Autodesk.Revit.DB.Parameter p)
        {
            string ret = string.Empty;

            switch (p.StorageType)
            {
                case StorageType.ElementId:
                    ret = p.AsElementId().ToString();
                    break;
                case StorageType.Integer:
                    ret = p.AsInteger().ToString();
                    break;
                case StorageType.String:
                    ret = p.AsString();
                    break;
                case StorageType.Double:
                    ret = p.AsValueString();
                    break;
                default:
                    break;
            }

            return ret;
        }
    }
}
