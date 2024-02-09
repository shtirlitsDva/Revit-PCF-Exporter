using System;
using System.Collections.Generic;
using System.Linq;
using MoreLinq;
using Autodesk.Revit.UI;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Plumbing;
using Autodesk.Revit.DB.Structure.StructuralSections;
using Shared;
using fi = Shared.Filter;
using op = Shared.Output;
using tr = Shared.Transformation;
using mp = Shared.MepUtils;

namespace MEPUtils.SupportTools
{
    public class CalculateHeightBySteelOrLevel
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
                    //ElementClassFilter filter = new ElementClassFilter(typeof(DirectShape));

                    IList<ElementFilter> filterList = new List<ElementFilter>
                                        { new ElementCategoryFilter(BuiltInCategory.OST_StructuralFraming),
                                          new ElementCategoryFilter(BuiltInCategory.OST_StructuralColumns),
                                          new ElementCategoryFilter(BuiltInCategory.OST_Floors)
                    };
                    LogicalOrFilter bicFilter = new LogicalOrFilter(filterList);

                    IList<ElementFilter> filterList2 = new List<ElementFilter>
                    {
                        new ElementClassFilter( typeof(FamilyInstance)),
                        new ElementClassFilter( typeof(Floor))
                    };
                    LogicalOrFilter classFilter = new LogicalOrFilter(filterList2);

                    LogicalAndFilter fiAndBicFilter = new LogicalAndFilter(bicFilter, classFilter);
                    //new ElementClassFilter(typeof(FamilyInstance)));

                    //Get the default 3D view
                    var view3D = Shared.Filter.Get3DView(doc);
                    if (view3D == null) throw new Exception("No default 3D view named {3D} is found!.");

                    //Instantiate the Reference Intersector
                    var refIntersect = new ReferenceIntersector(fiAndBicFilter, FindReferenceTarget.Face, view3D);
                    refIntersect.FindReferencesInRevitLinks = true;

