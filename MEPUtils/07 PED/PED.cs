using System;
using System.Collections.Generic;
using System.Linq;
using System.Data;
using System.Reflection;
using System.Globalization;
//using MoreLinq;
using System.Text;
using System.IO;
using System.Windows.Input;
using System.Threading.Tasks;
using Autodesk.Revit.UI;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Plumbing;
using Autodesk.Revit.DB.Structure;
using Autodesk.Revit.ApplicationServices;
using Shared;
using fi = Shared.Filter;
using op = Shared.Output;
using tr = Shared.Transformation;
using mp = Shared.MepUtils;
using NLog;

namespace MEPUtils.PED
{
    public class InitPED
    {
        public void processOlets(ExternalCommandData commandData)
        {
            Document doc = commandData.Application.ActiveUIDocument.Document;

            NumberFormatInfo nfi = new NumberFormatInfo();
            nfi.NumberDecimalSeparator = ",";

            //Collect and filter Olets
            ElementParameterFilter epf = fi.ParameterValueGenericFilter(doc, "Olet", new Guid("e0baa750-22ba-4e60-9466-803137a0cba8"));
            FilteredElementCollector col = new FilteredElementCollector(doc);
            var olets = col.OfCategory(BuiltInCategory.OST_PipeFitting).OfClass(typeof(FamilyInstance)).WherePasses(epf).ToHashSet();

            foreach (Element olet in olets)
            {
                Cons cons = mp.GetConnectors(olet);
                Connector prim = cons.Primary;
                //Test if Connector is connected
                if (prim.IsConnected)
                {
                    ConnectorSet refConSet = prim.AllRefs;
                    var refCons = mp.GetAllConnectorsFromConnectorSet(refConSet);
                    Connector refCon = refCons.Where(x => isPipe(x.Owner)).SingleOrDefault();
                    if (refCon == null) refCon = refCons.Where(x => x.Owner.IsType<FamilyInstance>()).FirstOrDefault();
                    if (refCon == null) throw new Exception($"Element {olet.Id.IntegerValue} refCon Owner cannot find a Pipe of FamilyInstance!");

                    double dia = 0;

                    switch (refCon.Owner)
                    {
                        case Pipe pipe:
                            dia = pipe.get_Parameter(BuiltInParameter.RBS_PIPE_OUTER_DIAMETER).AsDouble().FtToMm().Round(1);
                            break;
                        case FamilyInstance fis:
                            Element element = (Element)fis;
                            Cons consFis = mp.GetConnectors(element);
                            dia = (consFis.Primary.Radius * 2).FtToMm().Round(1);
                            break;
                        default:
                            break;
                    }

                    //Pipe pipe = (Pipe)refCon.Owner;

                    Parameter weldsToPar = olet.get_Parameter(new Guid("c3401bb0-2e6c-4831-9917-73d6784a4a6f"));
                    weldsToPar.Set("ø" + dia.ToString(nfi));
                }
            }

            bool isPipe(Element elem)
            {
                switch (elem)
                {
                    case Pipe pipe:
                        return true;
                    default:
                        return false;
                }
            }
        }
    }
}