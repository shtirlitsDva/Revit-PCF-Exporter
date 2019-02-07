using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MoreLinq;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Plumbing;
using Autodesk.Revit.DB.Structure;
using Autodesk.Revit.DB.Mechanical;
using Autodesk.Revit.UI;
using Shared.BuildingCoder;
using Shared;

using static RevitMEPSystemTraversal.Enums;

namespace RevitMEPSystemTraversal
{
    public class MEPSystemTraversal
    {
        Document doc;
        public AnalyticModel Model;
        double _1mmTol;

        public MEPSystemTraversal(Document doc, HashSet<Element> elements)
        {
            Model = new AnalyticModel(elements);
            this.doc = doc;
            _1mmTol = 0.00328;
        }

        public void AnalyzeSystem()
        {
            //Start analysis
            var openEnds = detectOpenEnds(Model);

            Connector To = null;
            Connector From = null;
            Node FromNode = null;
            Node ToNode = null;
            Element curElem = null;
            AnalyticElement curAElem = null;
            AnalyticSequence curSequence = null;

            bool continueSequence = false;

            IList<Connector> branchEnds = new List<Connector>();

            for (int i = 0; i < Model.AllElements.Count; i++)
            {
                if (!continueSequence)
                {
                    if (branchEnds.Count > 0)
                    {
                        From = branchEnds.FirstOrDefault();
                        branchEnds = branchEnds.ExceptWhere(c => c.Equalz(From, _1mmTol)).ToList();
                        FromNode = (from Node n in Model.AllNodes
                                    where n.PreviousCon != null && n.PreviousCon.Equalz(From, _1mmTol)
                                    select n).FirstOrDefault();
                        FromNode.NextCon = From;
                    }
                    else
                    {
                        From = openEnds.FirstOrDefault();
                        openEnds = openEnds.ExceptWhere(c => c.Equalz(From, _1mmTol)).ToList();
                        FromNode = new Node();
                        FromNode.NextCon = From;
                        Model.AllNodes.Add(FromNode);



                        curSequence = new AnalyticSequence();
                    }

                    ToNode = new Node();

                    curElem = From.Owner;
                    curAElem = new AnalyticElement(curElem);

                    continueSequence = true;


                }

                switch (curElem)
                {
                    case Pipe pipe:

                        To = (from Connector c in pipe.ConnectorManager.Connectors //End of the host/dummy pipe
                              where c.Id != From.Id && (int)c.ConnectorType == 1
                              select c).FirstOrDefault();

                        //Test if the ToNode already exists
                        //TODO: This test must be copied to all other elements
                        Node existingToNode = (from Node n in Model.AllNodes
                                               where
                                               (n.PreviousCon != null && n.PreviousCon.Equalz(To, _1mmTol)) ||
                                               (n.NextCon != null && n.NextCon.Equalz(To, _1mmTol))
                                               select n).FirstOrDefault();

                        if (existingToNode != null)
                        {
                            ToNode = existingToNode;
                            if (ToNode.PreviousCon == null) ToNode.PreviousCon = To;
                            else if (ToNode.NextCon == null) ToNode.NextCon = To;
                        }
                        else
                        {
                            ToNode.PreviousCon = To;
                            Model.AllNodes.Add(ToNode);
                        }

                        curAElem.From = FromNode;
                        curAElem.To = ToNode;

                        //Assign correct element type to analytic element
                        curAElem.Type = ElemType.Pipe;

                        curSequence.Sequence.Add(curAElem);

                        break;
                    case FamilyInstance fi:
                        Cons cons = MepUtils.GetConnectors(fi);
                        int cat = fi.Category.Id.IntegerValue;
                        switch (cat)
                        {
                            case (int)BuiltInCategory.OST_PipeFitting:
                                var mf = fi.MEPModel as MechanicalFitting;
                                var partType = mf.PartType;
                                switch (partType)
                                {
                                    case PartType.Elbow:
                                        //First Analytic Element
                                        XYZ elbowLoc = ((LocationPoint)fi.Location).Point;
                                        ToNode.PreviousLoc = elbowLoc; //The node has only element Location point defining it -
                                        ToNode.NextLoc = elbowLoc; //and not two adjacent Connectors as element connection nodes
                                        ToNode.Type = ElemType.Elbow;
                                        Model.AllNodes.Add(ToNode);

                                        curAElem.From = FromNode;
                                        curAElem.To = ToNode;

                                        curAElem.Type = ElemType.Elbow;

                                        curAElem.AnalyzeBend();

                                        curSequence.Sequence.Add(curAElem);

                                        //Second Analytic Element
                                        //First determine the To connector
                                        To = (from Connector c in MepUtils.GetALLConnectorsFromElements(curElem)
                                              where c.Id != From.Id
                                              select c).FirstOrDefault();

                                        FromNode = ToNode; //Switch to next element
                                        ToNode = new Node();
                                        Model.AllNodes.Add(ToNode);
                                        ToNode.PreviousCon = To;
                                        curAElem = new AnalyticElement(curElem);
                                        curAElem.From = FromNode;
                                        curAElem.To = ToNode;

                                        curAElem.Type = ElemType.Pipe;

                                        curSequence.Sequence.Add(curAElem);
                                        break;
                                    case PartType.Tee:
                                        //Junction logic
                                        Node primNode = null;
                                        Node secNode = null;
                                        Node tertNode = null;

                                        //First analytic element
                                        XYZ teeLoc = ((LocationPoint)fi.Location).Point;
                                        ToNode.PreviousLoc = teeLoc; //The node has only element Location point defining it -
                                        ToNode.NextLoc = teeLoc; //and not two adjacent Connectors as element connection nodes
                                        ToNode.Type = ElemType.Tee;
                                        Model.AllNodes.Add(ToNode);

                                        curAElem.From = FromNode;
                                        curAElem.To = ToNode;

                                        curAElem.Type = ElemType.Tee;

                                        curSequence.Sequence.Add(curAElem);

                                        //Logic to return correct node to next element
                                        if (From.GetMEPConnectorInfo().IsPrimary) primNode = FromNode;
                                        else if (From.GetMEPConnectorInfo().IsSecondary) secNode = FromNode;
                                        else tertNode = FromNode;

                                        //From node is common for next two elements
                                        FromNode = ToNode;

                                        //Second Analytic Element
                                        if (From.GetMEPConnectorInfo().IsPrimary) To = cons.Secondary;
                                        else if (From.GetMEPConnectorInfo().IsSecondary) To = cons.Primary;
                                        else To = cons.Primary;

                                        ToNode = new Node();
                                        Model.AllNodes.Add(ToNode);
                                        ToNode.PreviousCon = To;
                                        curAElem = new AnalyticElement(curElem);
                                        curAElem.From = FromNode;
                                        curAElem.To = ToNode;

                                        curAElem.Type = ElemType.Pipe;

                                        curSequence.Sequence.Add(curAElem);

                                        //Logic to return correct node to next element
                                        if (From.GetMEPConnectorInfo().IsPrimary) secNode = ToNode;
                                        else if (From.GetMEPConnectorInfo().IsSecondary) primNode = ToNode;
                                        else primNode = ToNode;

                                        //Third Analytic Element
                                        if (From.GetMEPConnectorInfo().IsPrimary) To = cons.Tertiary;
                                        else if (From.GetMEPConnectorInfo().IsSecondary) To = cons.Tertiary;
                                        else To = cons.Secondary;

                                        ToNode = new Node();
                                        Model.AllNodes.Add(ToNode);
                                        ToNode.PreviousCon = To;
                                        curAElem = new AnalyticElement(curElem);
                                        curAElem.From = FromNode;
                                        curAElem.To = ToNode;

                                        curAElem.Type = ElemType.Pipe;

                                        curSequence.Sequence.Add(curAElem);

                                        //Logic to return correct node to next element
                                        if (From.GetMEPConnectorInfo().IsPrimary) tertNode = ToNode;
                                        else if (From.GetMEPConnectorInfo().IsSecondary) tertNode = ToNode;
                                        else secNode = ToNode;

                                        //Continuation logic
                                        Connector candidate1;
                                        Connector candidate2;

                                        if (From.GetMEPConnectorInfo().IsPrimary)
                                        {
                                            candidate1 = (from Connector c in Model.AllConnectors
                                                          where c.Equalz(cons.Secondary, _1mmTol)
                                                          select c).FirstOrDefault();
                                            candidate2 = (from Connector c in Model.AllConnectors
                                                          where c.Equalz(cons.Tertiary, _1mmTol)
                                                          select c).FirstOrDefault();

                                            if (candidate1 != null)
                                            {
                                                To = cons.Secondary;
                                                ToNode = secNode;
                                                if (candidate2 != null) branchEnds.Add(candidate2);
                                            }
                                            else if (candidate2 != null)
                                            {
                                                To = cons.Tertiary;
                                                ToNode = tertNode;
                                            }
                                        }
                                        else if (From.GetMEPConnectorInfo().IsSecondary)
                                        {
                                            candidate1 = (from Connector c in Model.AllConnectors
                                                          where c.Equalz(cons.Primary, _1mmTol)
                                                          select c).FirstOrDefault();
                                            candidate2 = (from Connector c in Model.AllConnectors
                                                          where c.Equalz(cons.Tertiary, _1mmTol)
                                                          select c).FirstOrDefault();

                                            if (candidate1 != null)
                                            {
                                                To = cons.Primary;
                                                ToNode = primNode;
                                                if (candidate2 != null) branchEnds.Add(candidate2);
                                            }
                                            else if (candidate2 != null)
                                            {
                                                To = cons.Tertiary;
                                                ToNode = tertNode;
                                            }
                                        }
                                        else
                                        {
                                            candidate1 = (from Connector c in Model.AllConnectors
                                                          where c.Equalz(cons.Primary, _1mmTol)
                                                          select c).FirstOrDefault();
                                            candidate2 = (from Connector c in Model.AllConnectors
                                                          where c.Equalz(cons.Secondary, _1mmTol)
                                                          select c).FirstOrDefault();

                                            if (candidate1 != null)
                                            {
                                                To = cons.Primary;
                                                ToNode = primNode;
                                                if (candidate2 != null) branchEnds.Add(candidate2);
                                            }
                                            else if (candidate2 != null)
                                            {
                                                To = cons.Secondary;
                                                ToNode = secNode;
                                            }
                                        }
                                        break;
                                    case PartType.Transition:
                                        To = (from Connector c in MepUtils.GetALLConnectorsFromElements(curElem)
                                              where c.Id != From.Id
                                              select c).FirstOrDefault();
                                        ToNode.PreviousCon = To;
                                        Model.AllNodes.Add(ToNode);

                                        curAElem.From = FromNode;
                                        curAElem.To = ToNode;

                                        //Assign correct element type to analytic element
                                        curAElem.Type = ElemType.Transition;

                                        //Determine start dia and second dia
                                        curAElem.AnalyzeReducer();

                                        curSequence.Sequence.Add(curAElem);
                                        break;
                                    case PartType.Cap:
                                        //Handles flanges because of the workaround PartType.Cap for flanges
                                        //Real Caps are ignored for now
                                        To = (from Connector c in MepUtils.GetALLConnectorsFromElements(curElem)
                                              where c.Id != From.Id
                                              select c).FirstOrDefault();
                                        ToNode.PreviousCon = To;
                                        Model.AllNodes.Add(ToNode);

                                        curAElem.From = FromNode;
                                        curAElem.To = ToNode;

                                        //Assign correct element type to analytic element
                                        curAElem.Type = ElemType.Rigid;

                                        curSequence.Sequence.Add(curAElem);
                                        break;
                                    case PartType.Union:
                                        throw new NotImplementedException();
                                    case PartType.SpudAdjustable:
                                        throw new NotImplementedException();
                                    default:
                                        continue;
                                }
                                break;
                            case (int)BuiltInCategory.OST_PipeAccessory:
                                if (From.GetMEPConnectorInfo().IsPrimary) To = cons.Secondary;
                                else if (From.GetMEPConnectorInfo().IsSecondary) To = cons.Primary;
                                else throw new Exception("Something went wrong with connectors of element " + curElem.Id.ToString());

                                ToNode.PreviousCon = To;
                                Model.AllNodes.Add(ToNode);

                                curAElem.From = FromNode;
                                curAElem.To = ToNode;

                                //Assign correct element type to analytic element
                                curAElem.Type = ElemType.Rigid;

                                curSequence.Sequence.Add(curAElem);
                                break;
                            default:
                                continue;
                        }
                        break;
                    default:
                        continue;
                }

                //Prepare to restart iteration
                Model.AllConnectors = Model.AllConnectors.ExceptWhere(c => c.Owner.Id.IntegerValue == curElem.Id.IntegerValue).ToList();

                From = (from Connector c in Model.AllConnectors
                        where c.Equalz(To, _1mmTol)
                        select c).FirstOrDefault();

                if (From != null)
                {
                    curElem = From.Owner;

                    FromNode = ToNode;
                    FromNode.NextCon = From;

                    ToNode = new Node();

                    curAElem = new AnalyticElement(curElem);
                }
                else
                {
                    continueSequence = false;
                    openEnds = openEnds.ExceptWhere(c => c.Equalz(To, _1mmTol)).ToList();

                    if (branchEnds.Count > 0 && To != null)
                    {
                        branchEnds = branchEnds.ExceptWhere(c => c.Equalz(To, _1mmTol)).ToList();
                    }

                    if (branchEnds.Count < 1) Model.Sequences.Add(curSequence);

                }
            }

            //Reference ALL analytic elements in the collecting pool
            foreach (var sequence in Model.Sequences)
            {
                foreach (var item in sequence.Sequence)
                {
                    Model.AllAnalyticElements.Add(item);
                }
            }

            //Loop over ALL nodes and populate coordinate information
            foreach (Node n in Model.AllNodes) n.PopulateCoordinates();

           BuildingCoderUtilities.InfoMsg(Model.AllNodes.Count.ToString());
        }

