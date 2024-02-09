using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Autodesk.Revit.DB;
using Autodesk.Revit.UI.Selection;

//using static Shared.SimpleLogger;

using Shared;
using System.Linq.Expressions;

namespace MEPUtils.PressureLossCalc
{
    public static class CalcPressureLoss
    {
        public static double currentFlow;
        internal static double currentInsideDiameter;
        internal static double waterKinematicVisdcosity = 0.000000365; //m²/s
        internal static double area { get => 0.25 * Math.PI * Math.Pow(currentInsideDiameter, 2.0); } //m²
        static double waterDensity = 958.05; //kg/m³
        static double massFlow { get => waterDensity * currentFlow / 3600.0; } //kg/s
        static double velocity { get => currentFlow / area / 3600.0; }
        static double Reynolds { get => velocity * currentInsideDiameter / waterKinematicVisdcosity; }
        static double g = 9.81; //m/s²
        static double pipeRoughness = 0.0001; //m
        /// <summary>
        /// Using Colebrook formula iterative solution.
        /// </summary>
        /// <returns>Pressure loss in Pa/m.</returns>
        public static double CalculatePressureLoss()
        {
            double previousRHS;
            double LHS = 0.3;
            double RHS = 1000.0;

            bool run = true;

            int count = 0;
            while (run)
            {
                count++;
                previousRHS = RHS;

                RHS = -2 * Math.Log10(
                (2.51 / (Reynolds * Math.Sqrt(LHS))) +
                (pipeRoughness / (3.71 * currentInsideDiameter)));

                LHS = Math.Pow(1 / RHS, 2.0);

                if (Math.Abs(previousRHS - RHS) < 0.000001) run = false;
                if (count > 10000) run = false;
            }

            //log($"Area: {area}, Flow: {currentFlow}, Hst.hd: {velocity}, Re: {Reynolds}," +
            //    $" Count: {count}, LHS: {LHS}, RHS: {RHS}");

            return LHS * Math.Pow(velocity, 2) * waterDensity / (2 * currentInsideDiameter);
        }
    }
    public static class CalcFlow
    {
        static double waterSpecificThermalCapacity = 4545.0; //J/(kg*K)
        static double waterDensity = 958.05; //kg/m³
        public static double calculateFlow(double power, double tF, double tR)
        {
            return power / (
                waterSpecificThermalCapacity * waterDensity * (
                    tF - tR)) * (1000 * 3600); //m³/h
        }
    }
}
