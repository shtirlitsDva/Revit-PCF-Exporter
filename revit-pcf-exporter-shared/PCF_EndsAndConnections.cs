using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Electrical;
using Autodesk.Revit.DB.Mechanical;
using Autodesk.Revit.DB.Plumbing;
using Autodesk.Revit.UI;
using MoreLinq;
using PCF_Model;

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
using plst = PCF_Functions.Parameters;

namespace PCF_Pipeline
{
    public static class EndsAndConnections
    {
        internal static StringBuilder DetectAndWriteEndsAndConnections(
            string key, KeyValuePair<string, HashSet<IPcfElement>> gp, Document doc)
        {
            StringBuilder sb = new StringBuilder();

            //Iterate over all elements and check their connected counterparts
            //If they satisfy certain conditions -> write end continuation property
            //Cases for connected connectors:
            //1) In different PipingSystem -> Pipeline continuation
            //1.1) If Selection and connector belongs to an element not in selection -> Pipeline continuation
            //2) Belongs to MechanicalEquipment -> Equipment continuation -> write tags
            //3) Free end -> Null connection

            foreach (IPcfElement elem in gp.Value)
            {
                HashSet<Connector> cons = elem.AllConectors;

                foreach (Connector con in cons)
                {
                    //This if should also filter out free ends...
                    if (con.IsConnected)
                    {
                        var allRefsNotFiltered = MepUtils.GetAllConnectorsFromConnectorSet(con.AllRefs);
                        var correspondingCon = allRefsNotFiltered
                            .Where(x => x.Domain == Domain.DomainPiping).FirstOrDefault();
                            //.Where(x => x.Owner.Id.IntegerValue != elem.Id.IntegerValue).FirstOrDefault(); ???

                        //CASE: Free end -> Do nothing yet, for simplicity
                        //This also catches empty cons on multicons accessories
                        //Example: pressure take outs on filters.
                        if (correspondingCon == null) continue;

                        //CASE: If selection is exported, continuation for elements not in selection
                        //Even if same pipeline
                        if (iv.ExportSelection)
                        {
                            throw new NotImplementedException(
                                "Continuation (END Messages) are not implemented for Export Selection!");
                            //bool inElementsList = !all.Any(x => x.Id.IntegerValue == correspondingCon.Owner.Id.IntegerValue);
                            ////bool inDiscardedPipes = !discardedPipes.Any(x => x.Id.IntegerValue == correspondingCon.Owner.Id.IntegerValue);

                            //if (inElementsList)// && inDiscardedPipes)
                            //{
                            //    //CASE: Con belongs to MechanicalEquipment
                            //    if (correspondingCon.Owner.Category.Id.IntegerValue == (int)BuiltInCategory.OST_MechanicalEquipment)
                            //    {
                            //        sb.AppendLine("END-CONNECTION-EQUIPMENT");
                            //        sb.Append(PCF_Functions.EndWriter.WriteCO(correspondingCon.Origin));
                            //        sb.Append(PCF_Functions.ParameterDataWriter
                            //            .ParameterValue("CONNECTION-REFERENCE", new[] { "TAG 1", "TAG 2", "TAG 3", "TAG 4" }, correspondingCon.Owner));

                            //        continue;
                            //    }
                            //    //CASE: Any other component
                            //    else
                            //    {
                            //        sb.AppendLine("END-CONNECTION-PIPELINE");
                            //        sb.Append(PCF_Functions.EndWriter.WriteCO(correspondingCon.Origin));
                            //        sb.AppendLine("    PIPELINE-REFERENCE " + correspondingCon.MEPSystemAbbreviation(doc));

                            //        continue;
                            //    }
                            //}
                            ////CASE: None of the above hit -> continue with loop execution
                            ////To prevent from falling through to non selection cases.
                            //continue;
                        }

                        //CASE: Con belongs to MechanicalEquipment
                        else if (correspondingCon.Owner.Category.Id.IntegerValue == (int)BuiltInCategory.OST_MechanicalEquipment)
                        {
                            sb.AppendLine("END-CONNECTION-EQUIPMENT");
                            sb.Append(PCF_Functions.EndWriter.WriteCO(correspondingCon.Origin));
                            sb.Append(PCF_Functions.ParameterDataWriter
                                .ParameterValue("CONNECTION-REFERENCE", new[] { "TAG 1", "TAG 2", "TAG 3", "TAG 4" }, correspondingCon.Owner));

                            continue;
                        }
                        //CASE: If corrCon belongs to different Pipeline -> unconditional end
                        //MechanicalEquipment cons should belong to the same Piping System, else...
                        else if (correspondingCon.MEPSystemAbbreviation(doc) != key)
                        {
                            sb.AppendLine("END-CONNECTION-PIPELINE");
                            sb.Append(PCF_Functions.EndWriter.WriteCO(correspondingCon.Origin));
                            sb.AppendLine("    PIPELINE-REFERENCE " + correspondingCon.MEPSystemAbbreviation(doc));

                            continue;
                        }
                        //CASE: If corrCon belongs to EXISTING component
                        //Write PIPELINE-REFERENCE -> EXISTING
                        Element hostElement = correspondingCon.Owner;
                        //GUID is for PCF_ELEM_SPEC
                        Parameter existingParameter = hostElement.get_Parameter(new Guid("90be8246-25f7-487d-b352-554f810fcaa7"));
                        if (existingParameter.AsString() == "EXISTING")
                        {
                            sb.AppendLine("END-CONNECTION-PIPELINE");
                            sb.Append(PCF_Functions.EndWriter.WriteCO(correspondingCon.Origin));
                            sb.AppendLine("    PIPELINE-REFERENCE EKSISTERENDE");
                        }
                    }
                }
            }

            return sb;
        }
    }
}