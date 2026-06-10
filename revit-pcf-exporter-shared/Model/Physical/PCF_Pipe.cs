using Autodesk.Revit.DB;

using Shared;
using plst = PcfExporter.Model.Parameters;

using System;
using System.Collections.Generic;
using System.Text;

namespace PcfExporter.Model
{
    internal class PCF_PIPE : PCF_EP1_EP2
    {
        public PCF_PIPE(Element element, ExportSession s) : base(element, s) { }
        protected override StringBuilder WriteSpecificData()
        {
            StringBuilder sb = new StringBuilder();

            sb.Append(base.WriteSpecificData());

            var spec = plst.PCF_ELEM_SPEC.GetValue(Element);
            if (spec.IsNotNoE())
            {
                sb.Append(
                    S.Specs.GetWallThicknessLine(
                        spec, Conversion.PipeSizeToMm(Cons.Primary.Radius)));
            }

            return sb;
        }
    }
}
