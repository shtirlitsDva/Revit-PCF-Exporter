using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
//using MoreLinq;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Plumbing;
using Autodesk.Revit.DB.Structure;
using Autodesk.Revit.DB.Mechanical;
using Autodesk.Revit.UI;
using Shared.BuildingCoder;
using Shared;

using RevitMEPSystemTraversal;
using static RevitMEPSystemTraversal.Enums;

namespace CIINExporter
{
    public class ModelData
    {
        public StringBuilder _01_VERSION { get; set; }
        public StringBuilder _02_CONTROL { get; set; }
        public StringBuilder _03_ELEMENTS { get; set; }
        public StringBuilder _04_AUXDATA { get; } = new StringBuilder("#$ AUX_DATA\n");
        public StringBuilder _05_NODENAME { get; set; } = null;
        public StringBuilder _06_BEND { get; set; } = new StringBuilder("#$ BEND\n");
        public StringBuilder _07_RIGID { get; set; } = new StringBuilder("#$ RIGID\n");
        public StringBuilder _08_EXPJT { get; set; } = new StringBuilder("#$ EXPJT\n");
        public StringBuilder _09_RESTRANT { get; set; } = new StringBuilder("#$ RESTRANT\n");
        public StringBuilder _10_DISPLMNT { get; set; } = new StringBuilder("#$ DISPLMNT\n");
        public StringBuilder _11_FORCMNT { get; set; } = new StringBuilder("#$ FORCMNT\n");
        public StringBuilder _12_UNIFORM { get; set; } = new StringBuilder("#$ UNIFORM\n");
        public StringBuilder _13_WIND { get; set; } = new StringBuilder("#$ WIND\n");
        public StringBuilder _14_OFFSETS { get; set; } = new StringBuilder("#$ OFFSETS\n");
        public StringBuilder _15_ALLOWBLS { get; set; } = new StringBuilder("#$ ALLOWBLS\n");
        public StringBuilder _16_SIFTEES { get; set; } = new StringBuilder("#$ SIF&TEES\n");
        public StringBuilder _17_REDUCERS { get; set; } = new StringBuilder("#$ REDUCERS\n");
        public StringBuilder _18_FLANGES { get; set; } = new StringBuilder("#$ FLANGES\n");
        public StringBuilder _19_EQUIPMNT { get; set; } = new StringBuilder("#$ EQUIPMNT\n");
        public StringBuilder _20_MISCEL_1 { get; set; } = new StringBuilder("#$ MISCEL_1\n");
        public StringBuilder _21_UNITS { get; set; }
        public StringBuilder _22_COORDS { get; set; }

        public AnalyticModel Data;

        public ModelData(AnalyticModel Model)
        {
            Data = Model;
        }

        public void ProcessData()
        {
            _01_VERSION = Section_VERSION();
            _02_CONTROL = Section_CONTROL(Data);
            _03_ELEMENTS = Section_ELEMENTS(Data);

            _20_MISCEL_1.Append(Section_MISCEL_1(Data));

            _21_UNITS = Section_UNITS();
            _22_COORDS = Section_COORDS(Data);
        }

