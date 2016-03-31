﻿using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using Autodesk.Revit.DB.Plumbing;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB.Structure;

using PCF_Functions;
using PCF_Taps;
using pd = PCF_Functions.ParameterData;
using pdef = PCF_Functions.ParameterDefinition;

namespace PCF_Accessories
{
    public static class PCF_Accessories_Export
    {
        static IEnumerable<Element> accessoriesList;
        public static StringBuilder sbAccessories;
        static Document doc;

        public static StringBuilder Export(IEnumerable<Element> elements, Document document)
        {
            doc = document;
            //The list of fittings, sorted by TYPE then SKEY
            accessoriesList = elements.
                OrderBy(e => e.LookupParameter(pd.PCF_ELEM_TYPE).AsString()).
                ThenBy(e => e.LookupParameter(pd.PCF_ELEM_SKEY).AsString());

            sbAccessories = new StringBuilder();
            foreach (Element element in accessoriesList)
            {
                //If the Element Type field is empty -> ignore the component
                if (string.IsNullOrEmpty(element.LookupParameter(pd.PCF_ELEM_TYPE).AsString())) continue;

                sbAccessories.Append(element.LookupParameter(pd.PCF_ELEM_TYPE).AsString());
                sbAccessories.AppendLine();
                sbAccessories.Append("    COMPONENT-IDENTIFIER ");
                sbAccessories.Append(element.LookupParameter(pd.PCF_ELEM_COMPID).AsInteger());
                sbAccessories.AppendLine();

                //Cast the elements gathered by the collector to FamilyInstances
                FamilyInstance familyInstance = (FamilyInstance)element;
                Options options = new Options();
                //MEPModel of the elements is accessed
                MEPModel mepmodel = familyInstance.MEPModel;
                //Get connector set for the element
                ConnectorSet connectorSet = mepmodel.ConnectorManager.Connectors;

                //Switch to different element type configurations
                switch (element.LookupParameter(pd.PCF_ELEM_TYPE).AsString())
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
                        //Process endpoints of the component
                        primaryConnector = null;

                        foreach (Connector connector in connectorSet) primaryConnector = connector;

                        //Process endpoints of the component
                        sbAccessories.Append(EndWriter.WriteEP1(element, primaryConnector));

                        //The co-ords point is obtained by creating an unbound line from primary connector and taking an arbitrary point a long the line.
                        reverseConnectorVector = -primaryConnector.CoordinateSystem.BasisZ.Multiply(0.656167979);
                        XYZ coOrdsPoint = primaryConnector.Origin;
                        Transform pointTranslation;
                        pointTranslation = Transform.CreateTranslation(reverseConnectorVector);
                        coOrdsPoint = pointTranslation.OfPoint(coOrdsPoint);

                        sbAccessories.Append(EndWriter.WriteCO(coOrdsPoint));
                        
                        break;

                }

                var pQuery = from p in new pdef().ListParametersAll where !string.IsNullOrEmpty(p.Keyword) && string.Equals(p.Domain, "ELEM") select p;

                foreach (pdef p in pQuery)
                {
                    //Check for parameter's storage type (can be Int for select few parameters)
                    int sT = (int)element.get_Parameter(p.Guid).StorageType;

                    if (sT == 1)
                    {
                        //Check if the parameter contains anything
                        if (string.IsNullOrEmpty(element.get_Parameter(p.Guid).AsInteger().ToString())) continue;
                        sbAccessories.Append("    " + p.Keyword + " ");
                        sbAccessories.Append(element.get_Parameter(p.Guid).AsInteger());
                    }
                    else if (sT == 3)
                    {
                        //Check if the parameter contains anything
                        if (string.IsNullOrEmpty(element.get_Parameter(p.Guid).AsString())) continue;
                        sbAccessories.Append("    " + p.Keyword + " ");
                        sbAccessories.Append(element.get_Parameter(p.Guid).AsString());
                    }
                    sbAccessories.AppendLine();
                }

                sbAccessories.Append("    UNIQUE-COMPONENT-IDENTIFIER ");
                sbAccessories.Append(element.UniqueId);
                sbAccessories.AppendLine();

                //Process tap entries of the element if any
                if (string.IsNullOrEmpty(element.LookupParameter(pd.PCF_ELEM_TAP1).AsString()) == false)
                {
                    TapsWriter tapsWriter = new TapsWriter(element, pd.PCF_ELEM_TAP1, doc);
                    sbAccessories.Append(tapsWriter.tapsWriter);
                }
                if (string.IsNullOrEmpty(element.LookupParameter(pd.PCF_ELEM_TAP2).AsString()) == false)
                {
                    TapsWriter tapsWriter = new TapsWriter(element, pd.PCF_ELEM_TAP2, doc);
                    sbAccessories.Append(tapsWriter.tapsWriter);
                }
                if (string.IsNullOrEmpty(element.LookupParameter(pd.PCF_ELEM_TAP3).AsString()) == false)
                {
                    TapsWriter tapsWriter = new TapsWriter(element, pd.PCF_ELEM_TAP3, doc);
                    sbAccessories.Append(tapsWriter.tapsWriter);
                }
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