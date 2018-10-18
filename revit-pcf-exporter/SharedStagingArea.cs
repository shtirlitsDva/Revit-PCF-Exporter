using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Plumbing;
using Autodesk.Revit.UI;
using MoreLinq;
using PCF_Functions;
using PCF_Output;
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
    }
}
