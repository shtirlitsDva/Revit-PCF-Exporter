using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using NTR_Functions;
using Shared;
using dw = NTR_Functions.DataWriter;

namespace NTR_Exporter
{
    internal class NTR_Steel
    {
        private Document doc;

        public NTR_Steel(Document doc)
        {
            this.doc = doc;
        }

        internal StringBuilder ExportSteel()
        {
            StringBuilder sb = new StringBuilder();

            //Collect in-model steel elements' analytical stick models
            var AllAnalyticalModelSticks = Shared.Filter
                .GetElements<AnalyticalModelStick, BuiltInCategory>(doc, BuiltInCategory.INVALID);

            List<AnalyticalSteelElement> ASE_OriginalList = new List<AnalyticalSteelElement>();
            List<AnalyticalSteelElement> ASE_NewElementsList = new List<AnalyticalSteelElement>();

            foreach (AnalyticalModelStick ams in AllAnalyticalModelSticks)
            {
                AnalyticalSteelElement ase = new AnalyticalSteelElement(doc, ams);
                ASE_OriginalList.Add(ase);
            }

            #region Internal supports (supports on steels frames)
            //Create additional analytical stick elements for supports on steel frames
            //The GUID is for TAG 4
            //Possible non defined behaviour:
            //This function gets ALL supports in model
            //While nodes are created only for the current pipeLine
            //So right now this only works either for the whole model
            //Or only for a specific pipeline where the model has been extracted to separate project
            var SteelSupports = Shared.Filter.GetElements<FamilyInstance, Guid>
                (doc, new Guid("f96a5688-8dbe-427d-aa62-f8744a6bc3ee"), "FRAME");

            foreach (FamilyInstance fi in SteelSupports)
            {
                string familyName = fi.get_Parameter(BuiltInParameter.ELEM_FAMILY_PARAM).AsValueString();
                if (familyName == "VEKS bærering modplade")
                {
                    Element elType = doc.GetElement(fi.GetTypeId());
                    bool TopBool = elType.LookupParameter("Modpl_Top_Vis").AsInteger() == 1;
                    bool BottomBool = elType.LookupParameter("Modpl_Bottom_Vis").AsInteger() == 1;
                    bool LeftBool = elType.LookupParameter("Modpl_Left_Vis").AsInteger() == 1;
                    bool RightBool = elType.LookupParameter("Modpl_Right_Vis").AsInteger() == 1;

                    Transform trf = fi.GetTransform();
                    XYZ Origin = new XYZ();
                    Origin = trf.OfPoint(Origin);

                    #region Intersection preparation
                    //Prepare common objects for intersection analysis
                    //Create a filter that filters for structural columns and framing
                    //As I want to select by category, I need them to be FamilyInstances
                    IList<ElementFilter> filterList = new List<ElementFilter>
                                        { new ElementCategoryFilter(BuiltInCategory.OST_StructuralFraming),
                                          new ElementCategoryFilter(BuiltInCategory.OST_StructuralColumns) };
                    LogicalOrFilter bicFilter = new LogicalOrFilter(filterList);

                    LogicalAndFilter fiAndBicFilter = new LogicalAndFilter(bicFilter, new ElementClassFilter(typeof(FamilyInstance)));

                    //Get the default 3D view
                    var view3D = Shared.Filter.Get3DView(doc);
                    if (view3D == null) throw new Exception("No default 3D view named {3D} is found!.");

                    var refIntersect = new ReferenceIntersector(fiAndBicFilter, FindReferenceTarget.Element, view3D);
                    #endregion

                    if (TopBool)
                    {
                        //case "Top":
                        XYZ Top = trf.BasisZ;

                        DetectAndCreateInternalSupport(ASE_OriginalList, fi, Origin, refIntersect, ("Top", Top));
                    }

                    if (BottomBool)
                    {
                        //case "Bottom":
                        XYZ Bottom = trf.BasisZ * -1;

                        DetectAndCreateInternalSupport(ASE_OriginalList, fi, Origin, refIntersect, ("Bottom", Bottom));
                    }

                    if (LeftBool)
                    {
                        //case "Left":
                        XYZ Left = trf.BasisY;

                        DetectAndCreateInternalSupport(ASE_OriginalList, fi, Origin, refIntersect, ("Left", Left));
                    }

                    if (RightBool)
                    {
                        //case "Right":
                        XYZ Right = trf.BasisY * -1;

                        DetectAndCreateInternalSupport(ASE_OriginalList, fi, Origin, refIntersect, ("Right", Right));
                    }
                }
                else { }//Implement other possibilities later
            }
            #endregion

            //Analyze steel structure finding all intersections and end points
            //And create analytical stick model with nodes at each intersection and end point
            var result = ASE_OriginalList.SelectMany
                (
                    (fst, i) => ASE_OriginalList.Skip(i + 1).Select(snd => (fst, snd))
                );

            //Find all intersections in the structure system
            List<XYZ> AllIntersectionPoints = new List<XYZ>();
            foreach (var comb in result) FindIntersections(comb.fst, comb.snd, AllIntersectionPoints);

            //Clean intersection points collection for duplicates
            AllIntersectionPoints = AllIntersectionPoints.Distinct(new XyzComparer()).ToList();

            //Create partial elements by finding intersection points on element.
            foreach (var ase in ASE_OriginalList.Where(x => !x.IsInternalSupport)) CreatePartialElements(ase, ASE_NewElementsList, AllIntersectionPoints);

            //Write steel data
            foreach (AnalyticalSteelElement ase in ASE_NewElementsList.Where(x => !x.IsInternalSupport))
            {
                sb.Append("PROF");
                sb.Append(dw.PointCoords("P1", ase.P1));
                sb.Append(dw.PointCoords("P2", ase.P2));
                //Hardcoded material until further notice
                sb.Append(" MAT=S235JR");
                sb.Append(dw.ParameterValue("TYP", BuiltInParameter.ELEM_TYPE_PARAM, ase.Host));
                sb.Append(" ACHSE=Y");
                sb.Append(" RI='0,1,0'");
                sb.Append(" LAST=STEEL");
                sb.AppendLine();
            }

            foreach (AnalyticalSteelElement ase in ASE_OriginalList.Where(x => x.IsInternalSupport))
            {
                sb.Append("INT_SUP");
                sb.Append(dw.PointCoords("PNAME", ase.P1));
                sb.Append(dw.PointCoords("BASE", ase.P2));
                sb.AppendLine();
            }

            #region Debug
            //using (Transaction tx2 = new Transaction(doc))
            //{
            //    tx2.Start("Debug placement of profiles.");
            //    foreach (var ase in ASE_NewElementsList)
            //    {
            //        Shared.Dbg.PlaceAdaptiveFamilyInstance(doc, "Marker Line: Red", ase.P1, ase.P2);

            //    }
            //    tx2.Commit();
            //}
            #endregion
            return sb;
        }