        private StringBuilder Section_MISCEL_1(AnalyticModel data)
        {
            StringBuilder sb = new StringBuilder();
            string twox = "  ";

            int MaterialNumber = 406;

            //Write material number
            int numelt = data.AllAnalyticElements.Count();
            bool straight = true;

            int nlines = numelt / 6;
            if (numelt % 6 != 0) { nlines += 1; straight = false; }

            for (int i = 0; i < nlines; i++)
            {
                if (i == nlines - 1 && straight == false)
                {
                    int rest = numelt - (nlines - 1) * 6;
                    sb.Append(twox);
                    sb.Append(FLO(MaterialNumber, 13, 0, 3, rest));
                    sb.AppendLine();
                    break;
                }

                sb.Append(twox);
                sb.Append(FLO(MaterialNumber, 13, 0, 3, 6));
                sb.AppendLine();
            }

            //Nozzles - not implemented
            //Hangers - not implemented
            //Execution Options
            sb.Append(@"              1            0            0            2       0.0000            0
              0            0  10.0000      10.0000                0            0
              0            0            0            0       0.2500            3
              0");
            sb.AppendLine();

            return sb;
        }



        //CII VERSION section
        internal static StringBuilder Section_VERSION()
        {
            string bl = "                                                                             ";

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("#$ VERSION ");
            sb.AppendLine("    5.00000      10.0000        1252");
            sb.AppendLine("    PROJECT:                                                                 ");
            sb.AppendLine(bl);
            sb.AppendLine("    CLIENT :                                                                 ");
            sb.AppendLine(bl);
            sb.AppendLine("    ANALYST:                                                                 ");
            sb.AppendLine(bl);
            sb.AppendLine("    NOTES  :                                                                 ");
            for (int i = 0; i < 52; i++) sb.AppendLine(bl);
            sb.AppendLine("   Data generated by Revit Addin: revit-cii-neutral-exporter (GitHub)        ");
            return sb;
        }

        internal static StringBuilder Section_CONTROL(AnalyticModel model)
        {
            StringBuilder sb = new StringBuilder();
            string twox = "  ";

            //Gather data
            int numberOfReducers = model.AllAnalyticElements.Count(x => x.Type == ElemType.Transition);
            int numberOfElbows = model.AllAnalyticElements.Count(x => x.Type == ElemType.Elbow);
            int numberOfRigids = model.AllAnalyticElements.Count(x => x.Type == ElemType.Rigid);
            int numberOfTees = model.AllAnalyticElements.Count(x => x.Type == ElemType.Tee);

            sb.AppendLine("#$ CONTROL");

            //Start of a new line
            sb.Append(twox);

            //NUMELT - number of "piping" (every element with DX, DY, DZ) elements
            sb.Append(INT(model.AllAnalyticElements.Count, 13));

            //NUMNOZ - number of nozzles
            sb.Append(INT(0, 13));

            //NOHGRS - number of hangers
            sb.Append(INT(0, 13));

            //NONAM - number of Node Name data blocks (A node can be given a name besides number)
            sb.Append(INT(0, 13));

            //NORED - number of reducers
            sb.Append(INT(numberOfReducers, 13));

            //NUMFLG - number of flanges (I think they mean flange checks)
            sb.Append(INT(0, 13));

            //NEWLINE
            sb.AppendLine();
            sb.Append(twox);

            //BEND - number of bends
            sb.Append(INT(numberOfElbows, 13));

            //RIGID - number of rigids
            sb.Append(INT(numberOfRigids, 13));

            //EXPJT - number of expansion joints
            sb.Append(INT(0, 13));

            //RESTRANT - number of restraints aux blocks
            sb.Append(INT(0, 13));

            //DISPLMNT - number of displacements
            sb.Append(INT(0, 13));

            //FORCMNT - number of force/moments
            sb.Append(INT(0, 13));

            //NEWLINE
            sb.AppendLine();
            sb.Append(twox);

            //UNIFORM - number of uniform loads
            sb.Append(INT(0, 13));

            //WIND - number of wind loads
            sb.Append(INT(0, 13));

            //OFFSETS - number of element offsets
            sb.Append(INT(0, 13));

            //ALLOWBLS - number of allowables
            sb.Append(INT(0, 13));

            //SIF&TEES - number of tees
            sb.Append(INT(numberOfTees, 13));

            //IZUP flag - 0 global Y axis vertical and 1 global Z axis vertical
            sb.Append(INT(1, 13)); //Revit works with Z axis vertical, so it is easier to keep it that way

            //NEWLINE
            sb.AppendLine();
            sb.Append(twox);

            //NOZNOM - number of nozzles
            sb.AppendLine(INT(0, 13));

            return sb;
        }

        internal StringBuilder Section_ELEMENTS(AnalyticModel model)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("#$ ELEMENTS");

            foreach (AnalyticElement ae in model.AllAnalyticElements)
            {
                sb.Append(wElement(ae));
            }

            return sb;
        }

