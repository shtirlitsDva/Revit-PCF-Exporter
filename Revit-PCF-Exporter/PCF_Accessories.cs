using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Plumbing;
using PCF_Functions;
using PCF_Taps;

using pdef = PCF_Functions.ParameterDefinition;
using plst = PCF_Functions.ParameterList;

namespace PCF_Accessories
{
    public class PCF_Accessories_Export
    {
        public StringBuilder Export(string pipeLineAbbreviation, HashSet<Element> elements, Document document)
        {
            Document doc = document;
            string key = pipeLineAbbreviation;
            plst pList = new plst();
            //paramList = new plst();
            //The list of fittings, sorted by TYPE then SKEY
            IList<Element> accessoriesList = elements.
                OrderBy(e => e.get_Parameter(pList.PCF_ELEM_TYPE.Guid).AsString()).
                ThenBy(e => e.get_Parameter(pList.PCF_ELEM_SKEY.Guid).AsString()).ToList();
            
            StringBuilder sbAccessories = new StringBuilder();

            //This is a workaround to try to determine what element caused an exception
            Element element = null;

            try
            {
                foreach (Element Element in accessoriesList)
                {
                    //This is a workaround to try to determine what element caused an exception
                    element = Element;
                    //If the Element Type field is empty -> ignore the component
                    if (string.IsNullOrEmpty(element.get_Parameter(pList.PCF_ELEM_TYPE.Guid).AsString())) continue;

                    //Write Plant3DIso entries if turned on
                    if (InputVars.ExportToPlant3DIso) sbAccessories.Append(Composer.Plant3DIsoWriter(element));

                    sbAccessories.AppendLine(element.get_Parameter(new plst().PCF_ELEM_TYPE.Guid).AsString());
                    sbAccessories.AppendLine("    COMPONENT-IDENTIFIER " + element.get_Parameter(new plst().PCF_ELEM_COMPID.Guid).AsInteger());

                    //Cast the elements gathered by the collector to FamilyInstances
                    FamilyInstance familyInstance = (FamilyInstance)element;
                    Options options = new Options();
                    //MEPModel of the elements is accessed
                    MEPModel mepmodel = familyInstance.MEPModel;
                    //Get connector set for the element
                    ConnectorSet connectorSet = mepmodel.ConnectorManager.Connectors;

                    //Switch to different element type configurations
                    switch (element.get_Parameter(pList.PCF_ELEM_TYPE.Guid).AsString())
                    {
                        case ("FILTER"):
                            //Process endpoints of the component
                            Connector primaryConnector = null; Connector secondaryConnector = null;

                            foreach (Connector connector in connectorSet)
                            {
                                if (connector.GetMEPConnectorInfo().IsPrimary) primaryConnector = connector;
                                if (connector.GetMEPConnectorInfo().IsSecondary) secondaryConnector = connector;
                            }

                            //Process endpoints of the component
                            sbAccessories.Append(EndWriter.WriteEP1(element, primaryConnector));
                            sbAccessories.Append(EndWriter.WriteEP2(element, secondaryConnector));

                            break;

                        case ("INSTRUMENT"):
                            //Process endpoints of the component
                            primaryConnector = null; secondaryConnector = null;

                            foreach (Connector connector in connectorSet)
                            {
                                if (connector.GetMEPConnectorInfo().IsPrimary) primaryConnector = connector;
                                if (connector.GetMEPConnectorInfo().IsSecondary) secondaryConnector = connector;
                            }

                            //Process endpoints of the component
                            sbAccessories.Append(EndWriter.WriteEP1(element, primaryConnector));
                            sbAccessories.Append(EndWriter.WriteEP2(element, secondaryConnector));
                            sbAccessories.Append(EndWriter.WriteCP(familyInstance));

                            break;

                        case ("VALVE"):
                            goto case ("INSTRUMENT");

                        case ("VALVE-ANGLE"):
                            //Process endpoints of the component
                            primaryConnector = null; secondaryConnector = null;

                            foreach (Connector connector in connectorSet)
                            {
                                if (connector.GetMEPConnectorInfo().IsPrimary) primaryConnector = connector;
                                if (connector.GetMEPConnectorInfo().IsSecondary) secondaryConnector = connector;
                            }

                            //Process endpoints of the component
                            sbAccessories.Append(EndWriter.WriteEP1(element, primaryConnector));
                            sbAccessories.Append(EndWriter.WriteEP2(element, secondaryConnector));

                            //The centre point is obtained by creating an unbound line from primary connector and projecting the secondary point on the line.
                            XYZ reverseConnectorVector = -primaryConnector.CoordinateSystem.BasisZ;
                            Line primaryLine = Line.CreateUnbound(primaryConnector.Origin,reverseConnectorVector);
                            XYZ centrePoint = primaryLine.Project(secondaryConnector.Origin).XYZPoint;

                            sbAccessories.Append(EndWriter.WriteCP(centrePoint));

                            break;

                        case ("INSTRUMENT-DIAL"):
                            ////Process endpoints of the component
                            //primaryConnector = null;

                            //foreach (Connector connector in connectorSet) primaryConnector = connector;

                            ////Process endpoints of the component
                            //sbAccessories.Append(EndWriter.WriteEP1(element, primaryConnector));

                            ////The co-ords point is obtained by creating an unbound line from primary connector and taking an arbitrary point a long the line.
                            //reverseConnectorVector = -primaryConnector.CoordinateSystem.BasisZ.Multiply(0.656167979);
                            //XYZ coOrdsPoint = primaryConnector.Origin;
                            //Transform pointTranslation;
                            //pointTranslation = Transform.CreateTranslation(reverseConnectorVector);
                            //coOrdsPoint = pointTranslation.OfPoint(coOrdsPoint);

                            primaryConnector = null;
                            foreach (Connector connector in connectorSet) primaryConnector = connector;
                            //Connector information extraction

                            sbAccessories.Append(EndWriter.WriteEP1(element, primaryConnector));

                            XYZ primConOrigin = primaryConnector.Origin;
                        
                            //Analyses the geometry to obtain a point opposite the main connector.
                            //Extraction of the direction of the connector and reversing it
                            reverseConnectorVector = -primaryConnector.CoordinateSystem.BasisZ;
                            Line detectorLine = Line.CreateUnbound(primConOrigin, reverseConnectorVector);
                            //Begin geometry analysis
                            GeometryElement geometryElement = familyInstance.get_Geometry(options);

                            //Prepare resulting point
                            XYZ endPointAnalyzed = null;

                            foreach (GeometryObject geometry in geometryElement)
                            {
                                GeometryInstance instance = geometry as GeometryInstance;
                                if (null == instance) continue;
                                foreach (GeometryObject instObj in instance.GetInstanceGeometry())
                                {
                                    Solid solid = instObj as Solid;
                                    if (null == solid || 0 == solid.Faces.Size || 0 == solid.Edges.Size) continue;
                                    foreach (Face face in solid.Faces)
                                    {
                                        IntersectionResultArray results = null;
                                        XYZ intersection = null;
                                        SetComparisonResult result = face.Intersect(detectorLine, out results);
                                        if (result != SetComparisonResult.Overlap) continue;
                                        intersection = results.get_Item(0).XYZPoint;
                                        if (intersection.IsAlmostEqualTo(primConOrigin) == false) endPointAnalyzed = intersection;
                                    }
                                }
                            }

                            sbAccessories.Append(EndWriter.WriteCO(endPointAnalyzed));

                            break;

                        case "SUPPORT":
                            primaryConnector = (from Connector c in connectorSet where c.GetMEPConnectorInfo().IsPrimary select c).FirstOrDefault();
                            sbAccessories.Append(EndWriter.WriteCO(familyInstance, primaryConnector));
                            break;

                        case "INSTRUMENT-3WAY":
                            //Sort connectors to primary, secondary and none
                            primaryConnector = null; secondaryConnector = null; Connector tertiaryConnector = null;

                            foreach (Connector connector in connectorSet)
                            {
                                if (connector.GetMEPConnectorInfo().IsPrimary) primaryConnector = connector;
                                if (connector.GetMEPConnectorInfo().IsSecondary) secondaryConnector = connector;
                                if ((connector.GetMEPConnectorInfo().IsPrimary == false) && (connector.GetMEPConnectorInfo().IsSecondary == false))
                                    tertiaryConnector = connector;
                            }

                            //Process endpoints of the component
                            sbAccessories.Append(EndWriter.WriteEP1(element, primaryConnector));
                            sbAccessories.Append(EndWriter.WriteEP2(element, secondaryConnector));
                            sbAccessories.Append(EndWriter.WriteEP3(element, tertiaryConnector));
                            sbAccessories.Append(EndWriter.WriteCP(familyInstance));
                            break;
                    }

                    Composer elemParameterComposer = new Composer();
                    sbAccessories.Append(elemParameterComposer.ElemParameterWriter(element));

                    #region CII export
                    Composer composer = new Composer();
                    if (!string.Equals(element.get_Parameter(pList.PCF_ELEM_TYPE.Guid).AsString(), "SUPPORT")) sbAccessories.Append(composer.CIIWriter(doc, key));
                    #endregion

                    sbAccessories.Append("    UNIQUE-COMPONENT-IDENTIFIER ");
                    sbAccessories.Append(element.UniqueId);
                    sbAccessories.AppendLine();

                    //Process tap entries of the element if any
                    //Diameter Limit nullifies the tapsWriter output if the tap diameter is less than the limit so it doesn't get exported
                    if (string.IsNullOrEmpty(element.LookupParameter("PCF_ELEM_TAP1").AsString()) == false)
                    {
                        TapsWriter tapsWriter = new TapsWriter(element, "PCF_ELEM_TAP1", doc);
                        sbAccessories.Append(tapsWriter.tapsWriter);
                    }
                    if (string.IsNullOrEmpty(element.LookupParameter("PCF_ELEM_TAP2").AsString()) == false)
                    {
                        TapsWriter tapsWriter = new TapsWriter(element, "PCF_ELEM_TAP2", doc);
                        sbAccessories.Append(tapsWriter.tapsWriter);
                    }
                    if (string.IsNullOrEmpty(element.LookupParameter("PCF_ELEM_TAP3").AsString()) == false)
                    {
                        TapsWriter tapsWriter = new TapsWriter(element, "PCF_ELEM_TAP3", doc);
                        sbAccessories.Append(tapsWriter.tapsWriter);
                    }
                }
            }
            catch (Exception e)
            {
                throw new Exception("Element " + element.Id.IntegerValue.ToString() + " caused an exception: "+e.Message);
            }

            //// Clear the output file
            //System.IO.File.WriteAllBytes(InputVars.OutputDirectoryFilePath + "Accessories.pcf", new byte[0]);

            //// Write to output file
            //using (StreamWriter w = File.AppendText(InputVars.OutputDirectoryFilePath + "Accessories.pcf"))
            //{
            //    w.Write(sbAccessories);
            //    w.Close();
            //}
            return sbAccessories;
        }
    }
}