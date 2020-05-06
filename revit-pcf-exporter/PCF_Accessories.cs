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
using pdw = PCF_Functions.ParameterDataWriter;
using mp = Shared.MepUtils;

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

                    sbAccessories.AppendLine(element.get_Parameter(new plst().PCF_ELEM_TYPE.Guid).AsString());
                    sbAccessories.AppendLine("    COMPONENT-IDENTIFIER " + element.get_Parameter(new plst().PCF_ELEM_COMPID.Guid).AsString());

                    if (element.get_Parameter(new plst().PCF_ELEM_SPEC.Guid).AsString() == "EXISTING-INCLUDE")
                    {
                        sbAccessories.AppendLine("    STATUS DOTTED-UNDIMENSIONED");
                        sbAccessories.AppendLine("    MATERIAL-LIST EXCLUDE");
                    }

                    //Write Plant3DIso entries if turned on
                    if (InputVars.ExportToIsogen) sbAccessories.Append(Composer.Plant3DIsoWriter(element, doc));

                    //Cast the elements gathered by the collector to FamilyInstances
                    FamilyInstance familyInstance = (FamilyInstance)element;
                    Options options = new Options();

                    //Gather connectors of the element
                    var cons = mp.GetConnectors(element);

                    //Switch to different element type configurations
                    switch (element.get_Parameter(pList.PCF_ELEM_TYPE.Guid).AsString())
                    {
                        case ("FILTER"):
                            //Process endpoints of the component
                            sbAccessories.Append(EndWriter.WriteEP1(element, cons.Primary));
                            sbAccessories.Append(EndWriter.WriteEP2(element, cons.Secondary));
                            sbAccessories.Append(pdw.ParameterValue("TAG", new[] { "TAG 1", "TAG 2", "TAG 3" }, element));
                            break;

                        case ("INSTRUMENT"):
                            //Process endpoints of the component
                            sbAccessories.Append(EndWriter.WriteEP1(element, cons.Primary));
                            sbAccessories.Append(EndWriter.WriteEP2(element, cons.Secondary));
                            sbAccessories.Append(EndWriter.WriteCP(familyInstance));
                            sbAccessories.Append(pdw.ParameterValue("TAG", new[] { "TAG 1", "TAG 2", "TAG 3" }, element));

                            break;

                        case ("VALVE"):
                            goto case ("INSTRUMENT");

                        case ("VALVE-ANGLE"):
                            //Process endpoints of the component
                            sbAccessories.Append(EndWriter.WriteEP1(element, cons.Primary));
                            sbAccessories.Append(EndWriter.WriteEP2(element, cons.Secondary));

                            //The centre point is obtained by creating an bound line from primary connector and projecting the secondary point on the line.
                            XYZ reverseConnectorVector = -cons.Primary.CoordinateSystem.BasisZ;
                            Line primaryLine = Line.CreateBound(cons.Primary.Origin, cons.Primary.Origin + reverseConnectorVector * 10);
                            XYZ centrePoint = primaryLine.Project(cons.Secondary.Origin).XYZPoint;

                            sbAccessories.Append(EndWriter.WriteCP(centrePoint));

                            sbAccessories.Append(pdw.ParameterValue("TAG", new[] { "TAG 1", "TAG 2", "TAG 3" }, element));

                            break;

                        case ("INSTRUMENT-DIAL"):

                            //Connector information extraction
                            sbAccessories.Append(EndWriter.WriteEP1(element, cons.Primary));

                            XYZ primConOrigin = cons.Primary.Origin;

                            //Analyses the geometry to obtain a point opposite the main connector.
                            //Extraction of the direction of the connector and reversing it
                            reverseConnectorVector = -cons.Primary.CoordinateSystem.BasisZ;
                            Line detectorLine = Line.CreateBound(primConOrigin, primConOrigin + reverseConnectorVector * 10);
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
                                        try
                                        {
                                            SetComparisonResult result = face.Intersect(detectorLine, out results);
                                            if (result != SetComparisonResult.Overlap) continue;
                                            intersection = results.get_Item(0).XYZPoint;
                                            if (intersection.IsAlmostEqualTo(primConOrigin) == false) endPointAnalyzed = intersection;
                                        }
                                        catch (Exception)
                                        {
                                            continue;
                                        }
                                    }
                                }
                            }

                            //If the point is still null after geometry intersection, it means the analysis failed
                            //Create an artificial point
                            if (endPointAnalyzed == null)
                            {
                                endPointAnalyzed = cons.Primary.Origin + reverseConnectorVector * 2;
                            }

                            sbAccessories.Append(EndWriter.WriteCO(endPointAnalyzed));
                            sbAccessories.Append(pdw.ParameterValue("TAG", new[] { "TAG 1", "TAG 2", "TAG 3" }, element));
                            break;

                        case "SUPPORT":
                            sbAccessories.Append(EndWriter.WriteCO(familyInstance, cons.Primary));
                            sbAccessories.Append(pdw.ParameterValue("TAG", new[] { "TAG 1", "TAG 2", "TAG 3" }, element));
                            break;

                        case "FLOOR-SYMBOL":
                            sbAccessories.Append(EndWriter.WriteCO(familyInstance));
                            break;

                        case "INSTRUMENT-3WAY":
                            //Process endpoints of the component
                            sbAccessories.Append(EndWriter.WriteEP1(element, cons.Primary));
                            sbAccessories.Append(EndWriter.WriteEP2(element, cons.Secondary));
                            sbAccessories.Append(EndWriter.WriteEP3(element, cons.Tertiary));
                            sbAccessories.Append(EndWriter.WriteCP(familyInstance));
                            break;
                    }

                    Composer elemParameterComposer = new Composer();
                    sbAccessories.Append(elemParameterComposer.ElemParameterWriter(element));

                    #region CII export
                    if (InputVars.ExportToCII && !string.Equals(element.get_Parameter(pList.PCF_ELEM_TYPE.Guid).AsString(), "SUPPORT"))
                        sbAccessories.Append(Composer.CIIWriter(doc, key));
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
                throw new Exception("Element " + element.Id.IntegerValue.ToString() + " caused an exception: " + e.Message);
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