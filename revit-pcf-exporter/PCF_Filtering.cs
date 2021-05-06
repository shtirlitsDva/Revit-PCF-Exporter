using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Electrical;
using Autodesk.Revit.DB.Mechanical;
using Autodesk.Revit.DB.Plumbing;
//using MoreLinq;
using Shared.BuildingCoder;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace PCF_Functions
{
    class PCF_Filtering
    {
        HashSet<Element> ElementsToFilter;
        public PCF_Filtering(HashSet<Element> elementsToFilter)
        {
            ElementsToFilter = elementsToFilter;
        }
        public HashSet<Element> GetFilteredElements(FilterOptions options)
        {
            IEnumerable<Element> filtering = ElementsToFilter;
            if (options.FilterByDiameter) filtering = filtering.Where(x => Filters.FilterDL(x));
        }
    }

    class FilterOptions
    {
        public bool FilterByDiameter = false;

    }
}
