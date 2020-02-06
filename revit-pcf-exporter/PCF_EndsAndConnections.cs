using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Electrical;
using Autodesk.Revit.DB.Mechanical;
using Autodesk.Revit.DB.Plumbing;
using Autodesk.Revit.UI;
using MoreLinq;
using Shared;
using Shared.BuildingCoder;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using iv = PCF_Functions.InputVars;
using pdef = PCF_Functions.ParameterDefinition;
using plst = PCF_Functions.ParameterList;

namespace PCF_Pipeline
{
    public static class EndsAndConnections
    {
        public static StringBuilder DetectAndWriteEndsAndConnections(
            string key, HashSet<Element> pipes, HashSet<Element> fittings, HashSet<Element> accessories, Document doc)
        {
            StringBuilder sb = new StringBuilder();

            HashSet<Element> all = new HashSet<Element>(pipes);
            all.UnionWith(fittings);
            all.UnionWith(accessories);

            //Iterate over all elements and check their connected counterparts
            //If they satisfy certain conditions -> write end continuation property
            //Cases for connected connectors:
            //1) In different PipingSystem -> Pipeline continuation
            //1.1) If Selection and connector belongs to an element not in selection -> Pipeline continuation
            //2) Belongs to MechanicalEquipment -> Equipment continuation -> write tags
            //3) Free end -> Null connection

            foreach (Element elem in all)
            {
                HashSet<Connector> cons = new HashSet<Connector>();

                switch (elem)
                {
                    case Pipe pipe:
                        {
                            var consPipe = new Cons(elem);
                            cons.Add(consPipe.Primary);
                            cons.Add(consPipe.Secondary);
                        }
                        break;
                    case FamilyInstance fi:
                        {
                            cons = MepUtils.GetALLConnectorsFromElements(elem);
                        }
                        break;
                    default:
                        continue;
                }

                foreach (Connector con in cons)
                {
                    if (con.IsConnected)
                    {
                        var allRefsNotFiltered = MepUtils.GetAllConnectorsFromConnectorSet(con.AllRefs);
                        var correspondingCon = allRefsNotFiltered
                            .Where(x => x.Domain == Domain.DomainPiping)
                            .Where(x => x.Owner.Id.IntegerValue != elem.Id.IntegerValue).FirstOrDefault();
                        
                        //CASE: Free end -> Do nothing yet, for simplicity
                        if (correspondingCon != null) continue;

                        //CASE: If selection is exported, continuation for elements not in selection
                        //Even if same pipeline
                        if (iv.ExportSelection)
                        {
                            if (all.Any(x => !(x.Id.IntegerValue == correspondingCon.Owner.Id.IntegerValue)))
                            {
                                //CASE: Con belongs to MechanicalEquipment

                            }
                        }
                        
                        //CASE: If corrCon belongs to different Pipeline -> unconditional end
                        else if (correspondingCon.MEPSystemAbbreviation(doc) != key)
                        {

                        }

                    }
                }
            }

            return sb;
        }
    }
}