                    //Prepare to find levels
                    HashSet<Level> levels = fi.GetElements<Level, BuiltInCategory>(doc, BuiltInCategory.OST_Levels);

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
                            if (rwc != null)
                            {
                                //throw new Exception($"Hanger {hanger.Id} did not find any steel supports!\n" +
                                //                    $"Check if elements are properly aligned.");
                                Reference reference = rwc.GetReference();
                                XYZ intersection = reference.GlobalPoint;

                                //Set the support to internal support
                                //"dba1aec8-daa6-46c0-a4d0-db8d50155dcb" <-- NTR_ELEM_INTR_SUP
                                Element intersectedElement = doc.GetElement(reference);
                                if (intersectedElement.Category.Id.IntegerValue == (int)BuiltInCategory.OST_StructuralFraming ||
                                    intersectedElement.Category.Id.IntegerValue == (int)BuiltInCategory.OST_StructuralColumns)
                                {
                                    Parameter isInternalSupportParameter = hanger.get_Parameter(
                                        new Guid("dba1aec8-daa6-46c0-a4d0-db8d50155dcb"));
                                                 
                                    if (isInternalSupportParameter == null) throw new
                                            Exception("NTR_ELEM_INTR_SUP is not imported into project!");
                                    isInternalSupportParameter.Set(1);
                                }

                                //Get the hanger's height above it's reference level
                                Parameter offsetPar = hanger.get_Parameter(BuiltInParameter.INSTANCE_FREE_HOST_OFFSET_PARAM);
                                double offsetFromLvl = offsetPar.AsDouble();
                                //Just to make sure that the proper parameter is set
                                hanger.LookupParameter("PipeOffsetFromLevel").Set(offsetFromLvl);

                                //Calculate the height of the intersection above the reference level
                                ElementId refLvlId = hanger.LevelId;
                                Level refLvl = (Level)doc.GetElement(refLvlId);
                                double refLvlElevation = refLvl.ProjectElevation;
                                double profileHeight = intersection.Z - refLvlElevation;

                                //Set the hanger value so it updates
                                hanger.LookupParameter("LevelHeight").Set(profileHeight);

                                //Set the values for the BeamClamps
                                if (hanger.FamilyName() == "Spring Hanger - Witzenmann-Hydra - Type 11" ||
                                    hanger.FamilyName() == "Rigid Hanger - Hydra - Type 11" ||
                                    hanger.FamilyName() == "Spring Hanger - Witzenmann-Hydra - Type 12")
                                {
                                    //Instantiate the Reference Intersector
                                    var refIntersectElement = new ReferenceIntersector(fiAndBicFilter, FindReferenceTarget.Element, view3D);
                                    refIntersectElement.FindReferencesInRevitLinks = true;
                                    ReferenceWithContext rwcElement = refIntersect.FindNearest(Origin, Direction);
                                    if (rwcElement != null)
                                    {
                                        Reference referenceElement = rwcElement.GetReference();
                                        Element element = doc.GetElement(reference);

                                        if (element.Category.Id.IntegerValue == (int)BuiltInCategory.OST_StructuralFraming)
                                        {
                                            FamilyInstance fi = element as FamilyInstance;

                                            //StructuralSection ss = fi.Symbol.GetStructuralSection();
                                            ////StructuralSectionUtils.GetStructuralSection(doc, element.Id);
                                            //StructuralSectionShape sss = ss.StructuralSectionShape;

                                            FamilySymbol fs = fi.Symbol;
                                            double flangeWidth = fs.LookupParameter("bf").AsDouble();
                                            double flangeThickness = fs.LookupParameter("tf").AsDouble();

                                            hanger.LookupParameter("ProfilKlammeFlangeBredde")?.Set(flangeWidth);
                                            hanger.LookupParameter("ProfilKlammeFlangeTykkelse")?.Set(flangeThickness);
                                        }
                                    }
                                }
                            }
                            else
                            {
                                //Determine levels
                                List<(Level lvl, double dist)> levelsWithDist = new List<(Level lvl, double dist)>(levels.Count);

                                //Normalize all level elevations based on the lowest
                                var query = levels.MinBy(x => x.ProjectElevation);
                                double lowestElevation = query.ProjectElevation;

                                List<(Level lvl, double normalizedElevation)> levelsNormalized = 
                                    new List<(Level lvl, double normalizedElevation)>();

                                foreach (Level l in levels)
                                {
                                    levelsNormalized.Add((l, l.ProjectElevation - lowestElevation));
                                }

                                double normalizedP = ((LocationPoint)hanger.Location).Point.Z - lowestElevation;

                                foreach (var nl in levelsNormalized)
                                {
                                    (Level, double) result = (
                                        nl.lvl, 
                                        nl.normalizedElevation - normalizedP);
                                    levelsWithDist.Add(result);
                                }

                                var sorted = levelsWithDist.OrderBy(x => x.dist).ToList();

                                (Level lvl, double dist) minimumLvl = default;
                                foreach (var item in sorted)
                                {
                                    if (item.dist > 0.4)
                                    {
                                        minimumLvl = item;
                                        break;
                                    }
                                }
                                if (minimumLvl == default) continue;

                                ElementId refLvlId = hanger.LevelId;
                                Level refLvl = (Level)doc.GetElement(refLvlId);
                                double refLvlElevation = refLvl.Elevation;
                                double lvlHeight = minimumLvl.lvl.Elevation - refLvlElevation;
                                hanger.LookupParameter("LevelHeight").Set(lvlHeight);

                                Parameter offsetPar = hanger.get_Parameter(BuiltInParameter.INSTANCE_FREE_HOST_OFFSET_PARAM);
                                double offsetFromLvl = offsetPar.AsDouble();
                                hanger.LookupParameter("PipeOffsetFromLevel").Set(offsetFromLvl);
                            }
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
