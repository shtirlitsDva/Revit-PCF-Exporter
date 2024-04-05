using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using NLog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows.Forms;
using static Shared.Filter;
using WinForms = System.Windows.Forms;

namespace ModelessForms
{
    public interface IAsyncCommand
    {
        void Execute(UIApplication uiApp);
    }

    class AsyncFindOldElement : IAsyncCommand
    {
        private DataGridView Dgw { get; set; }
        private AsyncFindOldElement() { }
        public AsyncFindOldElement(DataGridView dgw)
        {
            Dgw = dgw;
        }

        private static readonly NLog.Logger log = NLog.LogManager.GetCurrentClassLogger();

        public void Execute(UIApplication uiApp)
        {
            #region LoggerSetup
            //Nlog configuration
            var nlogConfig = new NLog.Config.LoggingConfiguration();
            //Targets
            var logfile = new NLog.Targets.FileTarget("logfile") { FileName = "X:\\GitHub\\log.txt", DeleteOldFileOnStartup = false, Layout = "${message}" };
            //Rules
            nlogConfig.AddRule(LogLevel.Info, LogLevel.Fatal, logfile);
            //Apply config
            NLog.LogManager.Configuration = nlogConfig;
            //DISABLE LOGGING
            //NLog.LogManager.DisableLogging();
            #endregion

            Document doc = uiApp.ActiveUIDocument.Document;
            UIDocument uidoc = uiApp.ActiveUIDocument;

            Selection selection = uidoc.Selection;

            //I cannot find a way to get to a shared parameter element
            //whithout GUID so I must assume I am working with
            //Pipe Accessories and I use a first element as donor
            FilteredElementCollector donorFec = new FilteredElementCollector(doc);
            Element paDonor = donorFec.OfCategory(BuiltInCategory.OST_PipeAccessory).OfClass(typeof(FamilyInstance)).FirstElement();
            if (paDonor == null) { log.Info("Failed to get donor element! -> NULL"); }
            log.Info($"Donor element collected {paDonor.Id.ToString()}, {paDonor.Name}");
            FilteredElementCollector col = new FilteredElementCollector(doc);
            col = col.OfCategory(BuiltInCategory.OST_PipeAccessory).OfClass(typeof(FamilyInstance));
            log.Info($"Collected all Pipe Accessories: Count = {col.Count()}.");

            //Hardcoded! Select columns with tagnames preceeded with "Old#" and find elements only with those values

            int i = 0;
            foreach (DataGridViewColumn column in Dgw.Columns)
            {
                log.Info($"Iteration {i}:");
                //Test to see if there's a name of parameter specified
                var parNameValue = Dgw.Rows[1].Cells[i].Value;
                if (parNameValue == null) { i++; log.Info($"parNameValue -> NULL -> skipping"); continue; }
                string parName = parNameValue.ToString();
                if (string.IsNullOrEmpty(parName)) { i++; log.Info($"parName -> NULL or Empty -> skipping"); continue; }
                log.Info($"parName -> {parName}");
                if (!parName.Contains("Old#")) { i++; log.Info($"Not old parameter name! -> Skip"); continue; }

                //Remove the "Old#" prefix
                parName = parName.Replace("Old#", "");

                Parameter parToTest = paDonor.LookupParameter(parName);
                if (parToTest == null) { i++; log.Info($"Failed to get parToTest -> NULL. CRITICAL!"); continue; }
                log.Info($"parToTest acquired with GUID: {parToTest.GUID}.");

                //Retrieve value to filter against
                var parValue = Dgw.Rows[0].Cells[i].Value;
                if (parValue == null) { i++; log.Info($"parValue -> NULL -> skipping"); continue; }
                string parValueString = parValue.ToString();
                if (parValueString == null) { i++; log.Info($"parValueString -> NULL -> skipping"); continue; }
                log.Info($"Parameter value acquired: {parValueString}");

                ElementParameterFilter epf = ParameterValueGenericFilter(doc, parValueString, parToTest.GUID);
                col = col.WherePasses(epf);
                log.Info($"Collector filtered to number of elements: {col.Count()}");
                i++;
            }

            log.Info($"After last iteration collector contains elements: {col.Count()}");
            foreach (var id in col.ToElementIds())
            {
                log.Info($"{id.IntegerValue}");
            }
            uidoc.Selection.SetElementIds(col.ToElementIds());
        }
    }

    class AsyncFindSelectElement : IAsyncCommand
    {
        private DataGridView Dgw { get; set; }
        private AsyncFindSelectElement() { }
        public AsyncFindSelectElement(DataGridView dgw)
        {
            Dgw = dgw;
        }

        private static readonly NLog.Logger log = NLog.LogManager.GetCurrentClassLogger();

