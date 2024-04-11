using System;
using System.Collections.Generic;
using System.Linq;
//using MoreLinq;
using System.Data;
using System.IO;
using Autodesk.Revit.UI;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Plumbing;
using Autodesk.Revit.DB.Structure;
using Autodesk.Revit.DB.Mechanical;
using Shared;
using fi = Shared.Filter;
using op = Shared.Output;
using tr = Shared.Transformation;
using mp = Shared.MepUtils;
using dh = Shared.DataHandler;
using System.Diagnostics;
using Shared.BuildingCoder;

namespace MEPUtils.InsulationHandler
{
    public class InsulationHandler
    {
        /// <summary>
        /// This method is used to set and save settings for insulation creation for Pipe Accessories (valves etc.)
        /// </summary>
        public Result ExecuteInsulationSettings(UIApplication uiApp)
        {
            InsulationSettingsWindow isw = new InsulationSettingsWindow(uiApp);
            isw.ShowDialog();
            isw.Close();
            using (Stream stream = new FileStream(isw.PathToSettingsXml, FileMode.Create, FileAccess.Write))
            {
                isw.Settings.WriteXml(stream);
            }

            return Result.Succeeded;
        }
        public static Result CreateAllInsulation(UIApplication uiApp)
        {
            Document doc = uiApp.ActiveUIDocument.Document;

            //Collect all the elements to insulate
            var pipes = fi.GetElements<Element, BuiltInCategory>(doc, BuiltInCategory.OST_PipeCurves);
            var fittings = fi.GetElements<Element, BuiltInCategory>(doc, BuiltInCategory.OST_PipeFitting);
            var accessories = fi.GetElements<Element, BuiltInCategory>(doc, BuiltInCategory.OST_PipeAccessory);

            //Filter out grouped items
            pipes = pipes.Where(e => e.GroupId.IntegerValue == -1).ToHashSet();
            fittings = fittings.Where(e => e.GroupId.IntegerValue == -1).ToHashSet();
            accessories = accessories.Where(e => e.GroupId.IntegerValue == -1).ToHashSet();

            //Get the settings
            Settings.InsulationParameters = GetInsulationParameters();
            Settings.InsulationSettings = GetInsulationSettings(doc);

            //Create the wrapper objects
            HashSet<IW> iws = new HashSet<IW>();
            foreach (Element e in pipes) iws.Add(IWFactory.CreateIW(e));

            Debug.WriteLine($"Number of all elements P/PA/PF unfiltered: {pipes.Count}");

            #region Filter out nondefined SysAbbrs
            //Filter out items with where sysAbbr is not defined in the settings file
            HashSet<string> nonDefinedSysAbbrs = new HashSet<string>();
            int withoutSysAbbr = 0;

            HashSet<IW> toRemove = new HashSet<IW>();
            foreach (var iw in iws)
            {
                if (iw.sysAbbr.IsNoE()) { withoutSysAbbr++; toRemove.Add(iw); continue; }
                if (!Settings.InsulationParameters.AsEnumerable().Any(row => row.Field<string>("System") == iw.sysAbbr))
                {
                    nonDefinedSysAbbrs.Add(iw.sysAbbr);
                    toRemove.Add(iw);
                }
            }
            iws.ExceptWith(toRemove);
            #endregion

            Debug.WriteLine($"Number of Elements without SysAbbr (Undefined): {withoutSysAbbr}");
            Debug.WriteLine($"Following SysAbbrs not defined in settings and thus not insulated:\n" +
                //$"{string.Join("\n", nonDefinedSysAbbrs)}");
                $"{string.Join(", ", nonDefinedSysAbbrs)}");

            //Check if type of insulation is defined for all systems that are to be insulated
            var list = new HashSet<(string sysAbbr, string InsulationType)>();
            var groups = iws.GroupBy(iw => iw.sysAbbr);
            foreach (var group in groups)
            {
                string pipeInsulationName = dh.ReadParameterFromDataTable(
                    group.Key, Settings.InsulationParameters, "Type");
                if (pipeInsulationName == null)
                    throw new Exception($"No insulation type defined in settings for sysAbbr {group.Key}!");
                PipeInsulationType pipeInsulationType =
                    fi.GetElements<PipeInsulationType, BuiltInParameter>(
                        doc, BuiltInParameter.ALL_MODEL_TYPE_NAME, pipeInsulationName).FirstOrDefault();
                if (pipeInsulationType == null)
                {
                    list.Add((group.Key, pipeInsulationName));
                }
            }
            //Filter out elements where insulation type is not defined
            iws = iws.Where(iw => !list.Any(x => x.sysAbbr == iw.sysAbbr)).ToHashSet();

            BuildingCoderUtilities.InfoMsg(
                $"The following systems will not be insulated because their " +
                $"insulation type does not exist in project:\n" +
                $"{GetAsciiTableString(list)}");

            if (iws.Count == 0)
            {
                Debug.WriteLine("No elements left to insulate!");
                return Result.Failed;
            }
            else
            {
                Debug.WriteLine($"Number of elements to insulate: {iws.Count}");
                Debug.WriteLine($"Systems to insulate: " +
                    $"{string.Join(", ", iws.Select(iw => iw.sysAbbr).Distinct().OrderBy(x => x))}");
            }

            using (Transaction tx = new Transaction(doc))
            {
                try
                {
                    tx.Start("Create all insulation");

                    foreach (IW iw in iws) iw.Insulate(doc);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex.ToString());
                    tx.RollBack();
                    throw;
                    //return Result.Failed;
                }
                tx.Commit();
            }
            return Result.Succeeded;
        }
        private static DataTable GetInsulationSettings(Document doc)
        {
            //Manage Insulation creation settings
            //Test if settings file exist
            string pn = doc.ProjectInformation.Name;
            string pathToSettingsXml =
                Environment.ExpandEnvironmentVariables(
                    $"%AppData%\\MyRevitAddins\\MEPUtils\\Settings.{pn}.Insulation.xml"); //Magic text?
            bool settingsExist = File.Exists(pathToSettingsXml);

            //Initialize an empty datatable
            DataTable settings = new DataTable("InsulationSettings");

            if (settingsExist) //Read file if exists
            {
                using (Stream stream = new FileStream(pathToSettingsXml, FileMode.Open, FileAccess.Read))
                {
                    DataSet ds = new DataSet();
                    ds.ReadXml(stream);
                    settings = ds.Tables[0];
                }
            }
            else
                throw new Exception(
                    "Insulation creation settings file does not exist! Run configuration routine first!");
            return settings;
        }
        private static DataTable GetInsulationParameters()
        {
            //Manage Insulation parameters settings
            string pathToInsulationCsv =
                Environment.ExpandEnvironmentVariables("%AppData%\\MyRevitAddins\\MEPUtils\\Insulation.csv");
            bool fileExists = File.Exists(pathToInsulationCsv);
            if (!fileExists)
                throw new Exception(
                    @"No insulation configuration file exists at: %AppData%\MyRevitAddins\MEPUtils\Insulation.csv");

            //DataSet insulationDataSet = DataHandler.ImportExcelToDataSet(pathToInsulationExcel, "YES");
            //DataTable insulationData = DataHandler.ReadDataTable(insulationDataSet.Tables, "Insulation");
            //TODO: Interop is very slow. Implement a .csv solution.
            DataTable insulationData = CsvReader.ReadInsulationCsv(pathToInsulationCsv);
            return insulationData;
        }
        public static Result DeleteAllPipeInsulation(UIApplication uiApp)
        {
            Document doc = uiApp.ActiveUIDocument.Document;

            var allInsulation = fi.GetElements<PipeInsulation, BuiltInParameter>(doc, BuiltInParameter.INVALID);
            if (allInsulation == null) return Result.Failed;
            else if (allInsulation.Count == 0) return Result.Failed;

            var fittings = fi.GetElements<FamilyInstance, BuiltInCategory>(doc, BuiltInCategory.OST_PipeFitting).ToHashSet();



            Transaction tx = new Transaction(doc);
            tx.Start("Delete all insulation!");
            foreach (Element el in allInsulation) doc.Delete(el.Id);

            foreach (FamilyInstance fi in fittings)
            {
                var mf = fi.MEPModel as MechanicalFitting;
                if (mf.PartType.ToString() == "Tee")
                {
                    //Set insulation to 0
                    Parameter par1 = fi.LookupParameter("Insulation Projected");
                    par1?.Set(0);

                    //Make invisible also
                    Parameter par2 = fi.LookupParameter("Dummy Insulation Visible");
                    if (par2.AsInteger() == 1) par2.Set(0);
                }
            }

            tx.Commit();

            return Result.Succeeded;
        }
        private static string GetAsciiTableString(HashSet<(string sysAbbr, string InsulationType)> data)
        {
            if (data.Count == 0) return "";

            string header1 = "System";
            string header2 = "Insulation Type";

            int maxWidth1 = Math.Max(header1.Length, data.Max(t => t.sysAbbr.Length));
            int maxWidth2 = Math.Max(header2.Length, data.Max(t => t.InsulationType.Length));
            string divider = "+" + new string('-', maxWidth1 + 2) + "+" + new string('-', maxWidth2 + 2) + "+";
            var sb = new System.Text.StringBuilder();

            sb.AppendLine(divider);
            sb.AppendFormat("| {0,-" + maxWidth1 + "} | {1,-" + maxWidth2 + "} |\n", header1, header2);
            sb.AppendLine(divider);

            foreach (var tuple in data.OrderBy(t => t.sysAbbr).ThenBy(t => t.InsulationType))
            {
                sb.AppendFormat("| {0,-" + maxWidth1 + "} | {1,-" + maxWidth2 + "} |\n", tuple.sysAbbr, tuple.InsulationType);
            }

            sb.AppendLine(divider);
            return sb.ToString();
        }
    }
}
