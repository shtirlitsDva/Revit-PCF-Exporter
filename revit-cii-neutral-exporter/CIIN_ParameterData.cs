using System;
using System.Collections.Generic;
using Autodesk.Revit.DB;
using iv = CIINExporter.InputVars;
using pd = CIINExporter.ParameterData;
using pdef = CIINExporter.ParameterDefinition;

namespace CIINExporter
{
    public class ParameterDefinition
    {
        public ParameterDefinition(string pName, string pDomain, string pUsage, ForgeTypeId pType, Guid pGuid)
        {
            Name = pName;
            Domain = pDomain;
            Usage = pUsage; //U = user, P = programmatic
            Type = pType;
            Guid = pGuid;
            //Keyword = pKeyword;
        }

        public string Name { get; }
        public string Domain { get; } //PIPL = Pipeline, ELEM = Element, SUPP = Support, CTRL = Execution control.
        public string Usage { get; } //U = user defined values, P = programatically defined values.
        public ForgeTypeId Type { get; }
        public Guid Guid { get; }
        //public string Keyword { get; } //The keyword as defined in the PCF reference guide.
    }

    public class ParameterList
    {
        public readonly HashSet<pdef> LPAll = new HashSet<ParameterDefinition>();

        #region Parameter Definition
        //Element parameters user defined

        //Material

        //Programattically defined

        //Pipeline parameters
        public readonly pdef CII_PIPL_INSTHK = new pdef("CII_PIPL_INSTHK", "PIPL", "U", pd.Text, new Guid("87F7FAC4-CC0F-402A-A3AD-5FA756A5BD14"));
        public readonly pdef CII_PIPL_CORRALL = new pdef("CII_PIPL_CORRALL", "PIPL", "U", pd.Text, new Guid("C30C8AFF-97AC-4DA0-BF1E-01C23D5B1AAA"));
        public readonly pdef CII_PIPL_TEMP1 = new pdef("CII_PIPL_TEMP1", "PIPL", "U", pd.Text, new Guid("FC38DB51-715C-47B3-8BF8-C42533150B6F"));
        public readonly pdef CII_PIPL_TEMP2 = new pdef("CII_PIPL_TEMP2", "PIPL", "U", pd.Text, new Guid("A8964363-266D-4138-B675-A7E7C50F28DF"));
        public readonly pdef CII_PIPL_TEMP3 = new pdef("CII_PIPL_TEMP3", "PIPL", "U", pd.Text, new Guid("748EA684-B3FF-443A-9731-144E4075AD53"));
        public readonly pdef CII_PIPL_PRESS1 = new pdef("CII_PIPL_PRESS1", "PIPL", "U", pd.Text, new Guid("A3A0A184-EC1E-4057-8C67-4CEFC2A2C417"));
        public readonly pdef CII_PIPL_PRESS2 = new pdef("CII_PIPL_PRESS2", "PIPL", "U", pd.Text, new Guid("F1A6F2DB-3029-46A7-83FE-59F27FD55CE1"));
        public readonly pdef CII_PIPL_PRESS3 = new pdef("CII_PIPL_PRESS3", "PIPL", "U", pd.Text, new Guid("57C5D1AD-F905-4EFD-BDF2-F24E6EB1995D"));
        public readonly pdef CII_PIPL_INSDSTY = new pdef("CII_PIPL_INSDSTY", "PIPL", "U", pd.Text, new Guid("B5685712-9B58-432F-A711-AE6C31D2E82A"));
        public readonly pdef CII_PIPL_FLUIDSTY = new pdef("CII_PIPL_FLUIDSTY", "PIPL", "U", pd.Text, new Guid("C907EF13-EE11-496F-904A-EB6D5148D3A9"));
        public readonly pdef CII_PIPL_HYDRO = new pdef("CII_PIPL_HYDRO", "PIPL", "U", pd.Text, new Guid("60CC3750-A0A7-4145-A8AC-4CF56BA1235D"));
        public readonly pdef CII_PIPL_CLADTHK = new pdef("CII_PIPL_CLADTHK", "PIPL", "U", pd.Text, new Guid("8FF7151A-1A09-4BFA-8F2B-A10A45124F94"));
        public readonly pdef CII_PIPL_CLADSTY = new pdef("CII_PIPL_CLADSTY", "PIPL", "U", pd.Text, new Guid("C3E2D8B1-B593-4A61-A273-27BC01D8B948"));

        //Pipe Support parameters
        //public readonly pdef PCF_ELEM_SUPPORT_NAME = new pdef("PCF_ELEM_SUPPORT_NAME", "ELEM", "U", pd.Text, new Guid("25F67960-3134-4288-B8A1-C1854CF266C5"), "NAME");

        //Usability parameters
        public readonly pdef CII_ELEM_EXCL = new pdef("CII_ELEM_EXCL", "CTRL", "U", pd.YesNo, new Guid("0CA63F5E-6727-4390-BB7A-AD1BD577DC0F"));
        public readonly pdef CII_PIPL_EXCL = new pdef("CII_PIPL_EXCL", "CTRL", "U", pd.YesNo, new Guid("A394F73E-C734-4E7B-81E4-6615151CC4FE"));
        #endregion

        public ParameterList()
        {
            #region ListParametersAll
            //Populate the list with element parameters
            LPAll.Add(CII_PIPL_INSTHK);
            LPAll.Add(CII_PIPL_CORRALL);
            LPAll.Add(CII_PIPL_TEMP1);
            LPAll.Add(CII_PIPL_TEMP2);
            LPAll.Add(CII_PIPL_TEMP3);
            LPAll.Add(CII_PIPL_PRESS1);
            LPAll.Add(CII_PIPL_PRESS2);
            LPAll.Add(CII_PIPL_PRESS3);
            LPAll.Add(CII_PIPL_INSDSTY);
            LPAll.Add(CII_PIPL_FLUIDSTY);
            LPAll.Add(CII_PIPL_HYDRO);
            LPAll.Add(CII_PIPL_CLADTHK);
            LPAll.Add(CII_PIPL_CLADSTY);
            LPAll.Add(CII_ELEM_EXCL);
            LPAll.Add(CII_PIPL_EXCL);
            #endregion
        }
    }

    public static class ParameterData
    {
        #region Parameter Data Entry

        //general values
        public static ForgeTypeId Text = SpecTypeId.String.Text;
        public static ForgeTypeId Integer = SpecTypeId.Int.Integer;
        public static ForgeTypeId YesNo = SpecTypeId.Boolean.YesNo;
        #endregion

        public static IList<string> parameterNames = new List<string>();
    }
}