        internal static StringBuilder Section_UNITS()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine(@"#$ UNITS   
    25.4000      4.44800     0.453600     0.112980     0.112980     0.689460E-02
   0.555600     -17.7778     0.689460E-01 0.689460E-02  27680.0      27680.0    
    27680.0     0.175120     0.112980     0.175120      1.00000      6894.76    
   0.254000E-01  25.4000      25.4000      25.4000    
  units          
  ON 
  mm.
  N. 
  kg.
  N.m.  
  N.m.  
  MPa       
  C
  C
  bars      
  N./sq.mm. 
  kg/cu.m.  
  kg/cu.m.  
  kg/cu.m.  
  N./mm. 
  N.m./deg  
  N./mm. 
  g's
  N./sq.m.  
  m. 
  mm.
  mm.
  mm.");
            return sb;
        }

        internal static StringBuilder Section_COORDS(AnalyticModel model)
        {
            StringBuilder sb = new StringBuilder();
            string twox = "  ";

            sb.AppendLine("#$ COORDS");

            int count = model.Sequences.Count();
            sb.Append(twox);
            sb.AppendLine(INT(count, 13));

            foreach (var sequence in model.Sequences)
            {
                var ae = sequence.Sequence.FirstOrDefault();

                sb.Append(twox);
                sb.Append(INT(ae.From.Number, 13));
                sb.Append(FLO(ae.From.X, 13, 2, 4));
                sb.Append(FLO(ae.From.Y, 13, 2, 4));
                sb.Append(FLO(ae.From.Z, 13, 2, 4));
                sb.AppendLine();
            }

            return sb;
        }

