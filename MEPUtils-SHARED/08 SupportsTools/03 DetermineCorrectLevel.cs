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
    public class DetermineCorrectLevel
    {
        public static void Execute(UIApplication uiApp)
        {
            Document doc = uiApp.ActiveUIDocument.Document;
            UIDocument uidoc = uiApp.ActiveUIDocument;

            try
            {
                using (TransactionGroup txGp = new TransactionGroup(doc))
                {
                    txGp.Start("Determine correct level.");

                    //Collect elements
                    var supports = fi.GetElements<FamilyInstance, Guid>
                        (doc, new Guid("a7f72797-135b-4a1c-8969-e2e3fc76ff14"), "Pipe Support");
                    //Is the following required?
                    HashSet<Element> allSupports = new HashSet<Element>(supports.Cast<Element>()
                    .Where(x => x.Category.Id.IntegerValue == (int)BuiltInCategory.OST_PipeAccessory));

                    //Prepare all levels
                    HashSet<Level> levels = fi.GetElements<Level, BuiltInCategory>(doc, BuiltInCategory.OST_Levels);

                    using (Transaction trans1 = new Transaction(doc))
                    {
                        trans1.Start("Find the level.");

                        foreach (Element support in allSupports)
                        {
                            //Determine levels
                            List<(Level lvl, double dist)> levelsWithDist = new List<(Level lvl, double dist)>();

                            foreach (Level level in levels)
                            {
                                (Level, double) result = (level, ((LocationPoint)support.Location).Point.Z - level.Elevation);
                                if (result.Item2 > -1e-6) levelsWithDist.Add(result);
                            }

                            var minimumLevel = levelsWithDist.MinBy(x => x.dist).FirstOrDefault();
                            if (minimumLevel.Equals(default))
                            {
                                throw new Exception($"Element {support.Id.ToString()} is below all levels!");
                            }

                            support.get_Parameter(BuiltInParameter.FAMILY_LEVEL_PARAM).Set(minimumLevel.lvl.Id);
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
