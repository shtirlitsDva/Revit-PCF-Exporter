using System;
using System.Collections.Generic;
using System.Linq;
using MoreLinq;
using System.Data;
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


namespace MEPUtils
{
    public class MEPUtilsClass
    {
        public static Result FormCaller(ExternalCommandData cData)
        {
            MEPUtilsChooser mepuc = new MEPUtilsChooser(Cursor.Position.X, Cursor.Position.Y);
            mepuc.ShowDialog();
            //mepuc.Close();
            
            if (mepuc.MethodToExecute == null) return Result.Cancelled;

            return mepuc.MethodToExecute.Invoke(cData);
        }
    }
}