        internal StringBuilder wElement(AnalyticElement ae)
        {
            string twox = "  ";
            StringBuilder sb = new StringBuilder();

            //New line
            sb.Append(twox);
            //From number
            sb.Append(FLO(ae.From.Number, 13, 0, 2));
            //To number
            sb.Append(FLO(ae.To.Number, 13, 0, 2));
            //Delta X
            sb.Append(FLO(ae.To.X - ae.From.X, 13, 2, 4));
            //Delta Y
            sb.Append(FLO(ae.To.Y - ae.From.Y, 13, 2, 4));
            //Delta Z
            sb.Append(FLO(ae.To.Z - ae.From.Z, 13, 2, 4));
            //Actual diameter
            sb.AppendLine(FLO(ae.oDia, 13, 1, 5));

            //New line
            sb.Append(twox);
            //Wall thickness
            sb.Append(FLO(ae.WallThk, 13, 1, 5));
            //Insulation thickness
            sb.Append(FLO(ae.InsulationThk, 13, 0, 3));
            //Corrosion Allowance //TODO: Implement #$ ELEMENTS: Corrosion Allowance
            //Thermal Expansion (or Temperature #1) //TODO: Implement #$ ELEMENTS: Temperature 1-3
            sb.Append(FLO(0, 13, 0, 6, 4));
            sb.AppendLine();

            //New line
            sb.Append(twox);
            //Thermal Expansion (or Temperature #4-#9)
            sb.Append(FLO(0, 13, 0, 6, 6));
            sb.AppendLine();

            //New line
            sb.Append(twox);
            //Thermal Expansion (or Pressure #1-#6) //TODO: Implement #$ ELEMENTS: Pressure 1-6
            sb.Append(FLO(0, 13, 0, 6, 6));
            sb.AppendLine();

            //New line
            sb.Append(twox);
            //Thermal Expansion (or Pressure #7-#9)
            //Elastic Modulus (cold) //Should be specified by material
            //Poisoon's Ratio //Should be specified by material
            //Pipe Density //Should be specified by material???
            sb.Append(FLO(0, 13, 0, 6, 6));
            sb.AppendLine();

            //New line
            sb.Append(twox);
            //Insulation Density
            sb.Append(FLO(136.158, 13, 3, 3)); //TODO: Implement Insulation Density
            //Fluid Density
            sb.Append(FLO(999.556, 13, 3, 3));
            //Minus Mill Tolerance
            //Plus Mill Tolerance
            //Seam weld
            sb.Append(FLO(0, 13, 0, 6, 3));
            //Hydro Pressure
            sb.AppendLine(FLO(0, 13, 0, 6)); //TODO: Implement Hydro Pressure

            //New line
            sb.Append(twox);
            //Elastic Modulus (Hot #1-#6)
            sb.Append(FLO(0, 13, 0, 6, 6));
            sb.AppendLine();

            //New line
            sb.Append(twox);
            //Elastic Modulus (Hot #7-#9)
            //"wL" factor
            //Element Orientation Angle (To End)
            //Element Orientation Angle (From End)
            sb.Append(FLO(0, 13, 0, 6, 6));
            sb.AppendLine();

            //New line
            sb.Append(twox);
            //Cladding thickness
            sb.Append(FLO(1, 13, 0, 6));
            //Cladding Density
            sb.Append(FLO(2700, 13, 0, 6));
            //Insulation + Cladding Weight/length
            //Refractory Thickness
            //Refractory Density
            sb.Append(FLO(0, 13, 0, 6, 3));
            sb.AppendLine();

            //ELEMENT NAME
            //New line
            sb.Append(twox);
            sb.AppendLine(INT(0, 10));

            //POINTERS TO AUXILIARY DATA ARRAYS
            //New line
            sb.Append(twox);

            //Bend aux field
            if (ae.Type == ElemType.Elbow)
            {
                Data.Counter_Bends++;
                sb.Append(FLO(Data.Counter_Bends, 13, 0, 0));
                _06_BEND.Append(wBend(ae));
            }
            else sb.Append(FLO(0, 13, 0, 0));

            sb.Append(FLO(0, 13, 0, 0, 5));
            sb.AppendLine();

            sb.Append(twox);
            sb.Append(FLO(0, 13, 0, 0, 4));
            //Tee aux field
            if (ae.Type == ElemType.Tee)
            {
                Data.Counter_Intersection++;
                sb.Append(FLO(Data.Counter_Intersection, 13, 0, 0));
                _16_SIFTEES.Append(wTee(ae));
            }
            else sb.Append(FLO(0, 13, 0, 0));
            sb.Append(FLO(0, 13, 0, 0));
            sb.AppendLine();

            sb.Append(twox);
            //Reducer aux field
            if (ae.Type == ElemType.Transition)
            {
                Data.Counter_Reducers++;
                sb.Append(FLO(Data.Counter_Reducers, 13, 0, 0));
                _17_REDUCERS.Append(wReducer(ae));
            }
            else sb.Append(FLO(0, 13, 0, 0));

            sb.Append(FLO(0, 13, 0, 0, 2));
            sb.AppendLine();

            return sb;
        }

        internal static StringBuilder wTee(AnalyticElement ae)
        {
            StringBuilder sb = new StringBuilder();
            string twox = "  ";

            //New line
            sb.Append(twox);
            //Node number
            sb.Append(FLO(ae.To.Number, 13, 0, 4));
            //Tee type, 3 = welded tee - the type I use normally
            sb.Append(FLO(3, 13, 0, 5));
            sb.Append(FLO(0, 13, 0, 6, 4));
            sb.AppendLine();

            //New line
            sb.Append(twox);
            sb.Append(FLO(0, 13, 0, 6, 6));
            sb.AppendLine();

            //New line
            sb.Append(twox);
            sb.Append(FLO(0, 13, 0, 6, 6));
            sb.AppendLine();

            //New line
            sb.Append(twox);
            sb.Append(FLO(0, 13, 0, 6, 4));
            sb.Append(FLO(9999.99, 13, 2, 2, 2));
            sb.AppendLine();

            //New line
            sb.Append(twox);
            sb.Append(FLO(0, 13, 0, 6, 6));
            sb.AppendLine();

            //New line
            sb.Append(twox);
            sb.Append(FLO(0, 13, 0, 6, 6));
            sb.AppendLine();

            //New line
            sb.Append(twox);
            sb.Append(FLO(0, 13, 0, 6, 6));
            sb.AppendLine();

            //New line
            sb.Append(twox);
            sb.Append(FLO(0, 13, 0, 6, 6));
            sb.AppendLine();

            //New line
            sb.Append(twox);
            sb.Append(FLO(0, 13, 0, 6, 4));
            sb.Append(FLO(9999.99, 13, 2, 2, 2));
            sb.AppendLine();

            //New line
            sb.Append(twox);
            sb.Append(FLO(0, 13, 0, 6, 6));
            sb.AppendLine();

            return sb;
        }