        private void DetectAndCreateInternalSupport(List<AnalyticalSteelElement> list,
            FamilyInstance fi, XYZ Origin, ReferenceIntersector refIntersect, (string DirName, XYZ Dir) Direction)
        {
            //Find the first structural framing element in that direction
            ReferenceWithContext rwc = refIntersect.FindNearest(Origin, Direction.Dir);
            if (rwc == null) throw new Exception($"Direction {Direction.DirName} for frame support {fi.Id} did not find any steel members! Check if elements are properly aligned.");
            var refId = rwc.GetReference()?.ElementId;

            Element foundElement = doc.GetElement(refId);
            var ams = foundElement.GetAnalyticalModel();
            var ir = ams.GetCurve().Project(Origin);

            //Dbg.PlaceAdaptiveFamilyInstance(doc, "Marker Line: Red", Origin, Origin + Direction.Dir * 5);
            list.Add(new AnalyticalSteelElement(Origin, ir.XYZPoint, true));
        }

        private void CreatePartialElements(AnalyticalSteelElement ASE, List<AnalyticalSteelElement> NewElements, List<XYZ> intersections)
        {
            var PointsOnLine = intersections.Where(x => FindPointsOnLine(x, ASE)).ToList();
            //Add elements' own points to collection of intersections
            //Also ensures something is in the collection if there are no intersections
            PointsOnLine.Add(ASE.P1);
            PointsOnLine.Add(ASE.P2);
            //Sort points geometrically to be able to create new elements sequentially
            var SortedPoints = PointsOnLine
                .OrderBy(p => p.X.Round(6)).ThenBy(p => p.Y.Round(6)).ThenBy(p => p.Z.Round(6))
                .ToList();
            //Create new elements sequentially
            for (int i = 0; i < SortedPoints.Count - 1; i++)
            {
                NewElements.Add(new AnalyticalSteelElement(SortedPoints[i], SortedPoints[i + 1], ASE.Host));
            }
        }

        private bool FindPointsOnLine(XYZ xyz, AnalyticalSteelElement ASE)
        {
            //Make sure no intersections coincident with end points pass the filter
            //Points at ends will be added 
            if (ASE.P1.Equalz(xyz, 1e-6)) return false;
            else if (ASE.P2.Equalz(xyz, 1e-6)) return false;
            return Math.Abs((ASE.P1.DistanceTo(xyz) + ASE.P2.DistanceTo(xyz)) - ASE.Curve.Length) < 1e-6;
        }

        private void FindIntersections(AnalyticalSteelElement fst, AnalyticalSteelElement snd, List<XYZ> list)
        {
            fst.Curve.Intersect(snd.Curve, out IntersectionResultArray ira);
            if (ira != null) foreach (IntersectionResult intersection in ira) list.Add(intersection.XYZPoint);
        }

        internal StringBuilder ExportBoundaryConditions()
        {
            StringBuilder sb = new StringBuilder();

            var AllBoundaryConditions = Shared.Filter
                .GetElements<BoundaryConditions, BuiltInCategory>(doc, BuiltInCategory.INVALID);

            //Hardcoded pinned support for now
            foreach (BoundaryConditions bc in AllBoundaryConditions)
            {
                sb.Append("FLAX");
                sb.Append(dw.PointCoords("PNAME", bc.Point));
                sb.AppendLine();
            }

            return sb;
        }
    }

    internal class AnalyticalSteelElement
    {
        public XYZ P1;
        public XYZ P2;
        public Curve Curve;
        public Element Host;
        public bool IsInternalSupport = false;

        public AnalyticalSteelElement(XYZ p1, XYZ p2, Element host)
        {
            P1 = p1; P2 = p2; Host = host;
        }

        public AnalyticalSteelElement(XYZ p1, XYZ p2, bool isInternalSupport = true)
        {
            P1 = p1; P2 = p2; IsInternalSupport = isInternalSupport; Curve = Line.CreateBound(p1, p2);
        }

        public AnalyticalSteelElement(Document doc, AnalyticalModelStick ams)
        {
            Curve = ams.GetCurve();
            P1 = Curve.GetEndPoint(0);
            P2 = Curve.GetEndPoint(1);
            Host = doc.GetElement(ams.GetElementId());
        }
    }
}
