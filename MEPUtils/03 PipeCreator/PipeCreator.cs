using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Plumbing;
using Autodesk.Revit.DB.Structure;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using MoreLinq;
using Shared;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Windows.Input;
using System.Windows.Forms;
using fi = Shared.Filter;
using lad = MEPUtils.CreateInstrumentation.ListsAndDicts;
using mp = Shared.MepUtils;
using op = Shared.Output;
using tr = Shared.Transformation;

namespace MEPUtils
{
    public static class PipeCreator
    {
        public static Result CreatePipeFromConnector(UIApplication uiApp)
        {
            Document doc = uiApp.ActiveUIDocument.Document;

            bool ctrl = false;
            if ((int)Keyboard.Modifiers == 2) ctrl = true;

            string pipeTypeName = MEPUtils.Properties.Settings.Default.PipeCreator_SelectedPipeTypeName;

            //If the name of pipeType is null or empty for some reason -- reinitialize
            if (string.IsNullOrEmpty(pipeTypeName)) ctrl = true;

            if (ctrl)
            {
                FilteredElementCollector colPipeTypes = new FilteredElementCollector(doc);
                var pipeTypes = colPipeTypes.OfClass(typeof(PipeType)).ToElements();

                var pipeTypeNames = colPipeTypes.Select(x => x.Name).ToList();

                int count = pipeTypeNames.Count;

                var pc = new PipeTypeSelector(uiApp, pipeTypeNames);
                pc.ShowDialog();

                pipeTypeName = pc.pipeTypeName;
            }

            try
            {
                //One element selected, creates pipe at random connector
                //Or an elbow for pipe
                //Two elements selected: Creates pipe between them
                //NOTE: the connectors must be aligned
                Selection selection = uiApp.ActiveUIDocument.Selection;
                ICollection<ElementId> elIds = selection.GetElementIds();
                if (elIds.Count == 2)
                {
                    Element firstEl = doc.GetElement(elIds.First());
                    Element secondEl = doc.GetElement(elIds.Last());

                    HashSet<Connector> firstCons = mp.GetALLConnectorsFromElements(firstEl);
                    HashSet<Connector> secondCons = mp.GetALLConnectorsFromElements(secondEl);

                    var listToCompare = new List<(Connector firstCon, Connector secondCon, double Distance)>();

                    foreach (Connector c1 in firstCons) foreach (Connector c2 in secondCons)
                            listToCompare.Add((c1, c2, c1.Origin.DistanceTo(c2.Origin)));

                    var (firstCon, secondCon, Distance) = listToCompare.MinBy(x => x.Distance);

                    using (Transaction tx = new Transaction(doc))
                    {
                        //Get the typeId of the selected or read PipeType
                        var filter = fi.ParameterValueGenericFilter(doc, pipeTypeName, BuiltInParameter.ALL_MODEL_TYPE_NAME);
                        FilteredElementCollector col = new FilteredElementCollector(doc);
                        var pipeType = col.OfClass(typeof(PipeType)).WherePasses(filter).ToElements().FirstOrDefault();
                        if (pipeType == null) throw new Exception("Collection of PipeType failed!");

                        //LevelId can be null -> work around
                        ElementId levelId;

                        if (firstEl.LevelId.IntegerValue == -1)
                        {
                            FilteredElementCollector lcol = new FilteredElementCollector(doc);
                            var randomLvl = lcol.OfClass(typeof(Level)).ToElements().LastOrDefault(); //Select random levelid
                            levelId = randomLvl.Id;
                        }
                        else levelId = firstEl.LevelId;

                        tx.Start("Create pipe!");
                        Pipe.Create(doc, pipeType.Id, levelId, firstCon, secondCon);
                        tx.Commit();
                    }
                }
                else
                {
                    //if (elIds.Count == 0 || elIds.Count > 1) throw new Exception("Only works on single element! No or multiple elements selected!");
                    ElementId id = elIds.FirstOrDefault();
                    if (id == null) throw new Exception("Getting element from selection failed!");
                    Element element = doc.GetElement(id);
                    var cons = mp.GetALLConnectorsFromElements(element);

                    //Get the typeId of the selected or read PipeType
                    var filter = fi.ParameterValueGenericFilter(doc, pipeTypeName, BuiltInParameter.ALL_MODEL_TYPE_NAME);
                    FilteredElementCollector col = new FilteredElementCollector(doc);
                    var pipeType = col.OfClass(typeof(PipeType)).WherePasses(filter).ToElements().FirstOrDefault();
                    if (pipeType == null) throw new Exception("Collection of PipeType failed!");

                    //LevelId can be null -> work around
                    ElementId levelId;

                    if (element.LevelId.IntegerValue == -1)
                    {
                        FilteredElementCollector lcol = new FilteredElementCollector(doc);
                        var randomLvl = lcol.OfClass(typeof(Level)).ToElements().LastOrDefault(); //Select random levelid
                        levelId = randomLvl.Id;
                    }
                    else levelId = element.LevelId;

                    Connector con = (from Connector c in cons where c.IsConnected == false select c).FirstOrDefault();
                    if (con == null) throw new Exception("No not connected connectors in element!");

                    //If the element is a Pipe -> Create a bend in a specified direction
                    if (element is Pipe selectedPipe)
                    {
                        string bendDir;

                        //Get Pipe Size
                        double pipeSize = selectedPipe.Diameter;

                        //Select the direction to create in
                        BaseFormTableLayoutPanel_Basic ds = new BaseFormTableLayoutPanel_Basic(
                            System.Windows.Forms.Cursor.Position.X, System.Windows.Forms.Cursor.Position.Y, lad.Directions());
                        ds.ShowDialog();
                        bendDir = ds.strTR;

                        ElementId curPipingSysTypeId = selectedPipe.MEPSystem.GetTypeId();
                        ElementId curPipeTypeId = selectedPipe.PipeType.Id;

                        XYZ iP = con.Origin;
                        XYZ dirPoint = null;
                        //Create direction point
                        switch (bendDir)
                        {
                            case "Top":
                                dirPoint = new XYZ(iP.X, iP.Y, iP.Z + 5);
                                break;
                            case "Bottom":
                                dirPoint = new XYZ(iP.X, iP.Y, iP.Z - 5);
                                break;
                            case "Front":
                                dirPoint = new XYZ(iP.X, iP.Y - 5, iP.Z);
                                break;
                            case "Back":
                                dirPoint = new XYZ(iP.X, iP.Y + 5, iP.Z);
                                break;
                            case "Left":
                                dirPoint = new XYZ(iP.X - 5, iP.Y, iP.Z);
                                break;
                            case "Right":
                                dirPoint = new XYZ(iP.X + 5, iP.Y, iP.Z);
                                break;
                            default:
                                break;
                        }

                        using (TransactionGroup txGp = new TransactionGroup(doc))
                        {
                            txGp.Start("Create new elbow and pipe!");

                            Pipe newPipe;

                            using (Transaction tx1 = new Transaction(doc))
                            {
                                tx1.Start("Create new pipe!");

                                newPipe = Pipe.Create(doc, curPipingSysTypeId, curPipeTypeId, levelId, iP, dirPoint);

                                //Change size of the pipe
                                Parameter par = newPipe.get_Parameter(BuiltInParameter.RBS_PIPE_DIAMETER_PARAM);
                                par.Set(pipeSize);

                                tx1.Commit();
                            }

                            //Find the connector from the dummy pipe at intersection
                            var newCons = mp.GetALLConnectorsFromElements(newPipe);
                            Connector newCon = newCons.Where(c => c.Equalz(con, Extensions._1mmTol)).FirstOrDefault();

                            using (Transaction tx2 = new Transaction(doc))
                            {
                                tx2.Start("Create new bend!");

                                doc.Create.NewElbowFitting(con, newCon);

                                tx2.Commit();
                            }

                            txGp.Assimilate();
                        }
                    }
                    else //if element is anything other than a Pipe -> Create a pipe from random connector
                    {
                        //Create a point in space to connect the pipe
                        XYZ direction = con.CoordinateSystem.BasisZ.Multiply(2);
                        XYZ origin = con.Origin;
                        XYZ pointInSpace = origin.Add(direction);

                        //Transaction that creates the pipe
                        Transaction tx = new Transaction(doc);
                        tx.Start("Create pipe!");
                        //Create the pipe
                        Pipe.Create(doc, pipeType.Id, levelId, con, pointInSpace);
                        tx.Commit();
                    }
                }
            }
            catch (Exception e)
            {
                throw new Exception(e.Message);
            }

            return Result.Succeeded;
        }

