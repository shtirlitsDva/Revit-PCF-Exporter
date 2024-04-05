using System;
using System.Collections.Generic;
using System.Linq;
using MoreLinq;
using Autodesk.Revit.UI;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Plumbing;
using Autodesk.Revit.DB.Structure;
using Shared;
using fi = Shared.Filter;
using op = Shared.Output;
using tr = Shared.Transformation;
using mp = Shared.MepUtils;

namespace MEPUtils.SupportTools
{
    public class CalculateHeightBySteelSupport
    {
        public static void Calculate(UIApplication uiApp)
        {
            Document doc = uiApp.ActiveUIDocument.Document;
            UIDocument uidoc = uiApp.ActiveUIDocument;

            try
            {
                using (TransactionGroup txGp = new TransactionGroup(doc))
                {
                    txGp.Start("Calculate height of hangers");

                    //Collect elements
                    var hangerSupports = fi.GetElements<FamilyInstance, Guid>
                        (doc, new Guid("e0baa750-22ba-4e60-9466-803137a0cba8"), "Hænger");
                    //Is the following required?
                    HashSet<Element> allHangers = new HashSet<Element>(hangerSupports.Cast<Element>()
                    .Where(x => x.Category.Id.IntegerValue == (int)BuiltInCategory.OST_PipeAccessory));

                    //Prepare common objects for intersection analysis
                    //Create a filter that filters for structural columns and framing
                    //Why columns? Maybe only framing is enough.

                    //Linked IFC files create DirectShapes!
                    ElementClassFilter filter = new ElementClassFilter(typeof(DirectShape));

                    //IList<ElementFilter> filterList = new List<ElementFilter>
                    //                    { new ElementCategoryFilter(BuiltInCategory.OST_StructuralFraming),
                    //                      new ElementCategoryFilter(BuiltInCategory.OST_StructuralColumns) };
                    //LogicalOrFilter bicFilter = new LogicalOrFilter(filterList);
                    //LogicalAndFilter fiAndBicFilter = new LogicalAndFilter(bicFilter, new ElementClassFilter(typeof(FamilyInstance)));

                    //Get the default 3D view
                    var view3D = Shared.Filter.Get3DView(doc);
                    if (view3D == null) throw new Exception("No default 3D view named {3D} is found!.");
                    
                    //Instantiate the Reference Intersector
                    var refIntersect = new ReferenceIntersector(filter, FindReferenceTarget.Face, view3D);
                    refIntersect.FindReferencesInRevitLinks = true;

                    using (Transaction trans1 = new Transaction(doc))
                    {
                        trans1.Start("Calculate height to nearest framing");

                        foreach (Element hanger in allHangers)
                        {
                            //Find the point of the framing above the hanger
                            Transform trf = ((FamilyInstance)hanger).GetTransform();
                            XYZ Origin = new XYZ();
                            Origin = trf.OfPoint(Origin);
                            XYZ Direction = trf.BasisZ;
                            //XYZ Origin = ((LocationPoint)hanger.Location).Point;

                            ReferenceWithContext rwc = refIntersect.FindNearest(Origin, Direction);
                            if (rwc == null) continue;
                                             //throw new Exception($"Hanger {hanger.Id} did not find any steel supports!\n" +
                                             //                    $"Check if elements are properly aligned.");
                            Reference reference = rwc.GetReference();
                            XYZ intersection = reference.GlobalPoint;

                            //Get the hanger's height above it's reference level
                            Parameter offsetPar = hanger.get_Parameter(BuiltInParameter.INSTANCE_FREE_HOST_OFFSET_PARAM);
                            double offsetFromLvl = offsetPar.AsDouble();
                            //Just to make sure that the proper parameter is set
                            hanger.LookupParameter("PipeOffsetFromLevel").Set(offsetFromLvl);

                            //Calculate the height of the intersection above the reference level
                            ElementId refLvlId = hanger.LevelId;
                            Level refLvl = (Level)doc.GetElement(refLvlId);
                            double refLvlElevation = refLvl.Elevation;
                            double profileHeight = intersection.Z - refLvlElevation;

                            //Set the hanger value so it updates
                            hanger.LookupParameter("LevelHeight").Set(profileHeight);
                        }
                        trans1.Commit();
                    }
                    txGp.Assimilate();
                }
            }

            catch (Exception ex)
            {
                throw new Exception(ex.Message);
                //return Result.Failed;
            }
        }

    }
}
