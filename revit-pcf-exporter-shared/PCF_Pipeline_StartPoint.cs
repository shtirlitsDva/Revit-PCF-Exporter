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

namespace PCF_Pipeline
{
    public static class StartPoint
    {
        public static StringBuilder WriteStartPoint(
            string sysAbbr, Dictionary<string, Element> startPoints)
        {
            StringBuilder sb = new StringBuilder();
            if (!startPoints.ContainsKey(sysAbbr)) return sb;
            Element startPoint = startPoints[sysAbbr];
            XYZ elementLocation = ((LocationPoint)startPoint.Location).Point;
            sb.AppendLine($"    START-CO-ORDS {PCF_Functions.EndWriter.PointStringMm(elementLocation)}");
            return sb;
        }
    }
}
