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

using plst = PCF_Functions.ParameterList;

namespace PCF_Functions
{
    class PCF_Filtering
    {
        HashSet<Element> ElementsToFilter;
        public PCF_Filtering(HashSet<Element> elementsToFilter)
        {
            ElementsToFilter = elementsToFilter;
        }
        public HashSet<Element> GetFilteredElements(Document doc, FilterOptions options)
        {
            IEnumerable<Element> filtering = ElementsToFilter;
            if (options.FilterByDiameter) filtering = filtering.Where(x => Filters.FilterDL(x));
            if (options.FilterByPCF_ELEM_EXCL)
            {
                filtering = from element in filtering
                            let par = element.get_Parameter(plst.PCF_ELEM_EXCL.Guid)
                            where par != null && par.AsInteger() == 0
                            select element;
            }
            if (options.FilterByPCF_PIPL_EXCL)
            {
                filtering = filtering.Where(x => x.PipingSystemAllowed(doc) == true);
            }
        }
    }

    class FilterOptions
    {
        public bool FilterByDiameter = false;
        public bool FilterByPCF_ELEM_EXCL = false;
        public bool FilterByPCF_PIPL_EXCL = false;
    }
}
