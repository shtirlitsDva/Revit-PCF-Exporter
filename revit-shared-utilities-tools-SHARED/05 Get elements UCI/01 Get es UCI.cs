using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Plumbing;
using Autodesk.Revit.DB.Structure;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;

using MoreLinq;

using Shared;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;

using dbg = Shared.Dbg;
using fi = Shared.Filter;
using mp = Shared.MepUtils;
using tr = Shared.Transformation;

namespace Shared.Tools
{
    class GetElementsUCI
    {
        public static Result GetEsUCI(ExternalCommandData cData)
        {
            UIApplication uiApp = cData.Application;
            Document doc = cData.Application.ActiveUIDocument.Document;
            UIDocument uidoc = uiApp.ActiveUIDocument;

            Selection selection = uidoc.Selection;

            List<string> UCIs = new List<string>();

            foreach (var id in selection.GetElementIds())
            {
                Element el = doc.GetElement(id);
                UCIs.Add(el.UniqueId);
            }

            if (UCIs.Count > 0)
            {
                string path =
                    Environment.ExpandEnvironmentVariables("%temp%") + "\\" + "ElsUcis.txt";
                System.IO.File.WriteAllText(path, string.Join("\n", UCIs));
                Process.Start("notepad.exe", path);
            }

            return Result.Succeeded;
        }
    }
}
