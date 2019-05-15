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
        public static string ComponentClass1(this Element e)
        {
            Parameter par = e.get_Parameter(new Guid("a7f72797-135b-4a1c-8969-e2e3fc76ff14")); //Component Class 1
            if (par == null) return "";
            return par.AsString();
        }
    }
}
