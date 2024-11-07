using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Plumbing;
using Autodesk.Revit.DB.Structure;
using Autodesk.Revit.DB.Mechanical;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;

using Microsoft.WindowsAPICodePack.Dialogs;
using MoreLinq;
using Shared;
using Shared.Forms;
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

namespace MEPUtils.SetParValueAndIncrement
{
    [Transaction(TransactionMode.Manual)]
    class SelectByGuid : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIApplication uiApp = commandData.Application;
            Document doc = commandData.Application.ActiveUIDocument.Document;
            UIDocument uidoc = uiApp.ActiveUIDocument;

            string parName = string.Empty;
            string prefix = string.Empty;
            int startValue = 0;
            string format = string.Empty;

            InputBoxBasic ib = new InputBoxBasic();
            UI.SetStatusText("Input parameter name to modify:");
            ib.ShowDialog();
            if (ib.InputText.IsNoE()) return Result.Cancelled;
            parName = ib.InputText;

            ib = new InputBoxBasic();
            UI.SetStatusText("Input prefix value:");
            ib.ShowDialog();
            if (ib.InputText.IsNoE()) return Result.Cancelled;
            prefix = ib.InputText;

            ib = new InputBoxBasic();
            UI.SetStatusText("Input start value:");
            ib.ShowDialog();
            if (ib.InputText.IsNoE()) return Result.Cancelled;
            string inputValue = ib.InputText;

            if (!int.TryParse(inputValue, out startValue))
            {
                UI.SetStatusText("Start value is not a number (integer).");
                return Result.Cancelled;
            }

            ib = new InputBoxBasic();
            UI.SetStatusText("Input number format:");
            ib.ShowDialog();
            if (ib.InputText.IsNoE()) return Result.Cancelled;
            format = ib.InputText;

            while (true)
            {
                Element element = 
                    Shared.BuildingCoder.BuildingCoderUtilities.SelectSingleElement(
                        uidoc, " to modify parameter value: ");

                if (element == null) { return Result.Cancelled; }

                using (Transaction t = new Transaction(doc, "Set parameter value"))
                {
                    t.Start();

                    try
                    {
                        Parameter par = element.LookupParameter(parName);
                        if (par == null)
                        {
                            UI.SetStatusText("Parameter not found.");
                            t.RollBack();
                            return Result.Cancelled;
                        }

                        if (par.StorageType != StorageType.String)
                        {
                            UI.SetStatusText("Parameters' storage type is not a string.");
                            t.RollBack();
                            return Result.Cancelled;
                        }

                        string value = prefix + startValue.ToString(format);
                        par.Set(value);

                        startValue++;
                    }
                    catch (Exception)
                    {
                        t.RollBack();
                        throw;
                    }

                    t.Commit();
                }
            }
        }
    }
}
