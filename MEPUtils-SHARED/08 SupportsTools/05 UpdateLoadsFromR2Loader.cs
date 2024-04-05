using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using MoreLinq;
using Autodesk.Revit.UI;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Plumbing;
using Autodesk.Revit.DB.Structure.StructuralSections;
using static Autodesk.Revit.DB.UnitTypeId;
using Microsoft.WindowsAPICodePack.Dialogs;
using Shared;
using NLog;
using fi = Shared.Filter;
using op = Shared.Output;
using tr = Shared.Transformation;
using mp = Shared.MepUtils;

namespace MEPUtils.SupportTools
{
    public class UpdateLoadsFromR2Loader
    {
        private static readonly NLog.Logger log = NLog.LogManager.GetCurrentClassLogger();

        public static void Update(UIApplication uiApp)
        {
            Document doc = uiApp.ActiveUIDocument.Document;
            UIDocument uidoc = uiApp.ActiveUIDocument;

            LogManager.Configuration = new NLog.Config.XmlLoggingConfiguration(@"X:\AutoCAD DRI - Revit\Addins\NLog\NLog.config");

            try
            {
                using (TransactionGroup txGp = new TransactionGroup(doc))
                {
                    txGp.Start("Update support loads.");

                    SupportUpdater su = new SupportUpdater(uiApp);
                    su.ShowDialog();
                    su.Close();

                    //log.Debug("Test!");
                    txGp.Assimilate();
                }
            }

            catch (Exception ex)
            {
                throw new Exception(ex.Message);
                //return Result.Failed;
            }
        }

    }
}
