using System;
using System.Collections.Generic;
using System.Linq;
using MoreLinq;
using System.Data;
using Autodesk.Revit.UI;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Mechanical;
using Autodesk.Revit.DB.Plumbing;
using Autodesk.Revit.DB.Structure;
using Autodesk.Revit.UI.Selection;
using Shared;
using fi = Shared.Filter;
using op = Shared.Output;
using tr = Shared.Transformation;
using mp = Shared.MepUtils;

namespace MEPUtils
{
    public class FlangeCreator
    {
        public static Result CreateFlangeForElements(UIApplication uiApp)
        {
            try
            {
                Document doc = uiApp.ActiveUIDocument.Document;

                //One element selected, creates pipe at random connector
                Selection selection = uiApp.ActiveUIDocument.Selection;
                var elemIds = selection.GetElementIds();
                if (elemIds == null) throw new Exception("Getting element from selection failed!");

                //Choose the right flange to create
                FlangeCreatorChooser fcc = new FlangeCreatorChooser(uiApp);
                fcc.ShowDialog();
                fcc.Close();
                string familyAndTypeName = fcc.flangeName;

                //Collect the family symbol of the flange
                Element flangeFamilySymbol =
                    fi.GetElements<FamilySymbol, BuiltInParameter>(
                        doc, BuiltInParameter.SYMBOL_FAMILY_AND_TYPE_NAMES_PARAM, familyAndTypeName).First();

                using (Transaction trans = new Transaction(doc))
                {
                    trans.Start("Create flanges");

                    if (!((FamilySymbol)flangeFamilySymbol).IsActive)
                        ((FamilySymbol)flangeFamilySymbol).Activate();

                    //Process the elements
                    foreach (var id in elemIds)
                    {
                        Element pipeaccessory = doc.GetElement(id);
                        if (pipeaccessory is Pipe) throw new Exception("This method does not work on pipes!");
                        var pipeaccessoryCons = mp.GetConnectors(pipeaccessory);

                        #region Primary flange
                        CreateFlange(doc, pipeaccessoryCons.Primary, flangeFamilySymbol, pipeaccessory);
                        #endregion

                        #region Secondary flange
                        CreateFlange(doc, pipeaccessoryCons.Secondary, flangeFamilySymbol, pipeaccessory);
                        #endregion
                    }
                    trans.Commit();
                }

                return Result.Succeeded;
            }
            catch (Exception e)
            {
                throw new Exception(e.Message);
            }
        }

