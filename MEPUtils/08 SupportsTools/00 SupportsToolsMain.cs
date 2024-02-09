using System;
using System.Collections.Generic;
using System.Linq;
using MoreLinq;
using System.Windows.Forms;
using Autodesk.Revit.UI;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Plumbing;
using Autodesk.Revit.DB.Structure;
using Shared;
using fi = Shared.Filter;
using op = Shared.Output;
using tr = Shared.Transformation;
using mp = Shared.MepUtils;

namespace MEPUtils.SupportTools
{
    public class SupportToolsMain
    {
        public static Result CallForm(UIApplication uiApp)
        {
            Document doc = uiApp.ActiveUIDocument.Document;
            UIDocument uidoc = uiApp.ActiveUIDocument;

            SupportTools st = new SupportTools(Cursor.Position.X, Cursor.Position.Y);
            st.ShowDialog();
            //mepuc.Close();

            if (st.ToolToInvoke == null ||
                st.Cancelled) return Result.Cancelled;

            st.ToolToInvoke.Invoke(uiApp);

            return Result.Succeeded;
        }
    }
}