        public void NumberNodes()
        {
            int thCount = 0;
            int thousands = 0;

            foreach (AnalyticSequence ans in Model.Sequences)
            {
                int tens = 0;
                int tensCount = 0;

                foreach (AnalyticElement ae in ans.Sequence)
                {
                    if (ae.To.Number == 0) tensCount++; //Possible fix for skipping a number after branches.
                    if (tensCount == 1)
                    {
                        if (ae.From.Number == 0) ae.From.Number = thousands + 10;
                        if (ae.To.Number == 0) ae.To.Number = thousands + 20;
                        tensCount = 2;
                        continue;
                    }
                    tens = tensCount * 10;
                    if (ae.To.Number == 0) ae.To.Number = thousands + tens;
                }
                thCount++;
                thousands = thCount * 1000;
            }
        }

        public void PlaceTextNotesAtNodes()
        {
            using (Transaction t = new Transaction(doc))
            {
                t.Start("Create TextNotes");
                foreach (Node node in Model.AllNodes)
                {
                    XYZ location = null;
                    if (node.NextCon != null) location = node.NextCon.Origin;
                    else if (node.NextLoc != null) location = node.NextLoc;
                    else if (node.PreviousCon != null) location = node.PreviousCon.Origin;
                    else if (node.PreviousLoc != null) location = node.PreviousLoc;

                    TextNote.Create(doc, doc.ActiveView.Id, location, node.Number.ToString(), new ElementId(361));

                }
                t.Commit();
            }
        }

