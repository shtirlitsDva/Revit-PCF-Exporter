using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Plumbing;
using PCF_Functions;
using Shared;
using iv = PCF_Functions.InputVars;
using pdef = PCF_Functions.ParameterDefinition;
using plst = PCF_Functions.ParameterList;
using mp = Shared.MepUtils;

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
                OrderBy(e => e.get_Parameter(plst.PCF_ELEM_TYPE.Guid).AsString()).
                ThenBy(e => e.get_Parameter(plst.PCF_ELEM_SKEY.Guid).AsString()).ToList();

            StringBuilder sbFittings = new StringBuilder();
            foreach (Element element in fittingsList)
            {
                sbFittings.AppendLine(element.get_Parameter(plst.PCF_ELEM_TYPE.Guid).AsString());
                sbFittings.AppendLine("    COMPONENT-IDENTIFIER " + element.get_Parameter(plst.PCF_ELEM_COMPID.Guid).AsString());

                if (element.get_Parameter(plst.PCF_ELEM_SPEC.Guid).AsString() == "EXISTING-INCLUDE")
                {
                    sbFittings.AppendLine("    STATUS DOTTED-UNDIMENSIONED");
                    sbFittings.AppendLine("    MATERIAL-LIST EXCLUDE");
                }

                //Write Plant3DIso entries if turned on
                if (iv.ExportToIsogen) sbFittings.Append(Composer.Plant3DIsoWriter(element, doc));

                //Cast the elements gathered by the collector to FamilyInstances
                FamilyInstance familyInstance = (FamilyInstance)element;
                Options options = new Options();

                //Gather connectors of the element
                var cons = mp.GetConnectors(element);

                //Switch to different element type configurations
                switch (element.get_Parameter(plst.PCF_ELEM_TYPE.Guid).AsString())
                {
                    case ("ELBOW"):
                        sbFittings.Append(EndWriter.WriteEP1(element, cons.Primary));
                        sbFittings.Append(EndWriter.WriteEP2(element, cons.Secondary));
                        sbFittings.Append(EndWriter.WriteCP(familyInstance));

                        sbFittings.Append("    ANGLE ");

                        Parameter par = element.LookupParameter("Angle");
                        if (par == null) par = element.LookupParameter("angle");
                        if (par == null) throw new Exception($"Angle parameter on elbow {element.Id.IntegerValue} does not exist or is named differently!");
                        sbFittings.Append((Conversion.RadianToDegree(par.AsDouble()) * 100).ToString("0"));
                        sbFittings.AppendLine();

                        break;
                    //case ("BEND"):
                    //    sbFittings.Append(EndWriter.WriteEP1(element, cons.Primary));
                    //    sbFittings.Append(EndWriter.WriteEP2(element, cons.Secondary));
                    //    sbFittings.Append(EndWriter.WriteCP(familyInstance));
                    //    break;

                    case ("TEE"):
                        //Process endpoints of the component
                        sbFittings.Append(EndWriter.WriteEP1(element, cons.Primary));
                        sbFittings.Append(EndWriter.WriteEP2(element, cons.Secondary));
                        sbFittings.Append(EndWriter.WriteCP(familyInstance));
                        sbFittings.Append(EndWriter.WriteBP1(element, cons.Tertiary));

                        break;

                    case "UNION":
                    case ("REDUCER-CONCENTRIC"):
                        sbFittings.Append(EndWriter.WriteEP1(element, cons.Primary));
                        sbFittings.Append(EndWriter.WriteEP2(element, cons.Secondary));

                        break;

                    case ("REDUCER-ECCENTRIC"):
                        goto case ("REDUCER-CONCENTRIC");

                    case ("COUPLING"):
                        goto case ("REDUCER-CONCENTRIC");

                    case ("FLANGE"):
                        //Process endpoints of the component
                        //Secondary goes first because it is the weld neck point and the primary second because it is the flanged end
                        //(dunno if it is significant); It is not, it should be specified the type of end, BW, PL, FL etc. to work correctly.

                        sbFittings.Append(EndWriter.WriteEP1(element, cons.Secondary));
                        sbFittings.Append(EndWriter.WriteEP2(element, cons.Primary));

                        break;

                    case ("FLANGE-BLIND"):
                        sbFittings.Append(EndWriter.WriteEP1(element, cons.Primary));

                        XYZ endPointOriginFlangeBlind = cons.Primary.Origin;
                        double connectorSizeFlangeBlind = cons.Primary.Radius;

                        //Analyses the geometry to obtain a point opposite the main connector.
                        //Extraction of the direction of the connector and reversing it
                        XYZ reverseConnectorVector = -cons.Primary.CoordinateSystem.BasisZ;
                        Line detectorLine = Line.CreateBound(endPointOriginFlangeBlind, endPointOriginFlangeBlind + reverseConnectorVector * 10);
                        //Begin geometry analysis
                        GeometryElement geometryElement = familyInstance.get_Geometry(options);

                        //Prepare resulting point
                        XYZ endPointAnalyzed = null;

                        foreach (GeometryObject geometry in geometryElement)
                        {
                            if (geometry is GeometryInstance instance)
                            {
                                foreach (GeometryObject instObj in instance.GetInstanceGeometry())
                                {
                                    Solid solid = instObj as Solid;
                                    if (null == solid || 0 == solid.Faces.Size || 0 == solid.Edges.Size) { continue; }
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
                        XYZ endPointOriginOletPrimary = cons.Primary.Origin;
                        XYZ endPointOriginOletSecondary = cons.Secondary.Origin;

                        //get reference elements
                        Pipe refPipe = null;
                        var refCons = mp.GetAllConnectorsFromConnectorSet(cons.Primary.AllRefs);

                        bool isTap = false;
                        Connector refCon = refCons.Where(x => x.Owner.IsType<Pipe>()).FirstOrDefault();
                        if (refCon == null)
                        {
                            //Find the target pipe

                            IList<BuiltInCategory> bics = new List<BuiltInCategory>(3)
                            {
                                BuiltInCategory.OST_PipeAccessory,
                                BuiltInCategory.OST_PipeCurves,
                                BuiltInCategory.OST_PipeFitting
                            };

                            IList<ElementFilter> a = new List<ElementFilter>(bics.Count());

                            foreach (BuiltInCategory bic in bics) a.Add(new ElementCategoryFilter(bic));

                            LogicalOrFilter categoryFilter = new LogicalOrFilter(a);

                            LogicalAndFilter familyInstanceFilter = new LogicalAndFilter(categoryFilter, new ElementClassFilter(typeof(FamilyInstance)));

                            IList<ElementFilter> b = new List<ElementFilter>
                            {
                                new ElementClassFilter(typeof(Pipe)),
                                familyInstanceFilter
                            };
                            LogicalOrFilter filter = new LogicalOrFilter(b);

                            var view3D = Shared.Filter.Get3DView(doc);
                            var refIntersect = new ReferenceIntersector(filter, FindReferenceTarget.Element, view3D);
                            ReferenceWithContext rwc = refIntersect.FindNearest(cons.Primary.Origin, cons.Primary.CoordinateSystem.BasisZ);
                            var refId = rwc.GetReference().ElementId;
                            Element refElement = doc.GetElement(refId);

                            if (refElement is Pipe pipe)
                            {
                                refPipe = pipe;
                            }
                            else
                            {
                                //If no reference pipe can be found
                                //The olet could be tapped to a FamilyInstance
                                //Check to see if any of PipeAccessories or PipeFittings
                                //Have the olet as tap
                                HashSet<Element> possibleTappedElements = Shared.Filter.GetElements(
                                    doc,
                                    new List<BuiltInCategory>() { BuiltInCategory.OST_PipeFitting, BuiltInCategory.OST_PipeAccessory },
                                    new List<Type>() { typeof(FamilyInstance), typeof(FamilyInstance) });

                                string oletUid = element.UniqueId;
                                var query = possibleTappedElements.Where(x =>
                                    x.LookupParameter("PCF_ELEM_TAP1").AsString() == oletUid ||
                                    x.LookupParameter("PCF_ELEM_TAP2").AsString() == oletUid ||
                                    x.LookupParameter("PCF_ELEM_TAP3").AsString() == oletUid);

                                if (query.Count() == 0) throw new Exception($"Olet {element.Id.IntegerValue} cannot find a reference Pipe!");
                                else
                                {
                                    //It is detected that the olet is a tapping element
                                    isTap = true;

                                    sbFittings.Append(EndWriter.WriteTappingOletCP(cons.Primary, element.LookupParameter("PCF_ELEM_END1"), query.First()));
                                    sbFittings.Append(EndWriter.WriteBP1(element, cons.Secondary));
                                }
                            }
                        }
                        else { refPipe = (Pipe)refCon.Owner; }

                        //Guard against olet being tapping olet
                        if (!isTap)
                        {
                            Cons refPipeCons = new Cons(refPipe);

                            //Following code is ported from my python solution in Dynamo.
                            //The olet geometry is analyzed with congruent rectangles to find the connection point on the pipe even for angled olets.
                            XYZ B = endPointOriginOletPrimary; XYZ D = endPointOriginOletSecondary; XYZ pipeEnd1 = refPipeCons.Primary.Origin; XYZ pipeEnd2 = refPipeCons.Secondary.Origin;
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

                            sbFittings.Append(EndWriter.WriteBP1(element, cons.Secondary));

                            sbFittings.Append("    ANGLE ");
                            sbFittings.Append(Conversion.AngleToPCF(angle));
                            sbFittings.AppendLine();
                        }

                        break;

                        // In case of a Hot-tap valve
                    case ("VALVE"):
                        //Process endpoints of the component
                        sbFittings.Append(EndWriter.WriteEP1(element, cons.Primary));
                        sbFittings.Append(EndWriter.WriteEP2(element, cons.Secondary));
                        sbFittings.Append(EndWriter.WriteCP(familyInstance));
                        sbFittings.Append(ParameterDataWriter.ParameterValue(
                            "TAG", new[] { "TAG 1", "TAG 2", "TAG 3" }, element));
                        break;
                }

                Composer elemParameterComposer = new Composer();
                sbFittings.Append(elemParameterComposer.ElemParameterWriter(element));

                #region CII export
                if (iv.ExportToCII) sbFittings.Append(Composer.CIIWriter(doc, key));
                #endregion

                sbFittings.Append("    UNIQUE-COMPONENT-IDENTIFIER ");
                sbFittings.Append(element.UniqueId);
                sbFittings.AppendLine();

                //Process tap entries of the element if any
                //Diameter Limit nullifies the tapsWriter output if the tap diameter is less than the limit so it doesn't get exported
                if (string.IsNullOrEmpty(element.LookupParameter("PCF_ELEM_TAP1").AsString()) == false)
                {
                    PCF_Taps.TapsWriter tapsWriter = new PCF_Taps.TapsWriter(element, "PCF_ELEM_TAP1", doc);
                    sbFittings.Append(tapsWriter.tapsWriter);
                }
                if (string.IsNullOrEmpty(element.LookupParameter("PCF_ELEM_TAP2").AsString()) == false)
                {
                    PCF_Taps.TapsWriter tapsWriter = new PCF_Taps.TapsWriter(element, "PCF_ELEM_TAP2", doc);
                    sbFittings.Append(tapsWriter.tapsWriter);
                }
                if (string.IsNullOrEmpty(element.LookupParameter("PCF_ELEM_TAP3").AsString()) == false)
                {
                    PCF_Taps.TapsWriter tapsWriter = new PCF_Taps.TapsWriter(element, "PCF_ELEM_TAP3", doc);
                    sbFittings.Append(tapsWriter.tapsWriter);
                }
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