        private static void CreateFlange(
            Document doc, Connector placementConOnPipeAccessory,
            Element flangeFamilySymbol, Element pipeaccessory)
        {
            //Gather the information about the connected elements
            var allRefs = placementConOnPipeAccessory.AllRefs;
            Connector modConOnPipeAccessoryConnector = (
                from Connector c in allRefs
                where !(c.Owner is PipeInsulation)
                select c).FirstOrDefault();

            #region Determine level
            //Determine levels
            HashSet<Level> levels = fi.GetElements<Level, BuiltInCategory>(doc, BuiltInCategory.OST_Levels);
            List<(Level lvl, double dist)> levelsWithDist = new List<(Level lvl, double dist)>(levels.Count);

            foreach (Level level in levels)
            {
                (Level, double) result = (level, placementConOnPipeAccessory.Origin.Z - level.ProjectElevation);
                if (result.Item2 > -1e-6) levelsWithDist.Add(result);
            }

            var minimumLevel = levelsWithDist.MinBy(x => x.dist);
            if (minimumLevel.Equals(default))
            {
                throw new Exception($"Element {pipeaccessory.Id.ToString()} is below all levels!");
            }
            #endregion

            //Create the flange (must be rotated AND moved in place)
            Element flange = doc.Create.NewFamilyInstance(
                placementConOnPipeAccessory.Origin, (FamilySymbol)flangeFamilySymbol, minimumLevel.lvl,
                StructuralType.NonStructural);

            //Set the diameter of the flange
            double diaValue = placementConOnPipeAccessory.Radius * 2;
            Parameter dia = flange.LookupParameter("Nominal Diameter 1");
            dia.Set(diaValue);

            doc.Regenerate();

            //Access the newly created flange's connectors
            var flangeCons = mp.GetConnectors(flange);
            
            //Move the element into position, as it spawns somewhere unpredictable
            ElementTransformUtils.MoveElement(doc, flange.Id, placementConOnPipeAccessory.Origin - flangeCons.Primary.Origin);

            //Dbg.PlaceAdaptiveFamilyInstance(doc, "Marker Line: Red", flangeCons.Primary.Origin, placementConOnPipeAccessory.Origin);

            //Rotate the flange to align with the connector
            ////RotateElementInPosition(start.Origin, flangeCons.Primary, start, end, flange); <-- Old method
            RotateElementInPosition(placementConOnPipeAccessory.Origin, flangeCons.Primary, placementConOnPipeAccessory, flange);

            //Retrieve host element systemTypeId
            var pipingSystemTypeParameter = pipeaccessory.get_Parameter(BuiltInParameter.RBS_PIPING_SYSTEM_TYPE_PARAM);
            var pipingSystemTypeId = pipingSystemTypeParameter.AsElementId();

            //Retrieve owner of the connected connector
            var modOwner = modConOnPipeAccessoryConnector?.Owner;
            //If no connected elements, then return
            if (modOwner == null) return;

            //Case: Connected to Pipe
            if (modOwner is Pipe pipe)
            {
                Connector modCon2 =
                (from Connector c in ((Pipe)modConOnPipeAccessoryConnector.Owner).ConnectorManager.Connectors //End of the host/dummy pipe
                 where c.Id != modConOnPipeAccessoryConnector.Id && (int)c.ConnectorType == 1
                 select c).FirstOrDefault();

                //Create new pipe
                Pipe newPipe = Pipe.Create(
                    doc, pipingSystemTypeId, pipe.PipeType.Id,
                    pipeaccessory.LevelId, flangeCons.Secondary.Origin, modCon2.Origin);

                //Set pipe diameter
                Parameter pipeDia = newPipe.get_Parameter(BuiltInParameter.RBS_PIPE_DIAMETER_PARAM);
                pipeDia.Set(diaValue);

                //Delete the original pipe
                doc.Delete(modConOnPipeAccessoryConnector.Owner.Id);

                //Connect the new flange to element
                //start.ConnectTo(flangeCons.Primary);
            }
            //Case: Connected to something other than a pipe
            else if (modOwner is FamilyInstance)
            {
                var mf = ((FamilyInstance)modOwner).MEPModel as MechanicalFitting;
                if (mf == null) return;
                //Case: Reducer
                if (mf.PartType.ToString() == "Transition")
                {
                    //Disconnect the flange con from the connected element con
                    if (placementConOnPipeAccessory.IsConnectedTo(modConOnPipeAccessoryConnector)) placementConOnPipeAccessory.DisconnectFrom(modConOnPipeAccessoryConnector);

                    //Move the element to the start of the new flange
                    ElementTransformUtils.MoveElement(
                        doc, modOwner.Id, flangeCons.Secondary.Origin - flangeCons.Primary.Origin);
                }
            }
            else
            {
                //If owner is something else -- do nothing
                //The addin started to get error because Revit (dunno if it happened because of 2018.1 update)
                //modOwner somehow ended with piping system assigned to it
                //this do nothing else is fixing the behaviour
                //TODO: Find out why the piping system get assigned to modOwner
                //ut.ErrorMsg(modOwner.Name);

                //2017.09.05
                //Seems that allRefs contains now an additional connector with PipeInsulation as owner! Why??
                //Maybe this was as expected, but I failed to notice it before.
            }
        }

        #region Old rotation method
        //private static void RotateElementInPosition(
        //    XYZ placementPoint, Connector conOnFamilyToConnect, 
        //    Connector start, Connector end, Element element)
        //{
        //    #region Geometric manipulation
        //    //http://thebuildingcoder.typepad.com/blog/2012/05/create-a-pipe-cap.html

