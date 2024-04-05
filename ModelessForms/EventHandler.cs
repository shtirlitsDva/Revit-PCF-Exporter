using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
using WinForms = System.Windows.Forms;
using System.IO;
using System.Windows.Media.Imaging;
using System.Reflection;
using System.Linq;
using System.Diagnostics;

using Autodesk;
using Autodesk.Revit;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.UI.Selection;

namespace ModelessForms
{
    public class ExternalEventHandler : IExternalEventHandler
    {
        Application ThisApp;

        public ExternalEventHandler(Application thisApp)
        {
            ThisApp = thisApp;
        }

        public void Execute(UIApplication app)
        {
            UIDocument uidoc = app.ActiveUIDocument;
            Document doc = uidoc.Document;

            ThisApp.asyncCommand.Execute(app);
        }

        public string GetName()
        {
            return "Update parameter data";
        }
    }
}