        internal static StringBuilder wReducer(AnalyticElement ae)
        {
            StringBuilder sb = new StringBuilder();
            string twox = "  ";

            //New line
            sb.Append(twox);
            //Second oDia
            sb.Append(FLO(ae.secondODia, 13, 1, 3));
            //Second Wthk
            sb.Append(FLO(ae.secondWallThk, 13, 1, 3));
            //Rest
            sb.Append(FLO(0, 13, 0, 6, 3));
            sb.AppendLine();

            return sb;
        }

        internal static StringBuilder wBend(AnalyticElement ae)
        {
            StringBuilder sb = new StringBuilder();
            string twox = "  ";

            //New line
            sb.Append(twox);
            //Radius
            sb.Append(FLO(ae.BendRadius, 13, 1, 3));
            sb.Append(FLO(0, 13, 0, 6));
            sb.Append("     -2.02020");
            sb.Append(FLO(ae.To.Number - 1, 13, 0, 4));
            sb.Append(FLO(0, 13, 0, 6));
            sb.Append(FLO(ae.To.Number - 2, 13, 0, 4));
            sb.AppendLine();

            //New line
            sb.Append(twox);
            sb.Append(FLO(0, 13, 0, 6, 3));
            //Wallthk
            sb.Append(FLO(ae.WallThk, 13, 1, 5));
            sb.Append(FLO(0, 13, 0, 6, 2));
            sb.AppendLine();

            //New line
            sb.Append(twox);
            sb.Append(FLO(0, 13, 0, 6));
            sb.AppendLine();

            return sb;
        }

        internal static string INT(int number, int fieldWidth)
        {
            string input = number.ToString();
            string result = string.Empty;
            for (int i = 0; i < fieldWidth - input.Length; i++)
            {
                result += " ";
            }
            return result += input;
        }

        internal static string FLO<T>(T number, int fieldWidth, int significantDecimals, int numberOfDecimals, int totalNumberOfInstances = 1)
        {
            string result = string.Empty;
            if (number is double dbl)
            {
                result = dbl.Round(significantDecimals).ToString(System.Globalization.CultureInfo.InvariantCulture);
                int nrOfDigits = result.NrOfDigits();
                if (nrOfDigits < numberOfDecimals)
                {
                    if (!result.Contains('.')) result += ".";
                    int missingDigits = numberOfDecimals - nrOfDigits;
                    for (int i = 0; i < missingDigits; i++) result += "0";
                }
            }
            else if (number is int a)
            {
                result = a.ToString();
                if (numberOfDecimals > 0)
                {
                    result += ".";
                    for (int i = 0; i < numberOfDecimals; i++) result += "0";
                }
            }
            else if (number is string str) return "_PLACEHOLDER_";
            else throw new NotImplementedException();

            int delta = fieldWidth - result.Length;

            if (delta > 0) result = result.PadLeft(fieldWidth);
            else if (delta == 0)
            {
                ; //Do nothing
            }
            else result = result.Remove(result.Length + delta);

            if (totalNumberOfInstances > 1)
            {
                string singleInstance = result;
                for (int i = 1; i < totalNumberOfInstances; i++)
                {
                    result += singleInstance;
                }
            }

            return result;
        }
    }
}
