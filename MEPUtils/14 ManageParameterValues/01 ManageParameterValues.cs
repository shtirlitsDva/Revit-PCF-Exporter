using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System;
using System.Data;
using System.IO;
using System.Linq;
using MoreLinq;
using System.Windows.Input;
using System.Collections.Generic;
using MEPUtils.SharedStaging;
using static Shared.Filter;
using static Shared.Extensions;

namespace MEPUtils.ManageParameterValues
{
    [Autodesk.Revit.Attributes.Transaction(Autodesk.Revit.Attributes.TransactionMode.Manual)]
    public class SplitParameterValue : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            return SplitParameterValues(commandData);
        }

        internal Result SplitParameterValues(ExternalCommandData cData)
        {
            Document doc = cData.Application.ActiveUIDocument.Document;
            var pipeAccessories = GetElements<Element, BuiltInCategory>(doc, BuiltInCategory.OST_PipeAccessory);

            using (Transaction tx = new Transaction(doc))
            {
                tx.Start("Split TAGs!");
                foreach (Element e in pipeAccessories)
                {
                    Parameter TAG2Par = e.LookupParameter("TAG 2");
                    string TAG2Value = TAG2Par.ToValueString();

                    string valueToFindAndSplit = "DVL";

                    if (TAG2Value.Contains(valueToFindAndSplit))
                    {
                        Parameter TAG1Par = e.LookupParameter("TAG 1");
                        TAG1Par.Set(valueToFindAndSplit);

                        TAG2Value = TAG2Value.Replace(valueToFindAndSplit, "");
                        TAG2Par.Set(TAG2Value);
                    }
                }
                tx.Commit();
            }
            return Result.Succeeded;
        }
    }
}