        internal static List<Connector> detectOpenEnds(AnalyticModel Model)
        {
            List<Connector> singleConnectors = new List<Connector>();
            foreach (Connector c1 in Model.AllConnectors) if (!(1 < Model.AllConnectors.Count(c => c.Equalz(c1, Shared.Extensions._1mmTol)))) singleConnectors.Add(c1);
            return singleConnectors;
        }
    }

    public class Node
    {
        public Connector PreviousCon { get; set; } = null;
        public Connector NextCon { get; set; } = null;
        public XYZ PreviousLoc { get; set; } = null;
        public XYZ NextLoc { get; set; } = null;
        public int Number { get; set; } = 0;
        public ElemType Type { get; set; } = 0;
        public double X { get; set; }
        public double Y { get; set; }
        public double Z { get; set; }

        public void PopulateCoordinates()
        {
            XYZ location = null;
            if (NextCon != null) location = NextCon.Origin;
            else if (NextLoc != null) location = NextLoc;
            else if (PreviousCon != null) location = PreviousCon.Origin;
            else if (PreviousLoc != null) location = PreviousLoc;
            else throw new Exception($"Node number {Number} has no Connector or XYZ assigned!");

            X = location.X.FtToMm();
            Y = location.Y.FtToMm();
            Z = location.Z.FtToMm();
        }
    }

