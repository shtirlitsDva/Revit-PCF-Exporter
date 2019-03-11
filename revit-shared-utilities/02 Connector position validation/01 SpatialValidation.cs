using System;
using System.Collections.Generic;
using System.Linq;
using MoreLinq;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Plumbing;
using Autodesk.Revit.DB.Structure;
using Shared;
using fi = Shared.Filter;
using tr = Shared.Transformation;
using mp = Shared.MepUtils;
using dbg = Shared.Dbg;

namespace Shared.Tools
{
    class SpatialValidation
    {
        public static Result ValidateConnectorsSpatially(ExternalCommandData cData)
        {
            UIApplication uiApp = cData.Application;
            Document doc = cData.Application.ActiveUIDocument.Document;
            UIDocument uidoc = uiApp.ActiveUIDocument;

            //Gather all connectors from the document
            HashSet<Connector> AllCons = mp.GetALLConnectorsInDocument(doc);

            //Create collection with distinct connectors
            var DistinctCons = AllCons.ToHashSet(new ConnectorXyzComparer());

            return Result.Succeeded;
        }
    }
}