        public static Result CreatePipeFromTwoPoints(UIApplication uiApp, List<XYZ> endPoints)
        {
            if (endPoints.Count != 2) throw new Exception("Only works with two points!");

            Document doc = uiApp.ActiveUIDocument.Document;

            string pipeTypeName = MEPUtils.Properties.Settings.Default.PipeCreator_SelectedPipeTypeName;

            bool ctrl = false;
            if ((int)Keyboard.Modifiers == 2) ctrl = true;
            //If the name of pipeType is null or empty for some reason -- reinitialize
            if (string.IsNullOrEmpty(pipeTypeName)) ctrl = true;

            if (ctrl)
            {
                FilteredElementCollector colPipeTypes = new FilteredElementCollector(doc);
                var pipeTypes = colPipeTypes.OfClass(typeof(PipeType)).ToElements();

                var pipeTypeNames = colPipeTypes.Select(x => x.Name).ToList();

                int count = pipeTypeNames.Count;

                var pc = new PipeTypeSelector(uiApp, pipeTypeNames);
                pc.ShowDialog();

                pipeTypeName = pc.pipeTypeName;
            }

            try
            {
                //One element selected, creates pipe at random connector
                //Or an elbow for pipe
                //Two elements selected: Creates pipe between them
                //NOTE: the connectors must be aligned
                using (Transaction tx = new Transaction(doc))
                {
                    //Get the typeId of the selected or read PipeType
                    var filter = fi.ParameterValueGenericFilter(doc, pipeTypeName, BuiltInParameter.ALL_MODEL_TYPE_NAME);
                    FilteredElementCollector col = new FilteredElementCollector(doc);
                    var pipeType = col.OfClass(typeof(PipeType)).WherePasses(filter).ToElements().FirstOrDefault();
                    if (pipeType == null) throw new Exception("Collection of PipeType failed!");

                    //LevelId can be null -> work around
                    ElementId levelId;
                    FilteredElementCollector lcol = new FilteredElementCollector(doc);
                    var randomLvl = lcol.OfClass(typeof(Level)).ToElements().LastOrDefault(); //Select random levelid
                    levelId = randomLvl.Id;

                    FilteredElementCollector scol = new FilteredElementCollector(doc);
                    var randomPipingSystemTypeId = scol.OfClass(
                        typeof(PipingSystemType)).ToElements().LastOrDefault(); //Select random pipingsystemtypeid
                    ElementId systemTypeId = randomPipingSystemTypeId.Id;

                    tx.Start("Create pipe!");
                    Pipe.Create(doc, systemTypeId, pipeType.Id, levelId, endPoints[0], endPoints[1]);
                    tx.Commit();
                }
            }
            catch (Exception e)
            {
                throw new Exception(e.Message);
            }

            return Result.Succeeded;
        }
    }
}