    public class AnalyticElement
    {
        public Node From { get; set; } = null;
        public Node To { get; set; } = null;
        public Element Element { get; set; } = null;
        public ElemType Type { get; set; } = 0;
        public int DN { get; } = 0;
        public double oDia { get; set; } = 0;
        public double secondODia { get; set; } = 0;
        public double WallThk { get; set; } = 0;
        public double secondWallThk { get; set; } = 0;
        public int InsulationThk { get; } = 0;
        public double BendRadius { get; set; } = 0;

        public AnalyticElement(Element element)
        {
            Element = element;

            Parameter parInsTypeCheck = Element.get_Parameter(BuiltInParameter.RBS_REFERENCE_INSULATION_TYPE);
            if (parInsTypeCheck.HasValue)
            {
                Parameter parInsThickness = Element.get_Parameter(BuiltInParameter.RBS_REFERENCE_INSULATION_THICKNESS);
                InsulationThk = (int)parInsThickness.AsDouble().FtToMm().Round();
            }

            switch (Element)
            {
                case Pipe pipe:
                    //Outside diameter
                    oDia = Element.get_Parameter(BuiltInParameter.RBS_PIPE_OUTER_DIAMETER).AsDouble().FtToMm();
                    //Wallthk
                    DN = (int)pipe.Diameter.FtToMm().Round();
                    WallThk = MepUtils.pipeWallThkDict()[DN];
                    break;
                case FamilyInstance fi:
                    //Outside diameter
                    Cons cons = MepUtils.GetConnectors(fi);
                    DN = (int)(cons.Primary.Radius * 2).FtToMm().Round();
                    oDia = MepUtils.outerDiaDict()[DN];
                    WallThk = MepUtils.pipeWallThkDict()[DN];
                    break;
                default:
                    break;
            }

        }
        public void AnalyzeBend()
        {
            Cons cons = MepUtils.GetConnectors(Element);
            XYZ P = cons.Primary.Origin;
            XYZ Q = cons.Secondary.Origin;
            double a = P.DistanceTo(Q) / 2;

            double angle = Element.LookupParameter("Angle").AsDouble();
            double A = angle / 2;

            BendRadius = (a / (Math.Sin(A))).FtToMm();
        }
        public void AnalyzeReducer()
        {
            int fromDN = (int)(From.NextCon.Radius * 2).FtToMm().Round();
            oDia = MepUtils.outerDiaDict()[fromDN];
            WallThk = MepUtils.pipeWallThkDict()[fromDN];

            int toDN = (int)(To.PreviousCon.Radius * 2).FtToMm().Round();
            secondODia = MepUtils.outerDiaDict()[toDN];
            secondWallThk = MepUtils.pipeWallThkDict()[toDN];
        }
    }

