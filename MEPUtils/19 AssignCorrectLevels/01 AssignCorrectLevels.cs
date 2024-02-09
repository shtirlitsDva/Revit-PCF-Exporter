using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System;
using System.Data;
using System.IO;
using System.Linq;
//using MoreLinq;
using System.Windows.Input;
using System.Collections.Generic;
using MEPUtils.SharedStaging;
using static Shared.Filter;
using static Shared.Extensions;

namespace MEPUtils.AssignCorrectLevels
{
    [Autodesk.Revit.Attributes.Transaction(Autodesk.Revit.Attributes.TransactionMode.Manual)]
    public class AssignCorrectLevels : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            return AssignCorrectLevel(commandData);
        }

        internal Result AssignCorrectLevel(ExternalCommandData cData)
        {
            Document doc = cData.Application.ActiveUIDocument.Document;
            HashSet<Element> elements = GetElementsWithConnectors(doc).ToHashSet();

            List<Level> levels = GetElements<Level, BuiltInCategory>(doc, BuiltInCategory.OST_Levels).ToList();


            using (Transaction tx = new Transaction(doc))
            {
                tx.Start("Assign Correct Levels!");
                foreach (Element e in elements)
                {
                    List<(Element curEl, Level lvl, double dist)> tuples = new List<(Element curEl, Level lvl, double dist)>(levels.Count);
                    foreach (Level level in levels)
                    {

                    }
                }
                tx.Commit();
            }
            return Result.Succeeded;
        }
    }
}