        //    XYZ dir = (start.Origin - end.Origin);

        //    // rotate the cap if necessary
        //    // rotate about Z first

        //    XYZ pipeHorizontalDirection = new XYZ(dir.X, dir.Y, 0.0).Normalize();
        //    //XYZ pipeHorizontalDirection = new XYZ(dir.X, dir.Y, 0.0);

        //    XYZ connectorDirection = -conOnFamilyToConnect.CoordinateSystem.BasisZ;

        //    double zRotationAngle = pipeHorizontalDirection.AngleTo(connectorDirection);

        //    Transform trf = Transform.CreateRotationAtPoint(XYZ.BasisZ, zRotationAngle, placementPoint);

        //    XYZ testRotation = trf.OfVector(connectorDirection).Normalize();

        //    if (Math.Abs(testRotation.DotProduct(pipeHorizontalDirection) - 1) > 0.00001) zRotationAngle = -zRotationAngle;

        //    Line axis = Line.CreateBound(placementPoint, placementPoint + XYZ.BasisZ);

        //    ElementTransformUtils.RotateElement(element.Document, element.Id, axis, zRotationAngle);

        //    //Parameter comments = element.get_Parameter(BuiltInParameter.ALL_MODEL_INSTANCE_COMMENTS);
        //    //comments.Set("Horizontal only");

        //    // Need to rotate vertically?

        //    if (Math.Abs(dir.DotProduct(XYZ.BasisZ)) > 0.000001)
        //    {
        //        // if pipe is straight up and down, 
        //        // kludge it my way else

        //        if (dir.X.Round(3) == 0 && dir.Y.Round(3) == 0 && dir.Z.Round(3) != 0)
        //        {
        //            XYZ yaxis = new XYZ(0.0, 1.0, 0.0);

        //            double rotationAngle = dir.AngleTo(yaxis); //<-- value in radians

        //            if (dir.Z > 0) rotationAngle = -rotationAngle; //<-- Here is the culprit: Equals(1) was wrong!

        //            axis = Line.CreateBound(placementPoint, new XYZ(placementPoint.X, placementPoint.Y + 5, placementPoint.Z));

        //            ElementTransformUtils.RotateElement(element.Document, element.Id, axis, rotationAngle);

        //            //comments.Set("Vertical!");
        //        }
        //        else
        //        {
        //            #region sloped pipes

        //            double rotationAngle = dir.AngleTo(pipeHorizontalDirection);

        //            XYZ normal = pipeHorizontalDirection.CrossProduct(XYZ.BasisZ);

        //            trf = Transform.CreateRotationAtPoint(normal, rotationAngle, placementPoint);

        //            testRotation = trf.OfVector(dir).Normalize();

        //            if (Math.Abs(testRotation.DotProduct(pipeHorizontalDirection) - 1) < 0.00001)
        //                rotationAngle = -rotationAngle;

        //            axis = Line.CreateBound(placementPoint, placementPoint + normal);

        //            ElementTransformUtils.RotateElement(element.Document, element.Id, axis, rotationAngle);

        //            //comments.Set("Sloped");

        //            #endregion
        //        }
        //    }
        //    #endregion
        //} 
        #endregion

        /// <summary>
        /// Rotates the element based only on the direction of the placement connector.
        /// </summary>
        private static void RotateElementInPosition(
            XYZ placementPoint, Connector conOnFamilyToConnect,
            Connector startCon, Element element)
        {
            #region Geometric manipulation
            //http://thebuildingcoder.typepad.com/blog/2012/05/create-a-pipe-cap.html

            XYZ start = startCon.Origin;

            XYZ end = start - startCon.CoordinateSystem.BasisZ * 2;

            //Dbg.PlaceAdaptiveFamilyInstance(element.Document, "Marker Line: Red", start, end);

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
    }
}
