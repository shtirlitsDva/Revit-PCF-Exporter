using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Plumbing;
using Autodesk.Revit.DB.Structure;
using Autodesk.Revit.DB.Mechanical;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;

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
using Autodesk.Revit.Attributes;
using System.Diagnostics;
using log = Shared.SimpleLogger;

namespace MEPUtils.PipingSystemsAndFilters
{
    [Shared.DevReloadButton(Text = "Read link workset", Tooltip = "Read worksets from a linked model.", Group = "Piping Systems", Panel = "MEP", Order = 146, Icon16 = "ImgMUReadLinkWorkset16.png", Icon32 = "ImgMUReadLinkWorkset32.png")]
    [Transaction(TransactionMode.Manual)]
    public class ReadLinkWorkset : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIApplication uiApp = commandData.Application;
            Document doc = uiApp.ActiveUIDocument.Document;

            log.LogFileName = @"C:\Temp\DbgLog.txt";
            //log.clrLog();

            using (Transaction tr = new Transaction(doc, "Read link workset"))
            {
                tr.Start();
                try
                {
                    var wsTable = doc.GetWorksetTable();

                    using (FilteredElementCollector rvtLinks = new FilteredElementCollector(doc)
                        .OfCategory(BuiltInCategory.OST_RvtLinks).OfClass(typeof(RevitLinkType)))
                    {
                        if (rvtLinks.ToElements().Count > 0)
                        {
                            foreach (RevitLinkType rvtLink in rvtLinks.ToElements())
                            {
                                //if (rvtLink.GetLinkedFileStatus() == LinkedFileStatus.Loaded)
                                //{
                                    RevitLinkInstance link = 
                                        new FilteredElementCollector(doc)
                                        .OfCategory(BuiltInCategory.OST_RvtLinks)
                                        .OfClass(typeof(RevitLinkInstance))
                                        .Where(x => x.GetTypeId() == rvtLink.Id).First() as RevitLinkInstance;

                                    var ws = wsTable.GetWorkset(link.WorksetId);
                                log.log($"L:{link.Name.Split(':')[0].Trim()} WS:{ws.Name}");
                                //}
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex.ToString());
                    tr.RollBack();
                    throw;
                }
                tr.Commit();
            }

            return Result.Succeeded;
        }
    }
}