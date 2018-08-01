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
using Shared;

namespace PCF_Taps
{
    public class DefineTapConnection
    {
        public Result defineTapConnection(ExternalCommandData commandData, ref string msg, ElementSet elements)
        {
            UIApplication uiApp = commandData.Application;
            Document doc = uiApp.ActiveUIDocument.Document;
            Application app = doc.Application;
            UIDocument uidoc = uiApp.ActiveUIDocument;
            Transaction trans = new Transaction(doc, "Define tap");
            trans.Start();

            try
            {

                //Select tapped element
                Element tappedElement = Util.SelectSingleElement(uidoc, "Select tapped element.");

                if (!(tappedElement != null)) throw new Exception("Tap Connection cancelled!");

                //Pipe type to restrict selection of tapping element
                Type t = typeof(Pipe);

                //Select tap element
                Element tappingElement = Util.SelectSingleElementOfType(uidoc, t, "Select tapping element (must be a pipe).", false);

                if (!(tappingElement != null)) throw new Exception("Tap Connection cancelled!");

                ////Debugging
                //StringBuilder sbTaps = new StringBuilder();

                if (string.IsNullOrEmpty(tappedElement.LookupParameter("PCF_ELEM_TAP1").AsString()))
                {
                    tappedElement.LookupParameter("PCF_ELEM_TAP1").Set(tappingElement.UniqueId.ToString());
                }
                else if (string.IsNullOrEmpty(tappedElement.LookupParameter("PCF_ELEM_TAP2").AsString()))
                {
                    tappedElement.LookupParameter("PCF_ELEM_TAP2").Set(tappingElement.UniqueId.ToString());
                }
                else if (string.IsNullOrEmpty(tappedElement.LookupParameter("PCF_ELEM_TAP3").AsString()))
                {
                    tappedElement.LookupParameter("PCF_ELEM_TAP3").Set(tappingElement.UniqueId.ToString());
                }
                else
                {
                    Util.ErrorMsg("All tapping slots are taken. Manually delete unwanted values og increase number of tapping slots.");
                }

                trans.Commit();

                //Debug
                //sbTaps.Append(tappedElement.LookupParameter(InputVars.PCF_ELEM_TAP1).AsString() == "");
                //sbTaps.AppendLine();
                //sbTaps.Append(tappedElement.LookupParameter(InputVars.PCF_ELEM_TAP2).AsString() + " " + tappedElement.LookupParameter(InputVars.PCF_ELEM_TAP2).AsString() == null);
                //sbTaps.AppendLine();
                //sbTaps.Append(tappedElement.LookupParameter(InputVars.PCF_ELEM_TAP3).AsString() + " " + tappedElement.LookupParameter(InputVars.PCF_ELEM_TAP3).AsString() == null);
                //sbTaps.AppendLine();



                //// Debugging
                //// Clear the output file
                //File.WriteAllBytes(InputVars.OutputDirectoryFilePath + "Taps.pcf", new byte[0]);

                //// Write to output file
                //using (StreamWriter w = File.AppendText(InputVars.OutputDirectoryFilePath + "Taps.pcf"))
                //{
                //    w.Write(sbTaps);
                //    w.Close();
                //}
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

    public class TapsWriter
    {
        public StringBuilder tapsWriter = new StringBuilder();
        public TapsWriter(Element element, string tapName, Document doc)
        {
            try
            {
                FamilyInstance familyInstance = (FamilyInstance)element;
                XYZ elementOrigin = ((LocationPoint)familyInstance.Location).Point;
                string uniqueId = element.LookupParameter(tapName).AsString();

                Element tappingElement = null;
                if (uniqueId != null) tappingElement = doc.GetElement(uniqueId.ToString());

                Pipe tappingPipe = (Pipe)tappingElement;

                ConnectorSet connectorTapSet = tappingPipe.ConnectorManager.Connectors;
                //Filter out non-end types of connectors. The output is converted to a list to prevent deferred execution (I am afraid
                //that deferred execution leads to inconsisten returns of connectors from the connector set but am not sure it does).
                IList<Connector> connectorTapEnds = (from Connector connector in connectorTapSet
                                                     where connector.ConnectorType.ToString().Equals("End")
                                                     select connector).ToList();

                Connector end1 = connectorTapEnds.First(); Connector end2 = connectorTapEnds.Last();
                double dist1 = elementOrigin.DistanceTo(end1.Origin); double dist2 = elementOrigin.DistanceTo(end2.Origin);
                Connector tapConnector = null;

                tapConnector = dist1 > dist2 ? end2 : end1;

                XYZ connectorOrigin = tapConnector.Origin;
                double connectorSize = tapConnector.Radius;

                tapsWriter.Append("    TAP-CONNECTION");
                tapsWriter.AppendLine();
                tapsWriter.Append("    CO-ORDS ");
                if (InputVars.UNITS_CO_ORDS_MM) tapsWriter.Append(Conversion.PointStringMm(connectorOrigin));
                if (InputVars.UNITS_CO_ORDS_INCH) tapsWriter.Append(Conversion.PointStringInch(connectorOrigin));
                tapsWriter.Append(" ");
                if (InputVars.UNITS_BORE_MM) tapsWriter.Append(Conversion.PipeSizeToMm(connectorSize));
                if (InputVars.UNITS_BORE_INCH) tapsWriter.Append(Conversion.PipeSizeToInch(connectorSize));
                tapsWriter.AppendLine();
                if (!FilterDiameterLimit.FilterDL(tappingElement)) tapsWriter = null;
            }

            catch (NullReferenceException ex)
            {
                TaskDialog.Show("Tap error!", "An object in the Taps module returned: " + ex.Message + " Check if taps are correctly defined.");
            }

        }

    }
}