using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System;
using System.Data;
using System.IO;
using System.Linq;
//using MoreLinq;
using System.Windows.Input;
using System.Collections.Generic;
using static Shared.Filter;
using static Shared.Extensions;

namespace MEPUtils.NumberStuff
{
    public class NumberStuff
    {
        //1. Ask what families to number
        //  a. Use XML type of window from insulation
        //2. Present a windows:
        //  a. Shows choosen families 1. column
        //  b. Column 2: TAG 1 -> fixed text
        //  c. Column 3: TAG 2 -> Start number
        //  d. Column 4: How many digits number to be formatted (default: 2)

        public Result NumberStuffMethod(UIApplication uiApp)
        {
            Document doc = uiApp.ActiveUIDocument.Document;

            bool ctrl = false;
            if ((int)Keyboard.Modifiers == 2) ctrl = true;

            DataTable settings;

            if (!ctrl)
            {
                try
                {
                    settings = GetNumberStuffSettings(doc);
                }
                catch (Exception)
                {
                    ExecuteNumberStuffSettings(uiApp);
                }
            }
            else ExecuteNumberStuffSettings(uiApp);

            settings = GetNumberStuffSettings(doc);

            ctrl = false;
            NumberStuffForm nsf = new NumberStuffForm(doc, settings);
            nsf.ShowDialog();

            if ((int)Keyboard.Modifiers == 2) ctrl = true;

            //If Number button was pushed -- execute numbering
            if (nsf.Result == Result.Succeeded)
            {
                using (Transaction tx = new Transaction(doc))
                {
                    tx.Start("(Re-)Number!");
                    var rowsToNumber = nsf.Settings.AsEnumerable()
                                .Where(row => bool.Parse(row.Field<string>("Number")) == true);

                    var groupByPrefix = rowsToNumber.GroupBy(row => row.Field<string>("Prefix"));

                    foreach (IGrouping<string, DataRow> group in groupByPrefix)
                    {
                        HashSet<Element> elements = new HashSet<Element>();
                        int nrOfDigits = 2;

                        foreach (DataRow item in group)
                        {
                            elements.UnionWith(GetElements<FamilyInstance, BuiltInParameter>
                                (doc, BuiltInParameter.ELEM_FAMILY_PARAM, item.Field<string>("Family")));
                            nrOfDigits = int.Parse(item.Field<string>("Digits"));
                        }

                        //Quick hack: filter out EXISTING elements
                        elements = elements.Where(x => x.LookupParameter("PCF_ELEM_SPEC").ToValueString() != "EXISTING").ToHashSet();

                        List<Element> sortedElements = elements
                            .OrderBy(x => ((LocationPoint)x.Location).Point.X.Round(1))
                            .ThenBy(x => ((LocationPoint)x.Location).Point.Y.Round(1))
                            .ThenBy(x => ((LocationPoint)x.Location).Point.Z.Round(1))
                            .ToList();

                        //Number the sorted elements sequentially
                        int startNumber = 1;

                        //TODO: Implement overwrite/keep existing!
                        //Create a sequence of all TAGs and find missing in sequence.

                        foreach (Element e in sortedElements)
                        {
                            Parameter Tag1 = e.get_Parameter(new Guid("a93679f7-ca9e-4a1e-bb44-0d890a5b4ba1"));
                            Parameter Tag2 = e.get_Parameter(new Guid("3b2afba4-447f-422a-8280-fd394718ad4e"));

                            string value1 = Tag1.AsString();
                            string value2 = Tag2.AsString();

                            if (!ctrl) //If CONTROL is not pressed, do not overwrite
                            {
                                throw new NotImplementedException("\"DO NOT OVERWRITE\" mode is not implemented!");
                            }
                            else //Else overwrite
                            {
                                Tag1.Set(group.Key);
                                Tag2.Set(startNumber.ToString("D" + nrOfDigits.ToString()));
                                startNumber++;
                            }


                        }
                    }
                    tx.Commit();
                }
            }

            return Result.Succeeded;
        }

        public Result ExecuteNumberStuffSettings(UIApplication uiApp)
        {
            NumberStuffFamilyChooser isw = new NumberStuffFamilyChooser(uiApp);
            isw.ShowDialog();
            isw.Close();
            using (Stream stream = new FileStream(isw.PathToSettingsXml, FileMode.Create, FileAccess.Write))
            {
                isw.Settings.WriteXml(stream);
            }

            return Result.Succeeded;
        }

        private static DataTable GetNumberStuffSettings(Document doc)
        {
            //Manage Insulation creation settings
            //Test if settings file exist
            string pn = doc.ProjectInformation.Name;
            string pathToSettingsXml =
                Environment.ExpandEnvironmentVariables(
                    $"%AppData%\\MyRevitAddins\\MEPUtils\\Settings.{pn}.NumberStuff.xml"); //Magic text?
            bool settingsExist = File.Exists(pathToSettingsXml);

            //Initialize an empty datatable
            DataTable settings = new DataTable("NumberStuff");

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
                    "Number Stuff numbering settings file does not exist! Run configuration routine first!");
            return settings;
        }
    }
}
