using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Plumbing;
using Autodesk.Revit.DB.Structure;
using Autodesk.Revit.DB.Mechanical;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using MEPUtils.SharedStaging;
using Microsoft.WindowsAPICodePack.Dialogs;
using MoreLinq;
using Shared;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Windows.Input;
using dbg = Shared.Dbg;
using fi = Shared.Filter;
using lad = MEPUtils.CreateInstrumentation.ListsAndDicts;
using mp = Shared.MepUtils;
using tr = Shared.Transformation;

namespace MEPUtils.FlowCopy
{
    public class FlowCopy
    {
        public Result FlowCopyMethod(ExternalCommandData commandData)
        {
            UIApplication uiApp = commandData.Application;
            Document doc = commandData.Application.ActiveUIDocument.Document;
            UIDocument uidoc = uiApp.ActiveUIDocument;

            try
            {
                using (Transaction tx = new Transaction(doc))
                {
                    var allFittings = fi.GetElements<Element, BuiltInCategory>(doc, BuiltInCategory.OST_PipeFitting);

                    tx.Start("Copy flow data");
                    foreach (var fitting in allFittings)
                    {
                        var mf = ((FamilyInstance)fitting).MEPModel as MechanicalFitting;
                        Cons cons;
                        Parameter flow;

                        switch (mf.PartType)
                        {
                            case PartType.Elbow:
                                cons = mp.GetConnectors(fitting);
                                flow = fitting.LookupParameter("DRI.Mech.Flow");
                                if (flow == null) throw new Exception($"DRI.Mech.Flow parameter not found for elbow fitting {fitting.Id.IntegerValue}.");
                                flow.Set(cons.Primary.Flow);
                                break;
                            case PartType.Tee:
                                cons = mp.GetConnectors(fitting);
                                flow = fitting.LookupParameter("DRI.Mech.Flow");
                                if (flow == null) throw new Exception($"DRI.Mech.Flow parameter not found for tee fitting {fitting.Id.IntegerValue}.");
                                flow.Set(cons.Primary.Flow);

                                Parameter flowBranch = fitting.LookupParameter("DRI.Mech.FlowBranch");
                                if (flowBranch == null) throw new Exception($"DRI.Mech.FlowBranch parameter not found for tee fitting {fitting.Id.IntegerValue}.");
                                flowBranch.Set(cons.Tertiary.Flow);
                                break;
                            case PartType.Transition:
                                cons = mp.GetConnectors(fitting);
                                flow = fitting.LookupParameter("DRI.Mech.Flow");
                                if (flow == null) throw new Exception($"DRI.Mech.Flow parameter not found for transition fitting {fitting.Id.IntegerValue}.");
                                flow.Set(cons.Primary.Flow);
                                break;
                            case PartType.Cross:
                                break;
                            case PartType.Cap:
                                cons = mp.GetConnectors(fitting);
                                flow = fitting.LookupParameter("DRI.Mech.Flow");
                                if (flow == null) throw new Exception($"DRI.Mech.Flow parameter not found for cap type fitting {fitting.Id.IntegerValue}.");
                                flow.Set(cons.Primary.Flow);
                                break;
                            case PartType.TapAdjustable:
                                cons = mp.GetConnectors(fitting);
                                flow = fitting.LookupParameter("DRI.Mech.Flow");
                                if (flow == null) throw new Exception($"DRI.Mech.Flow parameter not found for tap adjustable fitting {fitting.Id.IntegerValue}.");
                                flow.Set(cons.Primary.Flow);
                                break;
                            case PartType.Union:
                                cons = mp.GetConnectors(fitting);
                                flow = fitting.LookupParameter("DRI.Mech.Flow");
                                if (flow == null) throw new Exception($"DRI.Mech.Flow parameter not found for union type fitting {fitting.Id.IntegerValue}.");
                                flow.Set(cons.Primary.Flow);
                                break;
                            case PartType.SpudAdjustable:
                                cons = mp.GetConnectors(fitting);
                                flow = fitting.LookupParameter("DRI.Mech.Flow");
                                if (flow == null) throw new Exception($"DRI.Mech.Flow parameter not found for spud adjustable type fitting {fitting.Id.IntegerValue}.");
                                flow.Set(cons.Primary.Flow);
                                break;
                            case PartType.EndCap:
                                cons = mp.GetConnectors(fitting);
                                flow = fitting.LookupParameter("DRI.Mech.Flow");
                                if (flow == null) throw new Exception($"DRI.Mech.Flow parameter not found for cap type fitting {fitting.Id.IntegerValue}.");
                                flow.Set(cons.Primary.Flow);
                                break;
                            default:
                                break;
                        }
                    }

                    var allAccessories = fi.GetElements<Element, BuiltInCategory>(doc, BuiltInCategory.OST_PipeAccessory);

                    foreach (var accessory in allAccessories)
                    {
                        Cons cons = mp.GetConnectors(accessory);
                        Parameter flow = accessory.LookupParameter("DRI.Mech.Flow");
                        if (flow == null) throw new Exception($"DRI.Mech.Flow parameter not found for accessory {accessory.Id.IntegerValue}.");
                        flow.Set(cons.Primary.Flow);
                    }

                    tx.Commit();
                    return Result.Succeeded; 
                }
                
            }

            catch (Autodesk.Revit.Exceptions.OperationCanceledException) { return Result.Cancelled; }

            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }
    }
}
