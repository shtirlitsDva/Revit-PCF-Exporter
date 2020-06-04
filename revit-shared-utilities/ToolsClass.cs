using System;
using System.Collections.Generic;
using System.Linq;
//using MoreLinq;
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


namespace Shared.Tools
{
    public class AnalysisTools
    {
        public static Result FormCaller(ExternalCommandData cData)
        {
            Tools tools = new Tools(Cursor.Position.X, Cursor.Position.Y);
            tools.ShowDialog();
            //mepuc.Close();
            
            if (tools.MethodToExecute == null) return Result.Cancelled;

            return tools.MethodToExecute.Invoke(cData);
        }
    }
}
