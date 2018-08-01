using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Plumbing;
using Autodesk.Revit.UI;
using Autodesk.Revit.ApplicationServices;
using PCF_Functions;
using Shared.BuildingCoder;
using iv = PCF_Functions.InputVars;

namespace PCF_Functions
{

    public class SetSupportPipingSystem
    {
        public Result Execute(ExternalCommandData commandData, ref string msg, ElementSet elements)
        {
            UIApplication uiApp = commandData.Application;
            Document doc = uiApp.ActiveUIDocument.Document;
            UIDocument uidoc = uiApp.ActiveUIDocument;
            Transaction trans = new Transaction(doc, "Set System");
            MEPModel mepModel = null;

            trans.Start();
            try
            {
                //Select Element to provide a system
                //Pipe type to restrict selection of tapping element
                Type t = typeof(Pipe);

                //Select tap element
                Element systemDonor = Util.SelectSingleElementOfType(uidoc, t, "Select a pipe in desired system.", false);

                if (systemDonor == null) throw new Exception("System assignment cancelled!");

                //Select Element to add to system
                Element elementToAdd = Util.SelectSingleElement(uidoc, "Select support to add to system.");

                if (elementToAdd == null) throw new Exception("System assignment cancelled!");

                //Cast the selected element to Pipe
                Pipe pipe = (Pipe) systemDonor;
                //Get the pipe type from pipe
                ElementId pipeTypeId = pipe.PipeType.Id;

                //Get system type from pipe
                ConnectorSet pipeConnectors = pipe.ConnectorManager.Connectors;
                Connector pipeConnector = (from Connector c in pipeConnectors where true select c).FirstOrDefault();
                ElementId pipeSystemType = pipeConnector.MEPSystem.GetTypeId();

                //Collect levels and select one level
                FilteredElementCollector collector = new FilteredElementCollector(doc);
                ElementClassFilter levelFilter = new ElementClassFilter(typeof(Level));
                ElementId levelId = collector.WherePasses(levelFilter).FirstElementId();

                //Get the connector from the support
                FamilyInstance familyInstanceToAdd = (FamilyInstance) elementToAdd;
                ConnectorSet connectorSetToAdd = new ConnectorSet();
                mepModel = familyInstanceToAdd.MEPModel;
                connectorSetToAdd = mepModel.ConnectorManager.Connectors;
                if (connectorSetToAdd.IsEmpty)
                    throw new Exception(
                        "The support family lacks a connector. Please read the documentation for correct procedure of setting up a support element.");
                Connector connectorToConnect =
                    (from Connector c in connectorSetToAdd where true select c).FirstOrDefault();

                //Create a point in space to connect the pipe
                XYZ direction = connectorToConnect.CoordinateSystem.BasisZ.Multiply(2);
                XYZ origin = connectorToConnect.Origin;
                XYZ pointInSpace = origin.Add(direction);
                
                //Create the pipe
                Pipe newPipe = Pipe.Create(doc, pipeTypeId, levelId, connectorToConnect, pointInSpace);

                //Change the pipe system type to match the picked pipe (it is not always matching)
                newPipe.SetSystemType(pipeSystemType);

                trans.Commit();

                trans.Start("Delete the pipe");
                
                //Delete the pipe
                doc.Delete(newPipe.Id);

                trans.Commit();
            }

            catch (Autodesk.Revit.Exceptions.OperationCanceledException)
            {
                trans.RollBack();
                return Result.Cancelled;
            }

            catch (Exception ex)
            {
                trans.RollBack();
                msg = ex.Message;
                return Result.Failed;
            }

            return Result.Succeeded;
        }
    }
}