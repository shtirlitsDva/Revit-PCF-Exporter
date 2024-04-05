using System;
using System.Collections.Generic;
using System.Linq;
using MoreLinq;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Plumbing;
using Autodesk.Revit.DB.Structure;
using Shared;
using fi = Shared.Filter;
using tr = Shared.Transformation;
using mp = Shared.MepUtils;
using lad = MEPUtils.CreateInstrumentation.ListsAndDicts;
using MEPUtils.SharedStaging;
using dbg = Shared.Dbg;

namespace MEPUtils.MoveToDistance
{
    public class MoveToDistance
    {
        public static Result Move(UIApplication uiApp)
        {
            Document doc = uiApp.ActiveUIDocument.Document;
            UIDocument uidoc = uiApp.ActiveUIDocument;

            try
            {
                using (TransactionGroup txGp = new TransactionGroup(doc))
                {
                    txGp.Start("Move elements to distance!");

                    Selection selection = uidoc.Selection;
                    var selIds = selection.GetElementIds();
                    if (selIds.Count == 0) throw new Exception("Empty selection: must select element(s) to move first!");

                    HashSet<Element> elsToMove = selIds.Select(x => doc.GetElement(x)).ToHashSet();

                    var elId = uidoc.Selection.PickObject(ObjectType.Element, "Select element to move to!");
                    Element MoveToEl = doc.GetElement(elId);
                    double distanceToKeep;

                    //Ask for a length input
                    InputBoxBasic ds = new InputBoxBasic();
                    ds.ShowDialog();
                    distanceToKeep = double.Parse(ds.DistanceToKeep).MmToFt();

                    //Business logic to move but keep desired distance
                    HashSet<Connector> toMoveCons = SpecialGetAllConnectors(elsToMove);

                    HashSet<Connector> moveToCons = SpecialGetAllConnectors(new HashSet<Element> { MoveToEl });

                    var listToCompare = new List<(Connector toMoveCon, Connector MoveToCon, double Distance)>();

                    foreach (Connector c1 in toMoveCons) foreach (Connector c2 in moveToCons) listToCompare.Add((c1, c2, c1.Origin.DistanceTo(c2.Origin)));

                    var (toMoveCon, MoveToCon, Distance) = listToCompare.MinBy(x => x.Distance);

                    XYZ moveVector = (MoveToCon.Origin - toMoveCon.Origin) * (1 - distanceToKeep / Distance);

                    using (Transaction trans3 = new Transaction(doc))
                    {
                        trans3.Start("Move Element!");
                        {
                            if (elsToMove.Count > 1) 
                            {
                                ElementTransformUtils.MoveElements(doc, selIds, moveVector);
                            }
                            else
                            {
                                foreach (Element elToMove in elsToMove)
                                {
                                    ElementTransformUtils.MoveElement(doc, elToMove.Id, moveVector);
                                }
                            }
                        }
                        trans3.Commit();
                    }

                    txGp.Assimilate();
                }

                return Result.Succeeded;
            }

            catch (Autodesk.Revit.Exceptions.OperationCanceledException) { return Result.Cancelled; }

            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        /// <summary>
        /// This method treats SpudAdjustables (Olets) as special apart from other elements.
        /// </summary>
        private static HashSet<Connector> SpecialGetAllConnectors(HashSet<Element> elements)
        {
            HashSet<Connector> col = new HashSet<Connector>();

            elements = elements.Where(x => (x.IsType<Pipe>() ||
            (x.IsType<FamilyInstance>() &&
            (x.Category.Id.IntegerValue == (int)BuiltInCategory.OST_PipeFitting ||
             x.Category.Id.IntegerValue == (int)BuiltInCategory.OST_PipeAccessory)))).ToHashSet();

            foreach (var e in elements)
            {

                if (e.MechFittingPartType() == PartType.SpudAdjustable)
                {
                    Cons cons = mp.GetConnectors(e);
                    col.UnionWith(mp.GetAllConnectorsFromConnectorSet(cons.Primary.AllRefs));
                    col.Add(cons.Secondary);
                }
                else
                {
                    var cons = mp.GetALLConnectorsFromElements(e);
                    col.UnionWith(cons);
                }
            }
            return col;
        }

        private static Element SelectElement(Document doc, UIDocument uidoc, string msg)
        {
            //Select the pipe to operate on
            return Shared.BuildingCoder.BuildingCoderUtilities.SelectSingleElementOfType(uidoc, typeof(Element), msg, true);
        }
    }
}
