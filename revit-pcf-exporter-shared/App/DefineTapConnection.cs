using System;

using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

using Shared.BuildingCoder;

namespace PcfExporter.App
{
    /// <summary>
    /// Interactive command: user picks the tapped element, then the tapping element;
    /// the tapping element's UniqueId is stored in the first free PCF_ELEM_TAP1..3 slot.
    /// </summary>
    public class DefineTapConnection
    {
        public Result Execute(ExternalCommandData commandData, ref string msg)
        {
            UIApplication uiApp = commandData.Application;
            Document doc = uiApp.ActiveUIDocument.Document;
            UIDocument uidoc = uiApp.ActiveUIDocument;
            var trans = new Transaction(doc, "Define tap");
            trans.Start();

            try
            {
                Element tappedElement = BuildingCoderUtilities.SelectSingleElement(
                    uidoc, "Select tapped element.");
                if (tappedElement == null) throw new OperationCanceledException("Tap Connection cancelled!");

                Element tappingElement = BuildingCoderUtilities.SelectSingleElement(
                    uidoc, "Select tapping element.");
                if (tappingElement == null) throw new OperationCanceledException("Tap Connection cancelled!");

                if (string.IsNullOrEmpty(tappedElement.LookupParameter("PCF_ELEM_TAP1").AsString()))
                    tappedElement.LookupParameter("PCF_ELEM_TAP1").Set(tappingElement.UniqueId);
                else if (string.IsNullOrEmpty(tappedElement.LookupParameter("PCF_ELEM_TAP2").AsString()))
                    tappedElement.LookupParameter("PCF_ELEM_TAP2").Set(tappingElement.UniqueId);
                else if (string.IsNullOrEmpty(tappedElement.LookupParameter("PCF_ELEM_TAP3").AsString()))
                    tappedElement.LookupParameter("PCF_ELEM_TAP3").Set(tappingElement.UniqueId);
                else
                    BuildingCoderUtilities.ErrorMsg(
                        "All tapping slots are taken. Manually delete unwanted values or increase number of tapping slots.");

                trans.Commit();
            }
            catch (OperationCanceledException)
            {
                trans.RollBack();
                return Result.Cancelled;
            }
            catch (Autodesk.Revit.Exceptions.OperationCanceledException)
            {
                trans.RollBack();
                return Result.Cancelled;
            }
            catch (Exception ex)
            {
                trans.RollBack();
                msg = ex.Message;
                return Result.Failed;
            }

            return Result.Succeeded;
        }
    }
}
