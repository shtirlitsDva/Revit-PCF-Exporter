using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Plumbing;
using Autodesk.Revit.DB.Structure;
using Autodesk.Revit.UI;
using MoreLinq;
using Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using dbg = Shared.Dbg;
using fi = Shared.Filter;
using lad = MEPUtils.CreateInstrumentation.ListsAndDicts;
using mp = Shared.MepUtils;
using tr = Shared.Transformation;
using MEPUtils.SharedStaging;

namespace MEPUtils.CreateInstrumentation
{
    public class StartCreatingInstrumentation
    {
        public static Result StartCreating(UIApplication UiApp)
        {
            UIApplication uiApp = UiApp;
            Document doc = uiApp.ActiveUIDocument.Document;
            UIDocument uidoc = uiApp.ActiveUIDocument;

            try
            {
                using (TransactionGroup txGp = new TransactionGroup(doc))
                {
                    txGp.Start("Create Instrumentation!");

                    Pipe selectedPipe = null;
                    XYZ iP = null;
                    string operation = string.Empty;
                    string direction = string.Empty;
                    string PipeTypeName;
                    PipeType pipeType;
                    double size;
                    FamilyInstance olet;
                    ElementId curLvlId;
                    ElementId curPipingSysTypeId;
                    ElementId curPipeTypeId;

                    //Section to detect if an auto air vent selected and apply fittings to other end
                    ElementId eId = uidoc.Selection.GetElementIds().FirstOrDefault();
                    Element selectedElement = null;
                    Parameter namePar;
                    string famTyp = string.Empty;
                    if (eId != null)
                    {
                        selectedElement = doc.GetElement(eId);
                        namePar = selectedElement.get_Parameter(BuiltInParameter.ELEM_FAMILY_AND_TYPE_PARAM);
                        famTyp = namePar.AsValueString();
                        operation = "Add fittings to ML";
                    }

                    //If selection is not an Auto Air Vent, continue as normal
                    if (famTyp != "SpiroTop_AB050-R004: Standard")
                    {
                        //TODO: Implement a selection of: 1) Point on a pipe selected directly 2) Distance from element on the pipe
                        using (Transaction trans1 = new Transaction(doc))
                        {
                            trans1.Start("SelectPipePoint");
                            (selectedPipe, iP) = SelectPipePoint(doc, uidoc);
                            trans1.Commit();
                        }

                        //Select operation to perform
                        BaseFormTableLayoutPanel_Basic op = new BaseFormTableLayoutPanel_Basic(Cursor.Position.X, Cursor.Position.Y, lad.Operations());
                        op.ShowDialog();
                        operation = op.strTR;

                        //Select the direction to create in
                        BaseFormTableLayoutPanel_Basic ds = new BaseFormTableLayoutPanel_Basic(Cursor.Position.X, Cursor.Position.Y, lad.Directions());
                        ds.ShowDialog();
                        direction = ds.strTR;
                        //ut.InfoMsg(ds.strTR); 
                    }

                    switch (operation)
                    {
                        case "Auto ML (Udlufter)":
                            using (Transaction trans2 = new Transaction(doc))
                            {
                                trans2.Start("Auto ML");
                                Element dummyPipe;
                                (olet, dummyPipe) = CreateOlet(doc, iP, direction, selectedPipe, 15, "Stålrør, sømløse sockolet");
                                doc.Delete(dummyPipe.Id);
                                doc.Regenerate();

                                //"DN15-SM-EL: SM-EL"
                                Element cpValve = createNextElement(doc, olet, "DN15-SM-EL: SM-EL") ??
                                    throw new Exception("Creation of cpValve failed for some reason!");

                                Element union1 = createNextElement(doc, cpValve,
                                    "PIF_Cast Iron 281 hex nipple RH and LH thread ISO EN N8 R-L_GF: DN10 - DN50, Galvanised",
                                    "connection_diameter1", 15.0) ?? 
                                    throw new Exception("Creation of union1 failed for some reason!");
                                doc.Regenerate();

                                Element mlValve = createNextElement(doc, union1, "SpiroTop_AB050-R004: Standard") ??
                                    throw new Exception("Creation of mlValve failed for some reason!");

                                trans2.Commit();
                            }
                            break;
                        case "Add fittings to ML":
                            using (Transaction trans21 = new Transaction(doc))
                            {
                                trans21.Start("Finish Auto ML");

                                Element union2 = createNextElement(doc, selectedElement,
                                    "PIF_Cast Iron 330 union flat seat ISO-EN U1_GF: DN8 - DN100, Black",
                                    "connection_diameter1", 15.0, true) ??
                                    throw new Exception("Creation of union2 failed for some reason!");

                                Element adapter = createNextElement(doc, union2,
                                    "PIF_Mapress-StSt Adapter MT_Geberit: Var. DN",
                                    "connection_diameter1", 15.0) ??
                                    throw new Exception("Creation of adapter failed for some reason!");

                                trans21.Commit();
                            }
                            break;
                        case "PT (Tryktransmitter)":
                            using (Transaction trans3 = new Transaction(doc))
                            {
                                trans3.Start("PT");

                                Element dummyPipe;
                                (olet, dummyPipe) = CreateOlet(doc, iP, direction, selectedPipe, 15, "Stålrør, sømløse sockolet");
                                doc.Delete(dummyPipe.Id);
                                doc.Regenerate();

                                //"DN15-SM-EL: SM-EL"
                                Element cpValve = createNextElement(doc, olet, "DN15-SM-EL: SM-EL") ??
                                    throw new Exception("Creation of cpValve failed for some reason!");

                                Element instr = createNextElement(doc, cpValve, "Sitrans_P200: Standard") ??
                                    throw new Exception("Creation of instrument failed for some reason!");

                                trans3.Commit();
                            }
                            break;
                        case "PI (Manometer)":
                            using (Transaction trans4 = new Transaction(doc))
                            {
                                trans4.Start("Manometer");
                                Element dummyPipe;
                                (olet, dummyPipe) = CreateOlet(doc, iP, direction, selectedPipe, 15, "Stålrør, sømløse sockolet");
                                doc.Delete(dummyPipe.Id);
                                doc.Regenerate();

                                Element cpValve = createNextElement(doc, olet, "DN15-SM-EL: SM-EL") ??
                                    throw new Exception("Creation of cpValve failed for some reason!");

                                Element instr = createNextElement(doc, cpValve, "WIKA.Manometer.233.50.100: Standard") ??
                                    throw new Exception("Creation of instrument failed for some reason!");

                                trans4.Commit();
                            }
                            break;
                        case "TT (Temp. transmitter)":
                            using (Transaction trans5 = new Transaction(doc))
                            {
                                trans5.Start("Temperaturtransmitter");
                                Element dummyPipe;
                                (olet, dummyPipe) = CreateOlet(doc, iP, direction, selectedPipe, 15, "Stålrør, sømløse, termolomme");
                                doc.Delete(dummyPipe.Id);
                                doc.Regenerate();

                                Element cpValve = createNextElement(doc, olet, "WIKA.Termolomme.TW55-6: L200.U65.G1/2.9") ??
                                    throw new Exception("Creation of cpValve failed for some reason!");

                                //TODO: Places the instrument at wrong angle!!!!
                                Element instr = createNextElement(doc, cpValve, "Sitrans_TS500: Standard") ??
                                    throw new Exception("Creation of instrument failed for some reason!");

                                trans5.Commit();
                            }
                            break;
                        case "TI (Termometer)":
                            using (Transaction trans6 = new Transaction(doc))
                            {
                                trans6.Start("Termometer");
                                Element dummyPipe;
                                (olet, dummyPipe) = CreateOlet(doc, iP, direction, selectedPipe, 15, "Stålrør, sømløse, termolomme");
                                doc.Delete(dummyPipe.Id);
                                doc.Regenerate();

                                Element cpValve = createNextElement(doc, olet, "WIKA.Termolomme.TW55-6: L200.U65.G1/2.9") ??
                                    throw new Exception("Creation of cpValve failed for some reason!");

                                //TODO: Places the instrument at wrong angle!!!!
                                Element instr = createNextElement(doc, cpValve, "WIKA.Termometer.A52.100: Standard") ??
                                    throw new Exception("Creation of instrument failed for some reason!");

                                trans6.Commit();
                            }
                            break;
                        case "PS (Pressostat)":
                            using (Transaction trans7 = new Transaction(doc))
                            {
                                trans7.Start("Pressostat");
                                Element dummyPipe;
                                (olet, dummyPipe) = CreateOlet(doc, iP, direction, selectedPipe, 15, "Stålrør, sømløse sockolet");
                                doc.Delete(dummyPipe.Id);
                                doc.Regenerate();

                                Element cpValve = createNextElement(doc, olet, "DN15-SM-EL: SM-EL") ??
                                    throw new Exception("Creation of cpValve failed for some reason!");

                                Element instr = createNextElement(doc, cpValve, "Danfoss_pressostat_017-519166: Standard") ??
                                    throw new Exception("Creation of instrument failed for some reason!");

                                trans7.Commit();
                            }
                            break;
                        case "Pipe":
                            #region "Case PIPE"
                            //Select type of Olet
                            BaseFormTableLayoutPanel_Basic oletSelector = new BaseFormTableLayoutPanel_Basic(Cursor.Position.X, Cursor.Position.Y, lad.PipeTypeByOlet());
                            oletSelector.ShowDialog();
                            if (oletSelector.strTR.IsNullOrEmpty()) return Result.Cancelled;
                            PipeTypeName = oletSelector.strTR;
                            //ut.InfoMsg(PipeTypeName);
                            pipeType = fi.GetElements<PipeType, BuiltInParameter>(doc, BuiltInParameter.SYMBOL_NAME_PARAM, PipeTypeName).First();

                            //Limit sizes for Olets
                            List<string> sizeListing;
                            switch (oletSelector.strTR)
                            {
                                case "Stålrør, sømløse, termolomme":
                                case "Stålrør, sømløse sockolet":
                                    sizeListing = lad.SockoletList();
                                    break;
                                case "Stålrør, sømløse weldolet":
                                    sizeListing = lad.WeldoletList();
                                    break;
                                default:
                                    sizeListing = lad.SizeList();
                                    break;
                            }

                            if (oletSelector.strTR != "Stålrør, sømløse")
                            {
                                BaseFormTableLayoutPanel_Basic sizeSelector = new BaseFormTableLayoutPanel_Basic(Cursor.Position.X, Cursor.Position.Y, sizeListing);
                                sizeSelector.ShowDialog();
                                size = double.Parse(sizeSelector.strTR);
                            }
                            else size = selectedPipe.Diameter.FtToMm().Round(0);

                            curLvlId = selectedPipe.ReferenceLevel.Id;
                            curPipingSysTypeId = selectedPipe.MEPSystem.GetTypeId();
                            curPipeTypeId = pipeType.Id;

                            if (oletSelector.strTR != "Stålrør, sømløse")
                            {
                                using (Transaction trans2 = new Transaction(doc))
                                {
                                    trans2.Start("Create Olet");

                                    Element dummyPipe;
                                    (olet, dummyPipe) = CreateOlet(doc, iP, direction, selectedPipe, size, oletSelector.strTR);
                                    if (olet == null || dummyPipe == null)
                                    {
                                        txGp.RollBack();
                                        return Result.Cancelled;
                                    };

                                    //dbg.PlaceAdaptiveFamilyInstance(doc, "Marker Line: Red", offsetPoint, dirPoint);

                                    trans2.Commit();
                                }
                            }
                            else if (oletSelector.strTR == "Stålrør, sømløse")
                            {
                                using (Transaction trans2 = new Transaction(doc))
                                {
                                    trans2.Start("Create Tee");

                                    Pipe dummyPipe;
                                    dummyPipe = CreateTee(doc, iP, direction, selectedPipe, size, oletSelector.strTR);
                                    if (dummyPipe == null)
                                    {
                                        txGp.RollBack();
                                        return Result.Cancelled;
                                    };

                                    //dbg.PlaceAdaptiveFamilyInstance(doc, "Marker Line: Red", offsetPoint, dirPoint);

                                    trans2.Commit();
                                }
                            }
                            #endregion
                            break;
                        default:
                            return Result.Cancelled;
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

        private static Element createNextElement(Document doc, Element prevElem, string elemFamType,
            bool rotateOnSingleConDirection = false)
        {
            Cons prevElemCons = mp.GetConnectors(prevElem);

            FamilySymbol familySymbol =
                    fi.GetElements<FamilySymbol, BuiltInParameter>
                    (doc, BuiltInParameter.SYMBOL_FAMILY_AND_TYPE_NAMES_PARAM, elemFamType).FirstOrDefault();
            if (familySymbol == null) throw new Exception(elemFamType + " not found!");

            //The strange symbol activation thingie...
            //See: http://thebuildingcoder.typepad.com/blog/2014/08/activate-your-family-symbol-before-using-it.html
            if (!familySymbol.IsActive)
            {
                familySymbol.Activate();
                doc.Regenerate();
            }

            //Determine levels
            HashSet<Level> levels = fi.GetElements<Level, BuiltInCategory>(doc, BuiltInCategory.OST_Levels);
            List<(Level lvl, double dist)> levelsWithDist = new List<(Level lvl, double dist)>(levels.Count);

            foreach (Level level in levels)
            {
                (Level, double) result = (level, ((LocationPoint)prevElem.Location).Point.Z - level.ProjectElevation);
                if (result.Item2 > -1e-6) levelsWithDist.Add(result);
            }

            var minimumLevel = levelsWithDist.MinBy(x => x.dist);
            if (minimumLevel.Equals(default))
            {
                throw new Exception($"Element {prevElem.Id.ToString()} is below all levels!");
            }

            Element elem = doc.Create.NewFamilyInstance(prevElemCons.Secondary.Origin, familySymbol, minimumLevel.lvl,
                                                           StructuralType.NonStructural);
            doc.Regenerate();
            Cons elemCons = mp.GetConnectors(elem);

            if (rotateOnSingleConDirection)
            {
                RotateElementInPosition(prevElemCons.Secondary.Origin, elemCons.Primary,
                        prevElemCons.Secondary, elem);
            }
            else RotateElementInPosition(prevElemCons.Secondary.Origin, elemCons.Primary,
                        prevElemCons.Secondary, prevElemCons.Primary, elem);

            ElementTransformUtils.MoveElement(doc, elem.Id,
                prevElemCons.Secondary.Origin - elemCons.Primary.Origin);

            return elem;
        }

        private static Element createNextElement(Document doc, Element prevElem, string elemFamType,
            string sizeParName, double sizeInMm, bool rotateOnSingleConDirection = false)
        {
            Cons prevElemCons = mp.GetConnectors(prevElem);

            FamilySymbol familySymbol =
                    fi.GetElements<FamilySymbol, BuiltInParameter>
                    (doc, BuiltInParameter.SYMBOL_FAMILY_AND_TYPE_NAMES_PARAM, elemFamType).FirstOrDefault();
            if (familySymbol == null) throw new Exception(elemFamType + " not found!");

            //The strange symbol activation thingie...
            //See: http://thebuildingcoder.typepad.com/blog/2014/08/activate-your-family-symbol-before-using-it.html
            if (!familySymbol.IsActive)
            {
                familySymbol.Activate();
                doc.Regenerate();
            }

            //Determine levels
            HashSet<Level> levels = fi.GetElements<Level, BuiltInCategory>(doc, BuiltInCategory.OST_Levels);
            List<(Level lvl, double dist)> levelsWithDist = new List<(Level lvl, double dist)>(levels.Count);

            foreach (Level level in levels)
            {
                (Level, double) result = (level, ((LocationPoint)prevElem.Location).Point.Z - level.ProjectElevation);
                if (result.Item2 > -1e-6) levelsWithDist.Add(result);
            }

            var minimumLevel = levelsWithDist.MinBy(x => x.dist);
            if (minimumLevel.Equals(default))
            {
                minimumLevel = (levels.FirstOrDefault(), 0);
                //throw new Exception($"Element {prevElem.Id.ToString()} is below all levels!");
            }

            Element elem = doc.Create.NewFamilyInstance(prevElemCons.Secondary.Origin, familySymbol, minimumLevel.lvl,
                                                           StructuralType.NonStructural);

            doc.Regenerate();

            //Set size
            Parameter sizeParameter = elem.LookupParameter(sizeParName);
            sizeParameter.Set(sizeInMm.MmToFt());
            doc.Regenerate();

            //Rotate the element
            Cons elemCons = mp.GetConnectors(elem);

            if (rotateOnSingleConDirection)
            {
                RotateElementInPosition(prevElemCons.Secondary.Origin, elemCons.Primary,
                        prevElemCons.Secondary, elem);
            }
            else RotateElementInPosition(prevElemCons.Secondary.Origin, elemCons.Primary,
                        prevElemCons.Secondary, prevElemCons.Primary, elem);


            //Move in position
            ElementTransformUtils.MoveElement(doc, elem.Id,
                prevElemCons.Secondary.Origin - elemCons.Primary.Origin);

            return elem;
        }

        /// <summary>
        /// Rotates the element based on the direction between Primary and Secondary
        /// on the previous element.
        /// </summary>
        private static void RotateElementInPosition(XYZ placementPoint, Connector conOnFamilyToConnect, Connector start, Connector end, Element element)
        {
            #region Geometric manipulation

            //http://thebuildingcoder.typepad.com/blog/2012/05/create-a-pipe-cap.html

            XYZ dirToAlignTo = (start.Origin - end.Origin);

            XYZ dirToRotate = -conOnFamilyToConnect.CoordinateSystem.BasisZ;

            double rotationAngle = dirToAlignTo.AngleTo(dirToRotate);

            XYZ normal = dirToAlignTo.CrossProduct(dirToRotate);

            //Case: Normal is 0 vector -> directions are already aligned, but may need flipping
            if (normal.Equalz(new XYZ(), 1.0e-6))
            {
                //Subcase: Element needs flipping
                if (rotationAngle > 0)
                {
                    Line axis2;
                    if (dirToRotate.X.Equalz(1, 1.0e-6) || dirToRotate.Y.Equalz(1, 1.0e-6))
                    {
                        axis2 = Line.CreateBound(placementPoint, placementPoint + new XYZ(0, 0, 1));
                    }
                    else axis2 = Line.CreateBound(placementPoint, placementPoint + new XYZ(1, 0, 0));

                    ElementTransformUtils.RotateElement(element.Document, element.Id, axis2, rotationAngle);
                    return;
                }
                //Subcase: Element already in correct alignment
                return;
            }

            Transform trf = Transform.CreateRotationAtPoint(normal, rotationAngle, placementPoint);

            XYZ testRotation = trf.OfVector(dirToAlignTo).Normalize();

            if (testRotation.DotProduct(dirToAlignTo) < 0.00001)
                rotationAngle = -rotationAngle;

            Line axis = Line.CreateBound(placementPoint, placementPoint + normal);

            ElementTransformUtils.RotateElement(element.Document, element.Id, axis, rotationAngle);

            #endregion
        }

        /// <summary>
        /// Rotates the element based only on the direction of the placement connector.
        /// </summary>
        private static void RotateElementInPosition(XYZ placementPoint, Connector conOnFamilyToConnect, Connector startCon, Element element)
        {
            #region Geometric manipulation

            //http://thebuildingcoder.typepad.com/blog/2012/05/create-a-pipe-cap.html

            XYZ start = startCon.Origin;

            XYZ end = start - startCon.CoordinateSystem.BasisZ * 2;

            XYZ dirToAlignTo = (start - end);

            XYZ dirToRotate = -conOnFamilyToConnect.CoordinateSystem.BasisZ;

            double rotationAngle = dirToAlignTo.AngleTo(dirToRotate);

            XYZ normal = dirToAlignTo.CrossProduct(dirToRotate);

            //Case: Normal is 0 vector -> directions are already aligned, but may need flipping
            if (normal.Equalz(new XYZ(), 1.0e-6))
            {
                //Subcase: Element needs flipping
                if (rotationAngle > 0)
                {
                    Line axis2;
                    if (dirToRotate.X.Equalz(1, 1.0e-6) || dirToRotate.Y.Equalz(1, 1.0e-6))
                    {
                        axis2 = Line.CreateBound(placementPoint, placementPoint + new XYZ(0, 0, 1));
                    }
                    else axis2 = Line.CreateBound(placementPoint, placementPoint + new XYZ(1, 0, 0));

                    ElementTransformUtils.RotateElement(element.Document, element.Id, axis2, rotationAngle);
                    return;
                }
                //Subcase: Element already in correct alignment
                return;
            }

            Transform trf = Transform.CreateRotationAtPoint(normal, rotationAngle, placementPoint);

            XYZ testRotation = trf.OfVector(dirToAlignTo).Normalize();

            if (testRotation.DotProduct(dirToAlignTo) < 0.00001)
                rotationAngle = -rotationAngle;

            Line axis = Line.CreateBound(placementPoint, placementPoint + normal);

            ElementTransformUtils.RotateElement(element.Document, element.Id, axis, rotationAngle);

            #endregion
        }


        private static (FamilyInstance, Pipe) CreateOlet(Document doc, XYZ iP, string direction,
                                                         Pipe selectedPipe, double size, string PipeTypeName)
        {
            PipeType pipeType = fi.GetElements<PipeType, BuiltInParameter>(doc, BuiltInParameter.SYMBOL_NAME_PARAM, PipeTypeName).FirstOrDefault();
            if (pipeType == null) throw new Exception(PipeTypeName + " does not exist in current project!");

            ElementId curLvlId = selectedPipe.ReferenceLevel.Id;
            ElementId curPipingSysTypeId = selectedPipe.MEPSystem.GetTypeId();
            ElementId curPipeTypeId = pipeType.Id;

            XYZ dirPoint = CreateDummyDirectionPoint(iP, direction);
            if (dirPoint == null) return (null, null);

            Pipe dummyPipe = Pipe.Create(doc, curPipingSysTypeId, curPipeTypeId, curLvlId, iP, dirPoint);

            //Change size of the pipe
            Parameter par = dummyPipe.get_Parameter(BuiltInParameter.RBS_PIPE_DIAMETER_PARAM);
            par.Set(size.MmToFt());

            //Find the connector from the dummy pipe at intersection
            var cons = mp.GetALLConnectorsFromElements(dummyPipe);
            Connector con = cons.Where(c => c.Origin.Equalz(iP, Shared.Extensions._1mmTol)).FirstOrDefault();

            return (doc.Create.NewTakeoffFitting(con, (MEPCurve)selectedPipe), dummyPipe);

            //dbg.PlaceAdaptiveFamilyInstance(doc, "Marker Line: Red", offsetPoint, dirPoint);
        }

        private static Pipe CreateTee(Document doc, XYZ iP, string direction, Pipe selectedPipe, double size, string PipeTypeName)
        {
            PipeType pipeType = fi.GetElements<PipeType, BuiltInParameter>(doc, BuiltInParameter.SYMBOL_NAME_PARAM, PipeTypeName).FirstOrDefault();
            if (pipeType == null) throw new Exception(PipeTypeName + " does not exist in current project!");

            ElementId curLvlId = selectedPipe.ReferenceLevel.Id;
            ElementId curPipingSysTypeId = selectedPipe.MEPSystem.GetTypeId();
            ElementId curPipeTypeId = pipeType.Id;

            XYZ dirPoint = CreateDummyDirectionPoint(iP, direction);
            if (dirPoint == null) return null;

            Pipe dummyPipe = Pipe.Create(doc, curPipingSysTypeId, curPipeTypeId, curLvlId, iP, dirPoint);

            //Change size of the pipe
            Parameter par = dummyPipe.get_Parameter(BuiltInParameter.RBS_PIPE_DIAMETER_PARAM);
            par.Set(size.MmToFt());

            //Find the connector from the dummy pipe at intersection
            //var cons = mp.GetALLConnectorsFromElements(dummyPipe);
            //Connector con = cons.Where(c => c.Origin.Equalz(iP, Extensions._1mmTol)).FirstOrDefault();

            return dummyPipe;
        }

        private static XYZ CreateDummyDirectionPoint(XYZ iP, string direction)
        {
            //Create direction point
            switch (direction)
            {
                case "Top":
                    return new XYZ(iP.X, iP.Y, iP.Z + 5);
                case "Bottom":
                    return new XYZ(iP.X, iP.Y, iP.Z - 5);
                case "Front":
                    return new XYZ(iP.X, iP.Y - 5, iP.Z);
                case "Back":
                    return new XYZ(iP.X, iP.Y + 5, iP.Z);
                case "Left":
                    return new XYZ(iP.X - 5, iP.Y, iP.Z);
                case "Right":
                    return new XYZ(iP.X + 5, iP.Y, iP.Z);
                default:
                    return null;
            }
        }

        private static (Pipe pipe, XYZ point) SelectPipePoint(Document doc, UIDocument uidoc)
        {
            //Select the pipe to operate on
            var selectedPipe = Shared.BuildingCoder.BuildingCoderUtilities.SelectSingleElementOfType(uidoc, typeof(Pipe),
                "Select a pipe where to place a support!", false);
            //Get end connectors
            var conQuery = (from Connector c in mp.GetALLConnectorsFromElements(selectedPipe)
                            where (int)c.ConnectorType == 1
                            select c).ToList();

            Connector c1 = conQuery.First();
            Connector c2 = conQuery.Last();

            //Define a plane by three points
            //Detect if the pipe concides with X-axis
            //If true use another axis to define point
            Plane plane;

            if (c1.Origin.Y.Equalz(c2.Origin.Y, Shared.Extensions._epx) && c1.Origin.Z.Equalz(c2.Origin.Z, Shared.Extensions._epx))
                plane = Plane.CreateByThreePoints(c1.Origin, c2.Origin, new XYZ(c1.Origin.X, c1.Origin.Y + 5, c1.Origin.Z));
            else
                plane = Plane.CreateByThreePoints(c1.Origin, c2.Origin, new XYZ(c1.Origin.X + 5, c1.Origin.Y, c1.Origin.Z));

            //Set view sketch plane to the be the created plane
            var sp = SketchPlane.Create(doc, plane);
            uidoc.ActiveView.SketchPlane = sp;
            //Get a 3d point by picking a point
            XYZ point_in_3d = null;
            try { point_in_3d = uidoc.Selection.PickPoint("Please pick a point on the plane defined by the selected face"); }
            catch (OperationCanceledException) { }


            return ((Pipe)selectedPipe, point_in_3d);
        }
    }
}
