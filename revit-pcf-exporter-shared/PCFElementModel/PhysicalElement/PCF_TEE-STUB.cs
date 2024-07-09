using Autodesk.Revit.DB;

using Shared;
using PCF_Functions;
using plst = PCF_Functions.Parameters;
using mp = Shared.MepUtils;

using System;
using System.Collections.Generic;
using System.Text;
using Autodesk.Revit.DB.Plumbing;
using System.Linq;

namespace PCF_Model
{
    internal class PCF_TEE_STUB: PcfPhysicalElement
    {
        public PCF_TEE_STUB(Element element) : base(element) { }
        protected override StringBuilder WriteSpecificData()
        {
            StringBuilder sb = new StringBuilder();

            XYZ endPointOriginOletPrimary = Cons.Primary.Origin;
            XYZ endPointOriginOletSecondary = Cons.Secondary.Origin;

            //get reference elements
            Pipe refPipe = null;
            var refCons = mp.GetAllConnectorsFromConnectorSet(Cons.Primary.AllRefs);

            bool isTap = false;
            Connector refCon = refCons.Where(x => x.Owner.IsType<Pipe>()).FirstOrDefault();
            if (refCon == null)
            {
                //Find the target pipe

                IList<BuiltInCategory> bics = new List<BuiltInCategory>(3)
                {
                    BuiltInCategory.OST_PipeAccessory,
                    BuiltInCategory.OST_PipeCurves,
                    BuiltInCategory.OST_PipeFitting
                };

                IList<ElementFilter> a = new List<ElementFilter>(bics.Count());

                foreach (BuiltInCategory bic in bics) a.Add(new ElementCategoryFilter(bic));

                LogicalOrFilter categoryFilter = new LogicalOrFilter(a);

                LogicalAndFilter familyInstanceFilter = new LogicalAndFilter(categoryFilter, new ElementClassFilter(typeof(FamilyInstance)));

                IList<ElementFilter> b = new List<ElementFilter>
                            {
                                new ElementClassFilter(typeof(Pipe)),
                                familyInstanceFilter
                            };
                LogicalOrFilter filter = new LogicalOrFilter(b);

                var view3D = Shared.Filter.Get3DView(doc);

                var refIntersect = new ReferenceIntersector(filter, FindReferenceTarget.All, view3D);
                ReferenceWithContext rwc = refIntersect.FindNearest(Cons.Primary.Origin, Cons.Primary.CoordinateSystem.BasisZ);
                var refId = rwc.GetReference().ElementId;
                Element refElement = doc.GetElement(refId);

                if (refElement is Pipe pipe)
                {
                    refPipe = pipe;
                }
                else
                {
                    //If no reference pipe can be found
                    //The olet could be tapped to a FamilyInstance
                    //Check to see if any of PipeAccessories or PipeFittings
                    //Have the olet as tap
                    HashSet<Element> possibleTappedElements = Shared.Filter.GetElements(
                        doc,
                        new List<BuiltInCategory>() { BuiltInCategory.OST_PipeFitting, BuiltInCategory.OST_PipeAccessory },
                        new List<Type>() { typeof(FamilyInstance), typeof(FamilyInstance) });

                    string oletUid = Element.UniqueId;
                    var query = possibleTappedElements.Where(x =>
                        x.LookupParameter("PCF_ELEM_TAP1").AsString() == oletUid ||
                        x.LookupParameter("PCF_ELEM_TAP2").AsString() == oletUid ||
                        x.LookupParameter("PCF_ELEM_TAP3").AsString() == oletUid);

                    if (query.Count() == 0) throw new Exception(
                        $"Olet {Element.Id} cannot find a reference Pipe!\n" +
                        $"Remember to assign 'olet to PCF_ELEM_TAPX!");
                    else
                    {
                        //It is detected that the olet is a tapping Element
                        isTap = true;

                        sb.Append(EndWriter.WriteTappingOletCP(Cons.Primary, Element.LookupParameter("PCF_ELEM_END1"), query.First()));
                        sb.Append(EndWriter.WriteBP1(Element, Cons.Secondary));
                    }
                }
            }
            else { refPipe = (Pipe)refCon.Owner; }

            //Guard against olet being tapping olet
            if (!isTap)
            {
                Cons refPipeCons = new Cons(refPipe);

                XYZ pipeEnd1 = refPipeCons.Primary.Origin; XYZ pipeEnd2 = refPipeCons.Secondary.Origin;
                XYZ BDvector = Cons.Primary.CoordinateSystem.BasisZ; XYZ ABvector = pipeEnd1 - pipeEnd2;
                double angle = Conversion.RadianToDegree(ABvector.AngleTo(BDvector));
                if (angle > 90)
                {
                    ABvector = -ABvector;
                    angle = Conversion.RadianToDegree(ABvector.AngleTo(BDvector));
                }
                Line refsLine = Line.CreateBound(pipeEnd1, pipeEnd2);

                var projectionPoint = refsLine.Project(Cons.Primary.Origin).XYZPoint;
                
                angle = Math.Round(angle * 100);

                sb.Append(EndWriter.WriteCP(projectionPoint));

                sb.Append(EndWriter.WriteBP1(Element, Cons.Secondary));

                sb.Append("    ANGLE ");
                sb.Append(Conversion.AngleToPCF(angle));
                sb.AppendLine();
            }

            return sb;
        }
    }
}
