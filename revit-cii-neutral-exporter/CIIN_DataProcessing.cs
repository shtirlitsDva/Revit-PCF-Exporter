﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MoreLinq;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Plumbing;
using Autodesk.Revit.DB.Structure;
using Autodesk.Revit.DB.Mechanical;
using Autodesk.Revit.UI;
using CIINExporter.BuildingCoder;

using static CIINExporter.MepUtils;
using static CIINExporter.Debugger;
using static CIINExporter.Enums;
using static CIINExporter.Extensions;

namespace CIINExporter
{
    public class ModelData
    {
        public StringBuilder _01_VERSION { get; set; }
        public StringBuilder _02_CONTROL { get; set; }
        public StringBuilder _03_ELEMENTS { get; set; }
        public StringBuilder _04_AUXDATA { get; } = new StringBuilder("#$ AUX_DATA\n");
        public StringBuilder _05_NODENAME { get; set; }
        public StringBuilder _06_BEND { get; set; }
        public StringBuilder _07_RIGID { get; set; }
        public StringBuilder _08_EXPJT { get; set; }
        public StringBuilder _09_RESTRANT { get; set; }
        public StringBuilder _10_DISPLMNT { get; set; }
        public StringBuilder _11_FORCMNT { get; set; }
        public StringBuilder _12_UNIFORM { get; set; }
        public StringBuilder _13_WIND { get; set; }
        public StringBuilder _14_OFFSETS { get; set; }
        public StringBuilder _15_ALLOWBLS { get; set; }
        public StringBuilder _16_SIFTEES { get; set; }
        public StringBuilder _17_REDUCERS { get; set; }
        public StringBuilder _18_FLANGES { get; set; }
        public StringBuilder _19_EQUIPMNT { get; set; }
        public StringBuilder _20_MISCEL_1 { get; set; }
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
            sb.Append(INT(1, 13));

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

        internal static StringBuilder Section_ELEMENTS(AnalyticModel model)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("#$ ELEMENTS");

            foreach (AnalyticElement ae in model.AllAnalyticElements)
            {
                sb.Append(wElement(ae));
            }

            return sb;
        }

        internal static StringBuilder wElement(AnalyticElement ae)
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
            double dia = ae.Element.get_Parameter(BuiltInParameter.RBS_PIPE_OUTER_DIAMETER).AsDouble().FtToMm();
            sb.AppendLine(FLO(dia, 13, 1, 5));
            
            //New line
            sb.Append(twox);
            //Wall thickness
            double iDia = ae.Element.get_Parameter(BuiltInParameter.RBS_PIPE_INNER_DIAM_PARAM).AsDouble().FtToMm();
            double wallThk = (dia - iDia) / 2;
            sb.Append(FLO(wallThk, 13, 1, 5));
            //Insulation thickness
            double insThick = 0;
            Parameter parInsTypeCheck = ae.Element.get_Parameter(BuiltInParameter.RBS_REFERENCE_INSULATION_TYPE);
            if (parInsTypeCheck.HasValue)
            {
                Parameter parInsThickness = ae.Element.get_Parameter(BuiltInParameter.RBS_REFERENCE_INSULATION_THICKNESS);
                insThick = parInsThickness.AsDouble().FtToMm();
            }
            sb.Append(FLO(insThick, 13, 0, 3));
            //Corrosion Allowance
            sb.Append(FLO(0, 13, 0, 6)); //TODO: Implement #$ ELEMENTS: Corrosion Allowance
            //Thermal Expansion (or Temperature #1)
            sb.Append(FLO(0, 13, 0, 6)); //TODO: Implement #$ ELEMENTS: Temperature 1
            //Thermal Expansion (or Temperature #2)
            sb.Append(FLO(0, 13, 0, 6)); //TODO: Implement #$ ELEMENTS: Temperature 2
            //Thermal Expansion (or Temperature #3)
            sb.AppendLine(FLO(0, 13, 0, 6)); //TODO: Implement #$ ELEMENTS: Temperature 3

            //New line
            sb.Append(twox);
            //Thermal Expansion (or Temperature #4)
            sb.Append(FLO(0, 13, 0, 6));
            //Thermal Expansion (or Temperature #5)
            sb.Append(FLO(0, 13, 0, 6));
            //Thermal Expansion (or Temperature #6)
            sb.Append(FLO(0, 13, 0, 6));
            //Thermal Expansion (or Temperature #7)
            sb.Append(FLO(0, 13, 0, 6));
            //Thermal Expansion (or Temperature #8)
            sb.Append(FLO(0, 13, 0, 6));
            //Thermal Expansion (or Temperature #9)
            sb.AppendLine(FLO(0, 13, 0, 6));

            //New line
            sb.Append(twox);
            //Thermal Expansion (or Pressure #1)
            sb.Append(FLO(0, 13, 0, 6)); //TODO: Implement #$ ELEMENTS: Pressure 1
            //Thermal Expansion (or Pressure #2)
            sb.Append(FLO(0, 13, 0, 6)); //TODO: Implement #$ ELEMENTS: Pressure 2
            //Thermal Expansion (or Pressure #3)
            sb.Append(FLO(0, 13, 0, 6)); //TODO: Implement #$ ELEMENTS: Pressure 3
            //Thermal Expansion (or Pressure #4)
            sb.Append(FLO(0, 13, 0, 6));
            //Thermal Expansion (or Pressure #5)
            sb.Append(FLO(0, 13, 0, 6));
            //Thermal Expansion (or Pressure #6)
            sb.AppendLine(FLO(0, 13, 0, 6));

            //New line
            sb.Append(twox);
            //Thermal Expansion (or Pressure #7)
            sb.Append(FLO(0, 13, 0, 6));
            //Thermal Expansion (or Pressure #8)
            sb.Append(FLO(0, 13, 0, 6));
            //Thermal Expansion (or Pressure #9)
            sb.Append(FLO(0, 13, 0, 6));
            //Elastic Modulus (cold)
            sb.Append(FLO(0, 13, 0, 6)); //Should be specified by material
            //Poisoon's Ratio
            sb.Append(FLO(0, 13, 0, 6)); //Should be specified by material
            //Pipe Density
            sb.AppendLine(FLO(0, 13, 0, 3)); //Should be specified by material???

            //New line
            sb.Append(twox);
            //Insulation Density
            sb.Append(FLO(136.158, 13, 3, 3)); //TODO: Implement Insulation Density
            //Fluid Density
            sb.Append(FLO(999.556, 13, 3, 3));
            //Minus Mill Tolerance
            sb.Append(FLO(0, 13, 0, 6));
            //Plus Mill Tolerance
            sb.Append(FLO(0, 13, 0, 6));
            //Seam weld
            sb.Append(FLO(0, 13, 0, 6));
            //Hydro Pressure
            sb.AppendLine(FLO(0, 13, 0, 6)); //TODO: Implement Hydro Pressure

            //New line
            sb.Append(twox);
            //Elastic Modulus (Hot #1-#6)
            sb.Append(FLO(0, 13, 0, 6, 6));
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
            else throw new NotImplementedException();

            int delta = fieldWidth - result.Length;
            
            if (delta > 0) result = result.PadLeft(fieldWidth);
            else if (delta == 0) ; //Do nothing
            else result = result.Remove(result.Length + delta);

            if (totalNumberOfInstances > 1)
            {
                string singleInstance = result;
                for (int i = 0; i < totalNumberOfInstances; i++)
                {
                    result += singleInstance;
                }
            }

            return result;
        }
    }
}
