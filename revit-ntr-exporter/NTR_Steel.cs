using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using NTR_Functions;
using dw = NTR_Functions.DataWriter;

namespace NTR_Exporter
{
    internal class NTR_Steel
    {
        private ConfigurationData conf;
        private Document doc;

        public NTR_Steel(ConfigurationData conf, Document doc)
        {
            this.conf = conf;
            this.doc = doc;
        }

        internal StringBuilder Export()
        {
            StringBuilder sb = new StringBuilder();

            var AllAnalyticalModelSticks = Shared.Filter
                .GetElements<AnalyticalModelStick, BuiltInCategory>(doc, BuiltInCategory.INVALID);

            List<AnalyticalSteelElement> ASE_OriginalList = new List<AnalyticalSteelElement>();

            foreach (AnalyticalModelStick ams in AllAnalyticalModelSticks)
            {
                AnalyticalSteelElement ase = new AnalyticalSteelElement(doc, ams);
                ASE_OriginalList.Add(ase);
            }



            return sb;
        }
    }

    internal class AnalyticalSteelElement
    {
        public XYZ P1;
        public XYZ P2;
        public Curve Curve;
        public Element Host;
        public bool Include = true;

        public AnalyticalSteelElement(Document doc, AnalyticalModelStick ams)
        {
            Curve = ams.GetCurve();
            P1 = Curve.GetEndPoint(0);
            P2 = Curve.GetEndPoint(1);
            Host = doc.GetElement(ams.GetElementId());
        }
    }
}