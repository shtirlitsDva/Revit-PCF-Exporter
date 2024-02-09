using System;
using System.Collections.Generic;
using System.Linq;
//using MoreLinq;
using Autodesk.Revit.UI;
using Autodesk.Revit.DB;
using System.Text;
using fi = Shared.Filter;
using op = Shared.Output;
using mp = Shared.MepUtils;
//using mySettings = GeneralStability.Properties.Settings;

namespace MEPUtils
{
    public class PipeInsulationVisibility
    {
        public static void TogglePipeInsulationVisibility(ExternalCommandData commandData)
        {
            var app = commandData.Application;
            var uiDoc = app.ActiveUIDocument;
            var doc = uiDoc.Document;

            //Get Pipe Insulation Category reference
            Category pipeInsCat = Category.GetCategory(doc, BuiltInCategory.OST_PipeInsulations);
            //Get current view reference
            View curView = doc.ActiveView;

            //Get custom Insulation category I use in Tees and Valves
            Category pipeFitCat = Category.GetCategory(doc, BuiltInCategory.OST_PipeFitting);
            var pipeFitCatSubs = pipeFitCat.SubCategories;
            var fitInsulCat = (from Category cat in pipeFitCatSubs where cat.Name == "Insulation" select cat).FirstOrDefault();

            Category pipeAccCat = Category.GetCategory(doc, BuiltInCategory.OST_PipeAccessory);
            var pipeAccCatSubs = pipeAccCat.SubCategories;
            var accInsulCat = (from Category cat in pipeAccCatSubs where cat.Name == "Insulation" select cat).FirstOrDefault();

            if (fitInsulCat != null && accInsulCat == null)
            {
                if (curView.GetCategoryHidden(pipeInsCat.Id))
                {
                    curView.SetCategoryHidden(pipeInsCat.Id, false);
                    curView.SetCategoryHidden(fitInsulCat.Id, false);
                }
                else
                {
                    curView.SetCategoryHidden(pipeInsCat.Id, true);
                    curView.SetCategoryHidden(fitInsulCat.Id, true);
                }
            }
            else if (fitInsulCat != null && accInsulCat != null)
            {
                if (curView.GetCategoryHidden(pipeInsCat.Id))
                {
                    curView.SetCategoryHidden(pipeInsCat.Id, false);
                    curView.SetCategoryHidden(fitInsulCat.Id, false);
                    curView.SetCategoryHidden(accInsulCat.Id, false);
                }
                else
                {
                    curView.SetCategoryHidden(pipeInsCat.Id, true);
                    curView.SetCategoryHidden(fitInsulCat.Id, true);
                    curView.SetCategoryHidden(accInsulCat.Id, true);
                }
            }
            else if (fitInsulCat == null && accInsulCat != null)
            {
                if (curView.GetCategoryHidden(pipeInsCat.Id))
                {
                    curView.SetCategoryHidden(pipeInsCat.Id, false);
                    curView.SetCategoryHidden(accInsulCat.Id, false);
                }
                else
                {
                    curView.SetCategoryHidden(pipeInsCat.Id, true);
                    curView.SetCategoryHidden(accInsulCat.Id, true);
                }
            }
            else
            {
                if (curView.GetCategoryHidden(pipeInsCat.Id))
                {
                    curView.SetCategoryHidden(pipeInsCat.Id, false);
                }
                else
                {
                    curView.SetCategoryHidden(pipeInsCat.Id, true);
                }
            }
        }
    }
}

