using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Plumbing;

using Shared;

using mp = Shared.MepUtils;

namespace PcfExporter.Model
{
    /// <summary>
    /// TEE-STUB: a branch fitting welded onto a host pipe. Finds the host pipe
    /// (directly connected, by ray-cast, or via tap registration) and writes the
    /// centre point on the pipe axis, the branch point and the branch angle.
    /// PCF_OLET derives from this and replaces the centre-point construction.
    /// </summary>
    internal class PCF_TEE_STUB : PcfPhysicalElement
    {
        public PCF_TEE_STUB(Element element, ExportSession s) : base(element, s) { }

        protected override StringBuilder WriteSpecificData()
        {
            var sb = new StringBuilder();

            //get reference elements
            Pipe refPipe = null;
            var refCons = mp.GetAllConnectorsFromConnectorSet(Cons.Primary.AllRefs);

            bool isTap = false;
            Connector refCon = refCons.Where(x => x.Owner.IsType<Pipe>()).FirstOrDefault();
            if (refCon == null)
            {
                //Find the target pipe by shooting a ray along the primary connector axis
                IList<BuiltInCategory> bics = new List<BuiltInCategory>(3)
                {
                    BuiltInCategory.OST_PipeAccessory,
                    BuiltInCategory.OST_PipeCurves,
                    BuiltInCategory.OST_PipeFitting
                };

                IList<ElementFilter> categoryFilters = bics
                    .Select(bic => (ElementFilter)new ElementCategoryFilter(bic)).ToList();

                var familyInstanceFilter = new LogicalAndFilter(
                    new LogicalOrFilter(categoryFilters),
                    new ElementClassFilter(typeof(FamilyInstance)));

                var filter = new LogicalOrFilter(new List<ElementFilter>
                {
                    new ElementClassFilter(typeof(Pipe)),
                    familyInstanceFilter
                });

                var view3D = Shared.Filter.Get3DView(doc);

                var refIntersect = new ReferenceIntersector(filter, FindReferenceTarget.All, view3D);
                ReferenceWithContext rwc = refIntersect.FindNearest(
                    Cons.Primary.Origin, Cons.Primary.CoordinateSystem.BasisZ);
                Element refElement = doc.GetElement(rwc.GetReference().ElementId);

                if (refElement is Pipe pipe)
                {
                    refPipe = pipe;
                }
                else
                {
                    //If no reference pipe can be found the olet could be tapped to a
                    //FamilyInstance: check if any fitting/accessory has it as a tap.
                    HashSet<Element> possibleTappedElements = Shared.Filter.GetElements(
                        doc,
                        new List<BuiltInCategory>() { BuiltInCategory.OST_PipeFitting, BuiltInCategory.OST_PipeAccessory },
                        new List<Type>() { typeof(FamilyInstance), typeof(FamilyInstance) });

                    string oletUid = Element.UniqueId;
                    var query = possibleTappedElements.Where(x =>
                        x.LookupParameter("PCF_ELEM_TAP1").AsString() == oletUid ||
                        x.LookupParameter("PCF_ELEM_TAP2").AsString() == oletUid ||
                        x.LookupParameter("PCF_ELEM_TAP3").AsString() == oletUid);

                    if (!query.Any()) throw new Exception(
                        $"Olet {Element.Id} cannot find a reference Pipe!\n" +
                        $"Remember to assign olet to PCF_ELEM_TAPX!");

                    //It is detected that the olet is a tapping element
                    isTap = true;

                    sb.Append(EW.WriteTappingOletCP(
                        Cons.Primary, Element.LookupParameter("PCF_ELEM_END1"), query.First()));
                    sb.Append(EW.WriteBP1(Element, Cons.Secondary));
                }
            }
            else { refPipe = (Pipe)refCon.Owner; }

            //Guard against olet being tapping olet
            if (!isTap) sb.Append(WriteNonTappedData(refPipe));

            return sb;
        }

        /// <summary>
        /// Centre point, branch point and angle for a stub sitting on a host pipe.
        /// TEE-STUB uses a plain projection of the primary connector onto the pipe axis.
        /// </summary>
        protected virtual StringBuilder WriteNonTappedData(Pipe refPipe)
        {
            var sb = new StringBuilder();

            var refPipeCons = new Cons(refPipe);

            XYZ pipeEnd1 = refPipeCons.Primary.Origin;
            XYZ pipeEnd2 = refPipeCons.Secondary.Origin;
            XYZ BDvector = Cons.Primary.CoordinateSystem.BasisZ;
            XYZ ABvector = pipeEnd1 - pipeEnd2;
            double angle = Conversion.RadianToDegree(ABvector.AngleTo(BDvector));
            if (angle > 90)
            {
                ABvector = -ABvector;
                angle = Conversion.RadianToDegree(ABvector.AngleTo(BDvector));
            }
            Line refsLine = Line.CreateBound(pipeEnd1, pipeEnd2);
            XYZ projectionPoint = refsLine.Project(Cons.Primary.Origin).XYZPoint;

            angle = Math.Round(angle * 100);

            sb.Append(EW.WriteCP(projectionPoint));
            sb.Append(EW.WriteBP1(Element, Cons.Secondary));
            sb.Append("    ANGLE ");
            sb.Append(Conversion.AngleToPCF(angle));
            sb.AppendLine();

            return sb;
        }
    }
}
