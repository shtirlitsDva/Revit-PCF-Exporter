using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Data;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Plumbing;
using Shared.BuildingCoder;
using Shared;
using PCF_Model;

namespace PCF_Pipeline
{
    public static class StartPoint
    {
        internal static StringBuilder WriteStartPoint(
            string sysAbbr, HashSet<IPcfElement> startPoints)
        {
            StringBuilder sb = new StringBuilder();
            if (!startPoints.Any(x => x.SystemAbbreviation == sysAbbr)) return sb;

            var sps = startPoints.Where(x => x.SystemAbbreviation == sysAbbr);
            if (sps.Count() > 1)
            {
                throw new Exception($"Multiple start points ({sps.Count()}) for the same system {sysAbbr}!\n" +
                    $"{string.Join("\n", sps.Select(x => x.ElementId))}");
            }

            var startPoint = sps.First() as PCF_VIRTUAL_STARTPOINT;
            sb.AppendLine($"    START-CO-ORDS {PCF_Functions.EndWriter.PointStringMm(startPoint.Location)}");
            return sb;
        }
    }
}
