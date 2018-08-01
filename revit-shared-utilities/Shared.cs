using System;
using System.Collections.Generic;
using System.Linq;
using MoreLinq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Mechanical;
using Autodesk.Revit.DB.Plumbing;

namespace Shared
{
    public static class Filter
    {
        /// <summary>
        /// Generic Parameter value filter. An attempt to write a generic method,
        /// that returns an element filter consumed by FilteredElementCollector.
        /// </summary>
        /// <typeparam name="T1">Type of the parameter VALUE to filter by.</typeparam>
        /// <typeparam name="T2">Type of the PARAMETER to filter.</typeparam>
        /// <param name="value">Currently: string, bool.</param>
        /// <param name="parameterId">Currently: Guid, BuiltInCategory.</param>
        /// <returns>ElementParameterFilter consumed by FilteredElementCollector.</returns>
        public static ElementParameterFilter ParameterValueGenericFilter<T1, T2>(Document doc, T1 value, T2 parameterId)
        {
            //Initialize ParameterValueProvider
            ParameterValueProvider pvp = null;
            switch (parameterId)
            {
                case BuiltInParameter bip:
                    pvp = new ParameterValueProvider(new ElementId((int)bip));
                    break;
                case Guid guid:
                    SharedParameterElement spe = SharedParameterElement.Lookup(doc, guid);
                    pvp = new ParameterValueProvider(spe.Id);
                    break;
                default:
                    throw new NotImplementedException("ParameterValueGenericFilter: T2 (parameter) type not implemented!");
            }

            //Branch off to value types
            switch (value)
            {
                case string str:
                    FilterStringRuleEvaluator fsrE = new FilterStringEquals();
                    FilterStringRule fsr = new FilterStringRule(pvp, fsrE, str, false);
                    return new ElementParameterFilter(fsr);
                case bool bol:
                    int _value;

                    if (bol == true) _value = 1;
                    else _value = 0;

                    FilterNumericRuleEvaluator fnrE = new FilterNumericEquals();
                    FilterIntegerRule fir = new FilterIntegerRule(pvp, fnrE, _value);
                    return new ElementParameterFilter(fir);
                default:
                    throw new NotImplementedException("ParameterValueGenericFilter: T1 (value) type not implemented!");
            }
        }

        public static FilteredElementCollector GetElementsWithConnectors(Document doc, bool mechEq = false)
        {
            // what categories of family instances
            // are we interested in?
            // From here: http://thebuildingcoder.typepad.com/blog/2010/06/retrieve-mep-elements-and-connectors.html

            IList<BuiltInCategory> bics = new List<BuiltInCategory>(3)
            {
                //BuiltInCategory.OST_CableTray,
                //BuiltInCategory.OST_CableTrayFitting,
                //BuiltInCategory.OST_Conduit,
                //BuiltInCategory.OST_ConduitFitting,
                //BuiltInCategory.OST_DuctCurves,
                //BuiltInCategory.OST_DuctFitting,
                //BuiltInCategory.OST_DuctTerminal,
                //BuiltInCategory.OST_ElectricalEquipment,
                //BuiltInCategory.OST_ElectricalFixtures,
                //BuiltInCategory.OST_LightingDevices,
                //BuiltInCategory.OST_LightingFixtures,
                //BuiltInCategory.OST_MechanicalEquipment,
                BuiltInCategory.OST_PipeAccessory,
                BuiltInCategory.OST_PipeCurves,
                BuiltInCategory.OST_PipeFitting,
                //BuiltInCategory.OST_PlumbingFixtures,
                //BuiltInCategory.OST_SpecialityEquipment,
                //BuiltInCategory.OST_Sprinklers,
                //BuiltInCategory.OST_Wire
            };

            if (mechEq) bics.Add(BuiltInCategory.OST_MechanicalEquipment);

            IList<ElementFilter> a = new List<ElementFilter>(bics.Count());

            foreach (BuiltInCategory bic in bics) a.Add(new ElementCategoryFilter(bic));

            LogicalOrFilter categoryFilter = new LogicalOrFilter(a);

            LogicalAndFilter familyInstanceFilter = new LogicalAndFilter(categoryFilter, new ElementClassFilter(typeof(FamilyInstance)));

            //IList<ElementFilter> b = new List<ElementFilter>(6);
            IList<ElementFilter> b = new List<ElementFilter>
            {

                //b.Add(new ElementClassFilter(typeof(CableTray)));
                //b.Add(new ElementClassFilter(typeof(Conduit)));
                //b.Add(new ElementClassFilter(typeof(Duct)));
                new ElementClassFilter(typeof(Pipe)),

                familyInstanceFilter
            };
            LogicalOrFilter classFilter = new LogicalOrFilter(b);

            FilteredElementCollector collector = new FilteredElementCollector(doc);

            collector.WherePasses(classFilter);

            return collector;
        }
    }

    public static class MepUtils
    {
        public static IList<string> GetDistinctPhysicalPipingSystemTypeNames(Document doc)
        {
            FilteredElementCollector collector = new FilteredElementCollector(doc);
            HashSet<PipingSystem> pipingSystems = collector.OfClass(typeof(PipingSystem)).Cast<PipingSystem>().ToHashSet();
            HashSet<PipingSystemType> pipingSystemTypes = pipingSystems.Select(ps => doc.GetElement(ps.GetTypeId())).Cast<PipingSystemType>().ToHashSet();

            //Following code takes care if PCF_PIPL_EXCL has not been properly imported.
            PipingSystemType pstype = pipingSystemTypes.FirstOrDefault();
            if (pstype == null) throw new Exception("No piping systems created yet! Draw some pipes.");

            HashSet<string> abbreviations;
            //Do not allow systems with PCF_PIPL_EXCL if it exists!
            //GUID is defined in PCF_Exporter ParameterList!!!
            //Do not change w/o coordination
            if (pstype.get_Parameter(new Guid("C1C2C9FE-2634-42BA-89D0-5AF699F54D4C")) == null)
            {
                //If parameter doesn't exist, get all systems
                abbreviations = pipingSystemTypes.Select(pst => pst.Abbreviation).ToHashSet();
            }
            else
            {
                //If parameter exists, take only not excluded
                abbreviations = pipingSystemTypes
                      .Where(pst => pst.get_Parameter(new Guid("C1C2C9FE-2634-42BA-89D0-5AF699F54D4C")).AsInteger() == 0) //Filter out EXCLUDED piping systems
                      .Select(pst => pst.Abbreviation).ToHashSet();
            }

            return abbreviations.Distinct().ToList();
        }
    }
}
