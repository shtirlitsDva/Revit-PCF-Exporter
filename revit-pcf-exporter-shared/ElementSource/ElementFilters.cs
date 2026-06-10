using System;

using Autodesk.Revit.DB;

using PcfExporter.Configuration;

using Shared;

using plst = PcfExporter.Model.Parameters;

namespace PcfExporter.ElementSource
{
    /// <summary>
    /// Element-level predicates used when filtering the export set.
    /// </summary>
    public static class ElementFilters
    {
        /// <summary>
        /// True when the element's nominal size (pipe diameter or governing connector)
        /// is at or above the configured diameter limit.
        /// </summary>
        public static bool PassesDiameterLimit(Element element, PcfConfiguration cfg)
        {
            double testedDiameter = 0;
            switch (element)
            {
                case MEPCurve pipe:
                    testedDiameter = cfg.BoreUnits == BoreUnits.Mm
                        ? pipe.Diameter.FtToMm().Round()
                        : pipe.Diameter.FtToInch().Round(3);
                    break;
                case FamilyInstance _:
                    Connector testedConnector = null;
                    var cons = MepUtils.GetConnectors(element);
                    if (cons.Primary == null) break;
                    else if (cons.Count == 0) break;
                    else if (cons.Count == 1 || cons.Count > 2) testedConnector = cons.Primary;
                    else if (cons.Count == 2) testedConnector = cons.Largest ?? cons.Primary; //Largest is only defined for reducers

                    testedDiameter = cfg.BoreUnits == BoreUnits.Mm
                        ? (testedConnector.Radius * 2).FtToMm().Round()
                        : (testedConnector.Radius * 2).FtToInch().Round(3);
                    break;
            }
            return testedDiameter >= cfg.DiameterLimit;
        }

        /// <summary>
        /// True when the element's piping system type is not excluded via PCF_PIPL_EXCL.
        /// </summary>
        public static bool PipingSystemAllowed(this Element elem, Document doc)
        {
            Element pipingSystemType = elem.PipingSystemType(doc);
            if (pipingSystemType == null) return false;
            Parameter exclusion = pipingSystemType.get_Parameter(plst.PCF_PIPL_EXCL.Guid);
            if (exclusion == null) throw new Exception(
                "PipingSystemAllowed cannot access PCF_PIPL_EXCL! Does the parameter exist in the project?");
            return exclusion.AsInteger() == 0;
        }

        public static Element PipingSystemType(this Element e, Document doc) =>
            doc.GetElement(e.get_Parameter(BuiltInParameter.RBS_PIPING_SYSTEM_TYPE_PARAM).AsElementId());
    }
}