        public void Execute(UIApplication uiApp)
        {
            #region LoggerSetup
            //Nlog configuration
            var nlogConfig = new NLog.Config.LoggingConfiguration();
            //Targets
            var logfile = new NLog.Targets.FileTarget("logfile") { FileName = "X:\\GitHub\\log.txt", DeleteOldFileOnStartup = true };
            //Rules
            nlogConfig.AddRule(LogLevel.Info, LogLevel.Fatal, logfile);
            //Apply config
            NLog.LogManager.Configuration = nlogConfig;
            //DISABLE LOGGING
            NLog.LogManager.DisableLogging();
            #endregion
            
            Document doc = uiApp.ActiveUIDocument.Document;
            UIDocument uidoc = uiApp.ActiveUIDocument;

            Selection selection = uidoc.Selection;

            //I cannot find a way to get to a shared parameter element
            //whithout GUID so I must assume I am working with
            //Pipe Accessories and I use a first element as donor
            FilteredElementCollector donorFec = new FilteredElementCollector(doc);
            Element paDonor = donorFec.OfCategory(BuiltInCategory.OST_PipeAccessory).OfClass(typeof(FamilyInstance)).FirstElement();
            if (paDonor == null) { log.Info("Failed to get donor element! -> NULL"); }
            log.Info($"Donor element collected {paDonor.Id.ToString()}, {paDonor.Name}");
            FilteredElementCollector col = new FilteredElementCollector(doc);
            col = col.OfCategory(BuiltInCategory.OST_PipeAccessory).OfClass(typeof(FamilyInstance));
            log.Info($"Collected all Pipe Accessories: Count = {col.Count()}.");

            //Iterate over each column of dgw, only acting on filled out cells for parameter names
            //then filter collector by a elementparameterfilter
            var dataList = new List<(ElementParameterFilter epf, bool match)>();
            int i = 0;
            foreach (DataGridViewColumn column in Dgw.Columns)
            {
                log.Info($"Iteration {i}:");
                //Test to see if there's a name of parameter specified
                var parNameValue = Dgw.Rows[1].Cells[i].Value;
                if (parNameValue == null) { i++; log.Info($"parNameValue -> NULL -> skipping"); continue; }
                string parName = parNameValue.ToString();
                if (string.IsNullOrEmpty(parName)) { i++; log.Info($"parName -> NULL or Empty -> skipping"); continue; }
                log.Info($"parName -> {parName}");

                //Skip OLD tags
                if (parName.Contains("Old#")) { i++; log.Info($"Old parameterName found -> Skip"); continue; }

                Parameter parToTest = paDonor.LookupParameter(parName);
                if (parToTest == null) { i++; log.Info($"Failed to get parToTest -> NULL. CRITICAL!"); continue; }
                log.Info($"parToTest acquired with GUID: {parToTest.GUID}.");

                //Retrieve value to filter against
                var parValue = Dgw.Rows[0].Cells[i].Value;
                if (parValue == null) { i++; log.Info($"parValue -> NULL -> skipping"); continue; }
                string parValueString = parValue.ToString();
                //if (string.IsNullOrEmpty(parValueString)) { i++; log.Info($"parValueString -> NULL or Empty -> skipping"); continue; }
                log.Info($"Parameter value acquired: {parValueString}");

                ElementParameterFilter epf = ParameterValueGenericFilter(doc, parValueString, parToTest.GUID);
                col = col.WherePasses(epf);
                log.Info($"Collector filtered to number of elements: {col.Count()}");
                i++;
            }

            log.Info($"After last iteration collector contains elements: {col.Count()}");
            foreach (var id in col.ToElementIds())
            {
                log.Info($"{id.IntegerValue}");
            }
            uidoc.Selection.SetElementIds(col.ToElementIds());
        }
    }

    class AsyncUpdateParameterValues : IAsyncCommand
    {
        private DataGridView Dgw { get; set; }

        private AsyncUpdateParameterValues() { }

        public AsyncUpdateParameterValues(DataGridView dgw)
        {
            Dgw = dgw;
        }

        public void Execute(UIApplication uiApp)
        {
            Document doc = uiApp.ActiveUIDocument.Document;
            UIDocument uidoc = uiApp.ActiveUIDocument;

            Selection selection = uidoc.Selection;
            var selIds = selection.GetElementIds();

            if (selIds.Count > 1)
            {
                ErrorMsg("More than one element selected! Please select only one element.");
                return;
            }
            if (selIds.Count < 1)
            {
                ErrorMsg("No element selected! Please select only one element.");
                return;
            }

            ElementId elId = selIds.FirstOrDefault();

            using (Transaction tx = new Transaction(doc))
            {
                tx.Start("Update parameter values");

                int i = 0;
                foreach (DataGridViewColumn column in Dgw.Columns)
                {
                    //Test to see if there's a name of parameter specified
                    var parNameValue = Dgw.Rows[1].Cells[i].Value;

                    if (parNameValue == null) { i++; continue; }

                    string parName = parNameValue.ToString();

                    if (string.IsNullOrEmpty(parName)) { i++; continue; }

                    //Skip OLD tags
                    if (parName.Contains("Old#")) { i++; continue; }

                    Element el = doc.GetElement(elId);

                    Parameter parToSet = el.LookupParameter(parName);
                    if (parToSet == null) throw new Exception($"Parameter name {parName} does not exist for element {el.Id.ToString()}!");

                    var parValue = Dgw.Rows[0].Cells[i].Value;

                    if (parValue == null) { i++; continue; }

                    parToSet.Set(parValue.ToString());

                    i++;
                }
                tx.Commit();
            }
        }

        public static void ErrorMsg(string msg)
        {
            Debug.WriteLine(msg);
            WinForms.MessageBox.Show(msg,
              "Error",
              WinForms.MessageBoxButtons.OK,
              WinForms.MessageBoxIcon.Error);
        }
    }
}
