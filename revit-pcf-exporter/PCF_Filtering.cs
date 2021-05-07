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
using Shared;

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
            if (options.FilterOutInstrumentPipes)
                filtering = filtering.ExceptWhere(x => x.get_Parameter(
                    BuiltInParameter.RBS_DUCT_PIPE_SYSTEM_ABBREVIATION_PARAM).AsString() == "INSTR");
            if (options.FilterOutSpecifiedPCF_ELEM_SPEC)
            {
                filtering = from element in filtering
                            let par = element.get_Parameter(plst.PCF_ELEM_SPEC.Guid)
                            where par != null && par.AsString() == InputVars.PCF_ELEM_SPEC_FILTER
                            select element;
            }
        }
    }

    class FilterOptions
    {
        public bool FilterByDiameter = false;
        public bool FilterByPCF_ELEM_EXCL = false;
        public bool FilterByPCF_PIPL_EXCL = false;
        public bool FilterOutInstrumentPipes = false;
        public bool FilterOutSpecifiedPCF_ELEM_SPEC = false;
    }
}
