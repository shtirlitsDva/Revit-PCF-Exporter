using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Plumbing;
using PCF_Functions;
using iv = PCF_Functions.InputVars;
using pdef = PCF_Functions.ParameterDefinition;
using plst = PCF_Functions.ParameterList;

namespace PCF_Fittings
{
    public class PCF_Fittings_Export
    {
        public StringBuilder Export(string pipeLineAbbreviation, HashSet<Element> elements, Document document)
        {
            Document doc = document;
            string key = pipeLineAbbreviation;
            //The list of fittings, sorted by TYPE then SKEY
            IList<Element> fittingsList = elements.
                OrderBy(e => e.get_Parameter(new plst().PCF_ELEM_TYPE.Guid).AsString()).
                ThenBy(e => e.get_Parameter(new plst().PCF_ELEM_SKEY.Guid).AsString()).ToList();

            StringBuilder sbFittings = new StringBuilder();
            foreach (Element element in fittingsList)
            {
                //If the Element Type field is empty -> ignore the component
                if (string.IsNullOrEmpty(element.get_Parameter(new plst().PCF_ELEM_TYPE.Guid).AsString())) continue;

                sbFittings.Append(element.get_Parameter(new plst().PCF_ELEM_TYPE.Guid).AsString());
                sbFittings.AppendLine();
                sbFittings.Append("    COMPONENT-IDENTIFIER ");
                sbFittings.Append(element.LookupParameter("PCF_ELEM_COMPID").AsInteger());
                sbFittings.AppendLine();

                //Cast the elements gathered by the collector to FamilyInstances
                FamilyInstance familyInstance = (FamilyInstance)element;
                Options options = new Options();
                //MEPModel of the elements is accessed
                MEPModel mepmodel = familyInstance.MEPModel;
                //Get connector set for the element
                ConnectorSet connectorSet = mepmodel.ConnectorManager.Connectors;
                
                //Switch to different element type configurations
                switch (element.get_Parameter(new plst().PCF_ELEM_TYPE.Guid).AsString())
                {
                    case ("ELBOW"):
                        Connector primaryConnector = null; Connector secondaryConnector = null;

                        //Process endpoints of the component
                        foreach (Connector connector in connectorSet)
                        {
                            if (connector.GetMEPConnectorInfo().IsPrimary) primaryConnector = connector;
                            if (connector.GetMEPConnectorInfo().IsSecondary) secondaryConnector = connector;
                        }

                        sbFittings.Append(EndWriter.WriteEP1(element, primaryConnector));
                        sbFittings.Append(EndWriter.WriteEP2(element, secondaryConnector));
                        sbFittings.Append(EndWriter.WriteCP(familyInstance));

                        sbFittings.Append("    ANGLE ");
                        sbFittings.Append((Conversion.RadianToDegree(element.LookupParameter("Angle").AsDouble()) * 100).ToString("0"));
                        sbFittings.AppendLine();

                        break;

                    case ("TEE"):
                        //Sort connectors to primary, secondary and none
                        primaryConnector = null; secondaryConnector = null; Connector tertiaryConnector = null;

                        foreach (Connector connector in connectorSet)
                        {
                            #region Debug
                            //Debug
                            //sbFittings.Append(connector.GetMEPConnectorInfo().IsPrimary);
                            //sbFittings.Append(connector.GetMEPConnectorInfo().IsSecondary);
                            //sbFittings.Append((connector.GetMEPConnectorInfo().IsPrimary == false) & (connector.GetMEPConnectorInfo().IsSecondary == false));
                            //sbFittings.AppendLine();
                            #endregion

                            if (connector.GetMEPConnectorInfo().IsPrimary) primaryConnector = connector;
                            if (connector.GetMEPConnectorInfo().IsSecondary) secondaryConnector = connector;
                            if ((connector.GetMEPConnectorInfo().IsPrimary == false) && (connector.GetMEPConnectorInfo().IsSecondary == false))
                                tertiaryConnector = connector;
                        }

                        //Process endpoints of the component
                        sbFittings.Append(EndWriter.WriteEP1(element, primaryConnector));
                        sbFittings.Append(EndWriter.WriteEP2(element, secondaryConnector));
                        sbFittings.Append(EndWriter.WriteCP(familyInstance));
                        sbFittings.Append(EndWriter.WriteBP1(element, tertiaryConnector));

                        break;

                    case ("REDUCER-CONCENTRIC"):
                        //Process endpoints of the component
                        primaryConnector = null; secondaryConnector = null;

                        //Process endpoints of the component
                        foreach (Connector connector in connectorSet)
                        {
                            if (connector.GetMEPConnectorInfo().IsPrimary) primaryConnector = connector;
                            if (connector.GetMEPConnectorInfo().IsSecondary) secondaryConnector = connector;
                        }

                        sbFittings.Append(EndWriter.WriteEP1(element, primaryConnector));
                        sbFittings.Append(EndWriter.WriteEP2(element, secondaryConnector));

                        break;

                    case ("REDUCER-ECCENTRIC"):
                        goto case ("REDUCER-CONCENTRIC");

                    case ("FLANGE"):
                        primaryConnector = null; secondaryConnector = null; tertiaryConnector = null;

                        foreach (Connector connector in connectorSet)
                        {
                            if (connector.GetMEPConnectorInfo().IsPrimary) primaryConnector = connector;
                            if (connector.GetMEPConnectorInfo().IsSecondary) secondaryConnector = connector;
                        }

                        //Process endpoints of the component
                        //Secondary goes first because it is the weld neck point and the primary second because it is the flanged end
                        //(dunno if it is significant);

                        sbFittings.Append(EndWriter.WriteEP1(element, secondaryConnector));
                        sbFittings.Append(EndWriter.WriteEP2(element, primaryConnector));

                        break;

                    case ("FLANGE-BLIND"):
                        primaryConnector = null;
                        foreach (Connector connector in connectorSet) primaryConnector = connector;
                        //Connector information extraction

                        sbFittings.Append(EndWriter.WriteEP1(element, primaryConnector));

                        XYZ endPointOriginFlangeBlind = primaryConnector.Origin;
                        double connectorSizeFlangeBlind = primaryConnector.Radius;

                        //Analyses the geometry to obtain a point opposite the main connector.
                        //Extraction of the direction of the connector and reversing it
                        XYZ reverseConnectorVector = -primaryConnector.CoordinateSystem.BasisZ;
                        Line detectorLine = Line.CreateUnbound(endPointOriginFlangeBlind, reverseConnectorVector);
                        //Begin geometry analysis
                        GeometryElement geometryElement = familyInstance.get_Geometry(options);

                        //Prepare resulting point
                        XYZ endPointAnalyzed = null;

                        foreach (GeometryObject geometry in geometryElement)
                        {
                            GeometryInstance instance = geometry as GeometryInstance;
                            if (null != instance)
                            {
                                foreach (GeometryObject instObj in instance.GetInstanceGeometry())
                                {
                                    Solid solid = instObj as Solid;
                                    if (null == solid || 0 == solid.Faces.Size || 0 == solid.Edges.Size) {continue;}
                                    // Get the faces
                                    foreach (Face face in solid.Faces)
                                    {
                                        IntersectionResultArray results = null;
                                        XYZ intersection = null;
                                        SetComparisonResult result = face.Intersect(detectorLine, out results);
                                        if (result == SetComparisonResult.Overlap)
                                        {
                                            intersection = results.get_Item(0).XYZPoint;
                                            if (intersection.IsAlmostEqualTo(endPointOriginFlangeBlind) == false) endPointAnalyzed = intersection;
                                        }
                                    }
                                }
                            }
                        }

                        sbFittings.Append(EndWriter.WriteEP2(element, endPointAnalyzed, connectorSizeFlangeBlind));

                        break;

                    case ("CAP"):
                        goto case ("FLANGE-BLIND");

                    case ("OLET"):
                        primaryConnector = null; secondaryConnector = null;

                        foreach (Connector connector in connectorSet)
                        {
                            if (connector.GetMEPConnectorInfo().IsPrimary) primaryConnector = connector;
                            if (connector.GetMEPConnectorInfo().IsSecondary) secondaryConnector = connector;
                        }

                        XYZ endPointOriginOletPrimary = primaryConnector.Origin;
                        XYZ endPointOriginOletSecondary = secondaryConnector.Origin;

                        //get reference elements
                        ConnectorSet refConnectors = primaryConnector.AllRefs;
                        Element refElement = null;
                        foreach (Connector c in refConnectors) refElement = c.Owner;
                        Pipe refPipe = (Pipe)refElement;
                        //Get connector set for the pipes
                        ConnectorSet refConnectorSet = refPipe.ConnectorManager.Connectors;
                        //Filter out non-end types of connectors
                        IEnumerable<Connector> connectorEnd = from Connector connector in refConnectorSet
                                                              where connector.ConnectorType.ToString() == "End"
                                                              select connector;

                        //Following code is ported from my python solution in Dynamo.
                        //The olet geometry is analyzed with congruent rectangles to find the connection point on the pipe even for angled olets.
                        XYZ B = endPointOriginOletPrimary; XYZ D = endPointOriginOletSecondary; XYZ pipeEnd1 = connectorEnd.First().Origin; XYZ pipeEnd2 = connectorEnd.Last().Origin;
                        XYZ BDvector = D - B; XYZ ABvector = pipeEnd1 - pipeEnd2;
                        double angle = Conversion.RadianToDegree(ABvector.AngleTo(BDvector));
                        if (angle > 90)
                        {
                            ABvector = -ABvector;
                            angle = Conversion.RadianToDegree(ABvector.AngleTo(BDvector));
                        }
                        Line refsLine = Line.CreateBound(pipeEnd1, pipeEnd2);
                        XYZ C = refsLine.Project(B).XYZPoint;
                        double L3 = B.DistanceTo(C);
                        XYZ E = refsLine.Project(D).XYZPoint;
                        double L4 = D.DistanceTo(E);
                        double ratio = L4 / L3;
                        double L1 = E.DistanceTo(C);
                        double L5 = L1 / (ratio - 1);
                        XYZ A;
                        if (angle < 89)
                        {
                            XYZ ECvector = C - E;
                            ECvector = ECvector.Normalize();
                            double L = L1 + L5;
                            ECvector = ECvector.Multiply(L);
                            A = E.Add(ECvector);

                            #region Debug
                            //Debug
                            //Place family instance at points to debug the alorithm
                            //StructuralType strType = (StructuralType)4;
                            //FamilySymbol familySymbol = null;
                            //FilteredElementCollector collector = new FilteredElementCollector(doc);
                            //IEnumerable<Element> collection = collector.OfClass(typeof(FamilySymbol)).ToElements();
                            //FamilySymbol marker = null;
                            //foreach (Element e in collection)
                            //{
                            //    familySymbol = e as FamilySymbol;
                            //    if (null != familySymbol.Category)
                            //    {
                            //        if ("Structural Columns" == familySymbol.Category.Name)
                            //        {
                            //            break;
                            //        }
                            //    }
                            //}

                            //if (null != familySymbol)
                            //{
                            //    foreach (Element e in collection)
                            //    {
                            //        familySymbol = e as FamilySymbol;
                            //        if (familySymbol.FamilyName == "Marker")
                            //        {
                            //            marker = familySymbol;
                            //            Transaction trans = new Transaction(doc, "Place point markers");
                            //            trans.Start();
                            //            doc.Create.NewFamilyInstance(A, marker, strType);
                            //            doc.Create.NewFamilyInstance(B, marker, strType);
                            //            doc.Create.NewFamilyInstance(C, marker, strType);
                            //            doc.Create.NewFamilyInstance(D, marker, strType);
                            //            doc.Create.NewFamilyInstance(E, marker, strType);
                            //            trans.Commit();
                            //        }
                            //    }

                            //}
                            #endregion
                        }
                        else A = E;
                        angle = Math.Round(angle * 100);

                        sbFittings.Append(EndWriter.WriteCP(A));

                        sbFittings.Append(EndWriter.WriteBP1(element, secondaryConnector));

                        sbFittings.Append("    ANGLE ");
                        sbFittings.Append(Conversion.AngleToPCF(angle));
                        sbFittings.AppendLine();

                        break;
                }

                Composer elemParameterComposer = new Composer();
                sbFittings.Append(elemParameterComposer.ElemParameterWriter(element));

                #region CII export
                Composer composer = new Composer();
                sbFittings.Append(composer.CIIWriter(doc, key));
                #endregion

                sbFittings.Append("    UNIQUE-COMPONENT-IDENTIFIER ");
                sbFittings.Append(element.UniqueId);
                sbFittings.AppendLine();
            }

            //// Clear the output file
            //File.WriteAllBytes(InputVars.OutputDirectoryFilePath + "Fittings.pcf", new byte[0]);

            //// Write to output file
            //using (StreamWriter w = File.AppendText(InputVars.OutputDirectoryFilePath + "Fittings.pcf"))
            //{
            //    w.Write(sbFittings);
            //    w.Close();
            //}

            return sbFittings;
        }
    }
}