using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;
//using MoreLinq;
using Autodesk.Revit.UI;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Plumbing;
using fi = Shared.Filter;
using op = Shared.Output;
using mp = Shared.MepUtils;
using Shared;
using MEPUtils.SharedStaging;
//using mySettings = GeneralStability.Properties.Settings;

namespace MEPUtils
{
    public class ConnectConnectors
    {
        public static void ConnectTheConnectors(ExternalCommandData commandData)
        {
            try
            {
                bool ctrl = false;
                bool shft = false;
                if ((int)Keyboard.Modifiers == 2) ctrl = true;
                if ((int)Keyboard.Modifiers == 4) shft = true;

                var app = commandData.Application;
                var uiDoc = app.ActiveUIDocument;
                var doc = uiDoc.Document;
                var selection = uiDoc.Selection.GetElementIds();

                //If no elements selected, connect ALL connectors to ALL connectors
                //Or if more than two -- connect to each other
                if ((selection.Count == 0 || selection.Count > 2) && !ctrl)
                {
                    //Argh! It seems Revit2019 doesn't break when connecting pipes at angle!!!
                    ////To filter out PCF_ELEM_EXCL set to true
                    ////Collecting pipes, fittings, accessories
                    ////Filtering out those with "true" value
                    ////The Guid below is for PCF_ELEM_EXCL
                    //var exclFilter = fi.ParameterValueGenericFilter(doc, 0, new Guid("CC8EC292-226C-4677-A32D-10B9736BFC1A"));

                    //FilteredElementCollector col1 = new FilteredElementCollector(doc);
                    //col1.WherePasses(
                    //        new LogicalOrFilter(
                    //            new List<ElementFilter>
                    //            {
                    //                new ElementCategoryFilter(BuiltInCategory.OST_PipeFitting),
                    //                new ElementCategoryFilter(BuiltInCategory.OST_PipeAccessory),
                    //                new ElementClassFilter(typeof (Pipe))
                    //            }));//.WherePasses(exclFilter);
                    //var col2 = mp.GetElementsOfBuiltInCategory(doc, BuiltInCategory.OST_MechanicalEquipment);

                    //HashSet<Element> elements = new HashSet<Element>();
                    //elements.UnionWith(col1);
                    //elements.UnionWith(col2);

                    //When selection is 0
                    IList<Connector> allConnectors;
                    if (selection.Count == 0)
                    {
                        allConnectors = mp.GetALLConnectorsInDocument(doc, true)
                            .Where(c => !c.IsConnected)
                            .ExceptWhere(c => c.MEPSystemAbbreviation(doc, true) == "ARGD")
                            .ToList();
                    }
                    //Selection is more than 2
                    else allConnectors = mp.GetALLConnectorsFromElements((from ElementId id in selection select doc.GetElement(id)).ToHashSet()).ToList();


                    //Employ reverse iteration to be able to modify the collection while iterating over it
                    for (int i = allConnectors.Count - 1; i > 0; i--)
                    {
                        if (allConnectors.Count < 2) break;
                        Connector c1 = allConnectors[i];
                        allConnectors.RemoveAt(i);
                        if (c1.IsConnected) continue; //Need: connectors connected in this loop are still in collection
                        Connector c2 = (from Connector c in allConnectors where c.Equalz(c1, Extensions._1mmTol) select c).FirstOrDefault();
                        try
                        {
                            if (c1?.Owner.Id == c2?.Owner.Id) continue;
                            c2?.ConnectTo(c1);
                        }
                        catch (Exception)
                        {
                            throw new Exception($"Element {c1.Owner.Id.ToString()} is already connected to element {c2.Owner.Id.ToString()}");
                        }
                    }

                    return;
                }

                else if (selection.Count == 1 && shft)
                {
                    ElementId hangerId = selection.First();
                    Element hanger = doc.GetElement(hangerId);
                    Cons cons = new Cons(hanger);

                    var allConnectors = mp.GetALLConnectorsInDocument(doc).ToList();

                    var query = allConnectors.Where(c => c.Equalz(cons.Primary, Extensions._1mmTol)).Where(c => c.Owner.Id.IntegerValue != hanger.Id.IntegerValue).ToList();

                    //Disconnect connectors of the existing components if the hanger was moved in place
                    Connector con1 = query.FirstOrDefault();
                    if (con1 == null) throw new Exception("Detection of existing con1 failed!");
                    Connector con2 = query.LastOrDefault();
                    if (con2 == null) throw new Exception("Detection of existing con2 failed!");

                    if (con1.IsConnectedTo(con2)) con1.DisconnectFrom(con2);

                    //If the hanger was created by placing on element and thus auto connected -> disconnect both connectors
                    //Dunno if this is needed and placing by auto connect does orient the connectors correctly
                    //Until it is proven true, both connectors are disconnected
                    if (cons.Primary.IsConnected)
                    {
                        var refCons = cons.Primary.AllRefs;
                        var refCon = MepUtils.GetAllConnectorsFromConnectorSet(refCons)
                            .Where(c => c.Owner.IsType<Pipe>() || c.Owner.IsType<FamilyInstance>()).FirstOrDefault();
                        if (refCon != null) cons.Primary.DisconnectFrom(refCon);
                    }
                    if (cons.Secondary.IsConnected)
                    {
                        var refCons = cons.Secondary.AllRefs;
                        var refCon = MepUtils.GetAllConnectorsFromConnectorSet(refCons)
                            .Where(c => c.Owner.IsType<Pipe>() || c.Owner.IsType<FamilyInstance>()).FirstOrDefault();
                        if (refCon != null) cons.Primary.DisconnectFrom(refCon);
                    }
                    doc.Regenerate();

                    //Start connecting hanger connectors
                    //https://stackoverflow.com/questions/7572640/how-do-i-know-if-two-vectors-are-near-parallel
                    var detectOpposite1 = query.Where(c => cons.Primary.CoordinateSystem.BasisZ.DotProduct(c.CoordinateSystem.BasisZ) < -1 + Extensions._epx);
                    Connector opposite1 = detectOpposite1.FirstOrDefault();
                    if (opposite1 == null) throw new Exception("Opposite primary detection failed!");
                    cons.Primary.ConnectTo(opposite1);

                    var detectOpposite2 = query.Where(c => cons.Secondary.CoordinateSystem.BasisZ.DotProduct(c.CoordinateSystem.BasisZ) < -1 + Extensions._epx);
                    Connector opposite2 = detectOpposite2.FirstOrDefault();
                    if (opposite2 == null) throw new Exception("Opposite secondary detection failed!");
                    cons.Secondary.ConnectTo(opposite2);

                    return;
                }

                else if (selection.Count == 1 && !ctrl) //If one and no CTRL key, connect the element
                {
                    var elements = new HashSet<Element>(from ElementId id in selection select doc.GetElement(id));
                    var elementConnectors = mp.GetALLConnectorsFromElements(elements);
                    var allConnectors = mp.GetALLConnectorsInDocument(doc, true).Where(c => !c.IsConnected).ToList();

                    IList<Connector> list1 = new List<Connector>();
                    IList<Connector> list2 = new List<Connector>();

                    foreach (var c1 in elementConnectors)
                    {
                        foreach (var c2 in allConnectors)
                        {
                            if (c1.Id != c2.Id && !c1.IsConnected && c1.Equalz(c2, Extensions._1mmTol))
                            {
                                list1.Add(c1);
                                list2.Add(c2);
                            }
                        }
                    }

                    if (list1.Count == 0 && list2.Count == 0) throw new Exception("No matches found! Check alignment!");

                    foreach (var (c1, c2) in list1.Zip(list2, (x, y) => (c1: x, c2: y)))
                    {
                        c1.ConnectTo(c2);
                    }

                    return;
                }

                else if ((selection.Count == 1 || selection.Count > 2) && ctrl) //If one and CTRL key is pressed, disconnect the element
                {
                    var elements = new HashSet<Element>(from ElementId id in selection select doc.GetElement(id));
                    var elementConnectors = mp.GetALLConnectorsFromElements(elements);

                    foreach (Connector c1 in elementConnectors)
                    {
                        if (c1.IsConnected)
                        {
                            var set = c1.AllRefs;
                            foreach (Connector c2 in set)
                            {
                                if (c1.IsConnectedTo(c2)) c1.DisconnectFrom(c2);
                            }
                        }
                    }

                    return;
                }

                //Connect or disconnect the connectors of selection
                //Only works on selection of two adjacent elements
                //That means only two connectors get connected to or disconnected from each other
                else if (selection.Count == 2)
                {
                    var elements = new HashSet<Element>(from ElementId id in selection select doc.GetElement(id));
                    var connectors = mp.GetALLConnectorsFromElements(elements).ToList();

                    for (int i = connectors.Count - 1; i > 0; i--)
                    {
                        if (connectors.Count < 2) throw new Exception("No eligible connectors found! Check alignment.");
                        Connector c1 = connectors[i];
                        connectors.RemoveAt(i);
                        Connector c2 = (from Connector c in connectors where c.Equalz(c1, Extensions._1mmTol) select c).FirstOrDefault();
                        if (c2 != null)
                        {
                            if (c1.Owner.Id == c2.Owner.Id) continue;
                            if (c1.IsConnected) c2.DisconnectFrom(c1);
                            else c2.ConnectTo(c1);
                        }
                    }

                    return;
                }
                else throw new Exception("Not correct amount of elements selected for the command! Choose none, one or two!");
            }
            catch (Exception e)
            {
                throw new Exception(e.Message);
            }
        }
    }
}

