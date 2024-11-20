using Autodesk.Revit.DB;

using Shared;
using PCF_Functions;
using plst = PCF_Functions.Parameters;

using System;
using System.Collections.Generic;
using System.Text;

namespace PCF_Model
{
    internal class PCF_PIPE : PCF_EP1_EP2
    {
        public PCF_PIPE(Element element) : base(element) { }
        protected override StringBuilder WriteSpecificData()
        {
            StringBuilder sb = new StringBuilder();

            sb.Append(base.WriteSpecificData());

            var spec = plst.PCF_ELEM_SPEC.GetValue(Element);
            if (spec.IsNotNoE())
            {
                sb.Append(
                    SpecManager.SpecManager.GetWALLTHICKNESS(
                        spec, Conversion.PipeSizeToMm(Cons.Primary.Radius)));
            }

            return sb;
        }
    }
}
