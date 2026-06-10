using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Plumbing;

using Shared;

using mp = Shared.MepUtils;

namespace PcfExporter.Model
{
    /// <summary>
    /// OLET differs from TEE-STUB in how the centre point on the host pipe is found:
    /// an angled olet's primary connector does not sit on the pipe axis, so a plain
    /// projection is wrong. The congruent-rectangle construction below (ported from
    /// the original exporter's Dynamo/Python solution) finds the true intersection
    /// of the olet axis with the pipe axis.
    /// </summary>
    internal class PCF_OLET : PCF_TEE_STUB
    {
        public PCF_OLET(Element element, ExportSession s) : base(element, s) { }

        protected override StringBuilder WriteNonTappedData(Pipe refPipe)
        {
            var sb = new StringBuilder();

            var refPipeCons = new Cons(refPipe);

            //The olet geometry is analyzed with congruent rectangles to find the
            //connection point on the pipe even for angled olets.
            XYZ B = Cons.Primary.Origin;
            XYZ D = Cons.Secondary.Origin;
            XYZ pipeEnd1 = refPipeCons.Primary.Origin;
            XYZ pipeEnd2 = refPipeCons.Secondary.Origin;
            XYZ BDvector = D - B;
            XYZ ABvector = pipeEnd1 - pipeEnd2;
            double angle = Conversion.RadianToDegree(ABvector.AngleTo(BDvector));
            if (angle > 90)
            {
                ABvector = -ABvector;
                angle = Conversion.RadianToDegree(ABvector.AngleTo(BDvector));
            }
            Line refsLine = Line.CreateBound(pipeEnd1, pipeEnd2);
            XYZ C = refsLine.Project(B).XYZPoint;
            double L3 = B.DistanceTo(C);
            XYZ E = refsLine.Project(D).XYZPoint;
            double L4 = D.DistanceTo(E);
            double ratio = L4 / L3;
            double L1 = E.DistanceTo(C);
            double L5 = L1 / (ratio - 1);
            XYZ A;
            if (angle < 89)
            {
                XYZ ECvector = (C - E).Normalize();
                double L = L1 + L5;
                A = E.Add(ECvector.Multiply(L));
            }
            else A = E;
            angle = Math.Round(angle * 100);

            sb.Append(EW.WriteCP(A));
            sb.Append(EW.WriteBP1(Element, Cons.Secondary));
            sb.Append("    ANGLE ");
            sb.Append(Conversion.AngleToPCF(angle));
            sb.AppendLine();

            return sb;
        }
    }
}