    public class AnalyticSequence
    {
        public List<AnalyticElement> Sequence { get; set; } = new List<AnalyticElement>();
    }

    public class AnalyticModel
    {
        public List<AnalyticSequence> Sequences { get; } = new List<AnalyticSequence>();
        public List<Node> AllNodes { get; } = new List<Node>();
        public List<AnalyticElement> AllAnalyticElements { get; } = new List<AnalyticElement>();
        public List<Connector> AllConnectors { get; set; }
        public List<Element> AllElements { get; }
        public int Counter_Bends { get; set; } = 0;
        public int Counter_Reducers { get; set; } = 0;
        public int Counter_Intersection { get; set; } = 0;

        public AnalyticModel(HashSet<Element> elements)
        {
            AllElements = elements.ToList();
            AllConnectors = MepUtils.GetALLConnectorsFromElements(elements).ToList();
        }
    }

    public static class Enums
    {
        public enum ElemType
        {
            NotAssigned = 0,
            Pipe = 1,
            Elbow = 2,
            Tee = 3,
            Transition = 4,
            Flange = 5,
            Rigid = 6
        }
    }

    public static class Debugger
    {
        public static void PlaceMarker(Document doc, XYZ loc)
        {
            FilteredElementCollector collector = new FilteredElementCollector(doc);
            var family = collector.OfClass(typeof(Family)).Where(e => e.Name == "Marker").Cast<Family>().FirstOrDefault();
            if (family == null) throw new Exception("No Marker family in project!");
            var famSymbolId = family.GetFamilySymbolIds().FirstOrDefault();
            if (famSymbolId == null) throw new Exception("Getting Marker familySymbol ID failed!");
            var famSymbol = (FamilySymbol)doc.GetElement(famSymbolId);
            if (famSymbol == null) throw new Exception("Getting Marker familySymbol failed!");



            using (Transaction trans = new Transaction(doc, "Place markers!"))
            {
                trans.Start();

                //The strange symbol activation thingie...
                //See: http://thebuildingcoder.typepad.com/blog/2014/08/activate-your-family-symbol-before-using-it.html
                if (!famSymbol.IsActive)
                {
                    famSymbol.Activate();
                    doc.Regenerate();
                }

                doc.Create.NewFamilyInstance(loc, famSymbol, StructuralType.NonStructural);

                trans.Commit();
            }
        }
    }
}
