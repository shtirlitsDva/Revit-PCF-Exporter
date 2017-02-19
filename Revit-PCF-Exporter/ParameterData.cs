using System;
using System.Collections.Generic;
using Autodesk.Revit.DB;
using iv = PCF_Functions.InputVars;
using pd = PCF_Functions.ParameterData;
using pdef = PCF_Functions.ParameterDefinition;

namespace PCF_Functions
{
    public class ParameterDefinition
    {
        public ParameterDefinition(string pName, string pDomain, string pUsage, ParameterType pType, Guid pGuid, string pKeyword)
        {
            Name = pName;
            Domain = pDomain;
            Usage = pUsage; //U = user, P = programmatic
            Type = pType;
            Guid = pGuid;
            Keyword = pKeyword;
        }

        public ParameterDefinition(string pName, string pDomain, string pUsage, ParameterType pType, Guid pGuid, string pKeyword, string pExportingTo)
        {
            Name = pName;
            Domain = pDomain;
            Usage = pUsage; //U = user, P = programmatic
            Type = pType;
            Guid = pGuid;
            Keyword = pKeyword;
            ExportingTo = pExportingTo;
        }

        public string Name { get; }
        public string Domain { get; } //PIPL = Pipeline, ELEM = Element, SUPP = Support.
        public string Usage { get; } //U = user defined values, P = programatically defined values.
        public ParameterType Type { get; }
        public Guid Guid { get; }
        public string Keyword { get; } //The keyword as defined in the PCF reference guide.
        public string ExportingTo { get; } = null; //Currently used with CII export to distinguish CII parameters from other PIPL parameters.
    }

    public class ParameterList
    {
        public readonly IList<pdef> ListParametersAll = new List<pdef>();

        #region Parameter Definition
        //Element parameters user defined
        public readonly pdef PCF_ELEM_TYPE = new pdef("PCF_ELEM_TYPE", "ELEM", "U", pd.Text, new Guid("bfc7b779-786d-47cd-9194-8574a5059ec8"), "");
        public readonly pdef PCF_ELEM_SKEY = new pdef("PCF_ELEM_SKEY", "ELEM", "U", pd.Text, new Guid("3feebd29-054c-4ce8-bc64-3cff75ed6121"), "SKEY");
        public readonly pdef PCF_ELEM_SPEC = new pdef("PCF_ELEM_SPEC", "ELEM", "U", pd.Text, new Guid("90be8246-25f7-487d-b352-554f810fcaa7"), "PIPING-SPEC");
        //public readonly pdef PCF_ELEM_CATEGORY = new pdef("PCF_ELEM_CATEGORY", "ELEM", "U", pd.Text, new Guid("35efc6ed-2f20-4aca-bf05-d81d3b79dce2"), "CATEGORY");
        public readonly pdef PCF_ELEM_END1 = new pdef("PCF_ELEM_END1", "ELEM", "U", pd.Text, new Guid("cbc10825-c0a1-471e-9902-075a41533738"), "");
        public readonly pdef PCF_ELEM_END2 = new pdef("PCF_ELEM_END2", "ELEM", "U", pd.Text, new Guid("ecaf3f8a-c28b-4a89-8496-728af3863b09"), "");
        public readonly pdef PCF_ELEM_END3 = new pdef("PCF_ELEM_END3", "ELEM", "U", pd.Text, new Guid("501E24A0-C23A-43EE-94A0-F6D17960CB78"), "");
        public readonly pdef PCF_ELEM_BP1 = new pdef("PCF_ELEM_BP1", "ELEM", "U", pd.Text, new Guid("89b1e62e-f9b8-48c3-ab3a-1861a772bda8"), "");
        //public readonly pdef PCF_ELEM_STATUS = new pdef("PCF_ELEM_STATUS", "ELEM", "U", pd.Text, new Guid("c16e4db2-15e8-41ac-9b8f-134e133df8a4"), "STATUS");
        //public readonly pdef PCF_ELEM_TRACING_SPEC = new pdef("PCF_ELEM_TRACING_SPEC", "ELEM", "U", pd.Text, new Guid("8e1d43fb-9cd2-4591-a1f5-ba392f0a8708"), "TRACING-SPEC");
        //public readonly pdef PCF_ELEM_INSUL_SPEC = new pdef("PCF_ELEM_INSUL_SPEC", "ELEM", "U", pd.Text, new Guid("d628605e-c0bf-43dc-9f05-e22dbae2022e"), "INSULATION-SPEC");
        //public readonly pdef PCF_ELEM_PAINT_SPEC = new pdef("PCF_ELEM_PAINT_SPEC", "ELEM", "U", pd.Text, new Guid("b51db394-85ee-43af-9117-bb255ac0aaac"), "PAINTING-SPEC");
        //public readonly pdef PCF_ELEM_MISC1 = new pdef("PCF_ELEM_MISC1", "ELEM", "U", pd.Text, new Guid("ea4315ce-e5f5-4538-a6e9-f548068c3c66"), "MISC-SPEC1");
        //public readonly pdef PCF_ELEM_MISC2 = new pdef("PCF_ELEM_MISC2", "ELEM", "U", pd.Text, new Guid("cca78e21-5ed7-44bc-9dab-844997a1b965"), "MISC-SPEC2");
        //public readonly pdef PCF_ELEM_MISC3 = new pdef("PCF_ELEM_MISC3", "ELEM", "U", pd.Text, new Guid("0e065f3e-83c8-44c8-a1cb-babaf20476b9"), "MISC-SPEC3");
        //public readonly pdef PCF_ELEM_MISC4 = new pdef("PCF_ELEM_MISC4", "ELEM", "U", pd.Text, new Guid("3229c505-3802-416c-bf04-c109f41f3ab7"), "MISC-SPEC4");
        //public readonly pdef PCF_ELEM_MISC5 = new pdef("PCF_ELEM_MISC5", "ELEM", "U", pd.Text, new Guid("692e2e97-3b9c-4616-8a03-dfd493b01762"), "MISC-SPEC5");

        //Material
        public readonly pdef PCF_MAT_DESCR = new pdef("PCF_MAT_DESCR", "ELEM", "U", pd.Text, new Guid("d39418f2-fcb3-4dd1-b0be-3d647486ebe6"), "");

        //Programattically defined
        public readonly pdef PCF_ELEM_TAP1 = new pdef("PCF_ELEM_TAP1", "ELEM", "P", pd.Text, new Guid("5fda303c-5536-429b-9fcc-afb40d14c7b3"), "");
        public readonly pdef PCF_ELEM_TAP2 = new pdef("PCF_ELEM_TAP2", "ELEM", "P", pd.Text, new Guid("e1e9bc3b-ce75-4f3a-ae43-c270f4fde937"), "");
        public readonly pdef PCF_ELEM_TAP3 = new pdef("PCF_ELEM_TAP3", "ELEM", "P", pd.Text, new Guid("12693653-8029-4743-be6a-310b1fbc0620"), "");
        public readonly pdef PCF_ELEM_COMPID = new pdef("PCF_ELEM_COMPID", "ELEM", "P", pd.Integer, new Guid("876d2334-f860-4b5a-8c24-507e2c545fc0"), "");
        public readonly pdef PCF_MAT_ID = new pdef("PCF_MAT_ID", "ELEM", "P", pd.Integer, new Guid("fc5d3b19-af5b-47f6-a269-149b701c9364"), "MATERIAL-IDENTIFIER");

        //Pipeline parameters
        public readonly pdef PCF_PIPL_AREA = new pdef("PCF_PIPL_AREA", "PIPL", "U", pd.Text, new Guid("642e8ab1-f87d-4da6-894e-a007a4a186a6"), "AREA");
        public readonly pdef PCF_PIPL_DATE = new pdef("PCF_PIPL_DATE", "PIPL", "U", pd.Text, new Guid("86dc9abf-80fa-4c87-8079-4a28824ff529"), "DATE-DMY");
        //public readonly pdef PCF_PIPL_GRAV = new pdef("PCF_PIPL_GRAV", "PIPL", "U", pd.Text, new Guid("a32c0713-a6a5-4e6c-9a6b-d96e82159611"), "SPECIFIC-GRAVITY");
        //public readonly pdef PCF_PIPL_INSUL = new pdef("PCF_PIPL_INSUL", "PIPL", "U", pd.Text, new Guid("d0c429fe-71db-4adc-b54a-58ae2fb4e127"), "INSULATION-SPEC");
        //public readonly pdef PCF_PIPL_JACKET = new pdef("PCF_PIPL_JACKET", "PIPL", "U", pd.Text, new Guid("a810b6b8-17da-4191-b408-e046c758b289"), "JACKET-SPEC");
        //public readonly pdef PCF_PIPL_MISC1 = new pdef("PCF_PIPL_MISC1", "PIPL", "U", pd.Text, new Guid("22f1dbed-2978-4474-9a8a-26fd14bc6aac"), "MISC-SPEC1");
        //public readonly pdef PCF_PIPL_MISC2 = new pdef("PCF_PIPL_MISC2", "PIPL", "U", pd.Text, new Guid("6492e7d8-cbc3-42f8-86c0-0ba9000d65ca"), "MISC-SPEC2");
        //public readonly pdef PCF_PIPL_MISC3 = new pdef("PCF_PIPL_MISC3", "PIPL", "U", pd.Text, new Guid("680bac72-0a1c-44a9-806d-991401f71912"), "MISC-SPEC3");
        //public readonly pdef PCF_PIPL_MISC4 = new pdef("PCF_PIPL_MISC4", "PIPL", "U", pd.Text, new Guid("6f904559-568b-4eff-a016-9c81e3a6c3ab"), "MISC-SPEC4");
        //public readonly pdef PCF_PIPL_MISC5 = new pdef("PCF_PIPL_MISC5", "PIPL", "U", pd.Text, new Guid("c375351b-b585-4fb1-92f7-abcdc10fd53a"), "MISC-SPEC5");
        //public readonly pdef PCF_PIPL_NOMCLASS = new pdef("PCF_PIPL_NOMCLASS", "PIPL", "U", pd.Text, new Guid("998fa331-7f38-4129-9939-8495fcd6c3ae"), "NOMINAL-CLASS");
        //public readonly pdef PCF_PIPL_PAINT = new pdef("PCF_PIPL_PAINT", "PIPL", "U", pd.Text, new Guid("e440ed45-ce29-4b42-9a48-238b62b7522e"), "PAINTING-SPEC");
        //public readonly pdef PCF_PIPL_PREFIX = new pdef("PCF_PIPL_PREFIX", "PIPL", "U", pd.Text, new Guid("c7136bbc-4b0d-47c6-95d1-8623ad015e8f"), "SPOOL-PREFIX");
        public readonly pdef PCF_PIPL_PROJID = new pdef("PCF_PIPL_PROJID", "PIPL", "U", pd.Text, new Guid("50509d7f-1b99-45f9-9b24-0c423dff5078"), "PROJECT-IDENTIFIER");
        public readonly pdef PCF_PIPL_REV = new pdef("PCF_PIPL_REV", "PIPL", "U", pd.Text, new Guid("fb1a5913-4c64-4bfe-b50a-a8243a5db89f"), "REVISION");
        public readonly pdef PCF_PIPL_SPEC = new pdef("PCF_PIPL_SPEC", "PIPL", "U", pd.Text, new Guid("7b0c932b-2ebe-495f-9d2e-effc350e8a59"), "PIPING-SPEC");
        //public readonly pdef PCF_PIPL_TEMP = new pdef("PCF_PIPL_TEMP", "PIPL", "U", pd.Text, new Guid("7efb37ee-b1a1-4766-bb5b-015f823f36e2"), "PIPELINE-TEMP");
        //public readonly pdef PCF_PIPL_TRACING = new pdef("PCF_PIPL_TRACING", "PIPL", "U", pd.Text, new Guid("9d463d11-c9e8-4160-ac55-578795d11b1d"), "TRACING-SPEC");
        //public readonly pdef PCF_PIPL_TYPE = new pdef("PCF_PIPL_TYPE", "PIPL", "U", pd.Text, new Guid("af00ee7d-cfc0-4e1c-a2cf-1626e4bb7eb0"), "PIPELINE-TYPE");
        public readonly pdef PCF_PIPL_DWGNAME = new pdef("PCF_PIPL_DWGNAME","PIPL","U",pd.Text, new Guid("8FFFE45B-E8CD-4781-84DF-E86EFC58ACD0"), "DRAWINGNAME", "ISO");
        public readonly pdef PCF_PIPL_AT01 = new pdef("PCF_PIPL_AT01", "PIPL", "U", pd.Text, new Guid("718A6AF7-8068-40C0-94FD-B2D387DBBE87"), "Attribute1", "ISO");
        public readonly pdef PCF_PIPL_AT02 = new pdef("PCF_PIPL_AT02", "PIPL", "U", pd.Text, new Guid("DCD496AC-ACFD-4492-A7D9-EBC562559E2A"), "Attribute2", "ISO");
        public readonly pdef PCF_PIPL_AT03 = new pdef("PCF_PIPL_AT03", "PIPL", "U", pd.Text, new Guid("A0A067D4-49BB-4E4B-91FB-F30F9E189005"), "Attribute3", "ISO");
        public readonly pdef PCF_PIPL_AT04 = new pdef("PCF_PIPL_AT04", "PIPL", "U", pd.Text, new Guid("D40703F1-DA19-4FDC-AD1D-28180786A40A"), "Attribute4", "ISO");
        public readonly pdef PCF_PIPL_AT05 = new pdef("PCF_PIPL_AT05", "PIPL", "U", pd.Text, new Guid("8A1624ED-B59A-47B4-A99D-D45C0254224F"), "Attribute5", "ISO");
        public readonly pdef PCF_PIPL_AT06 = new pdef("PCF_PIPL_AT06", "PIPL", "U", pd.Text, new Guid("FFE67C20-324D-4997-A7C8-F48AE570A38B"), "Attribute6", "ISO");
        public readonly pdef PCF_PIPL_AT07 = new pdef("PCF_PIPL_AT07", "PIPL", "U", pd.Text, new Guid("03EC10D4-4837-4C3A-8712-7D140A325D7D"), "Attribute7", "ISO");
        public readonly pdef PCF_PIPL_AT08 = new pdef("PCF_PIPL_AT08", "PIPL", "U", pd.Text, new Guid("94E807D8-536B-4FDC-900A-A14DB8363A93"), "Attribute8", "ISO");
        public readonly pdef PCF_PIPL_AT09 = new pdef("PCF_PIPL_AT09", "PIPL", "U", pd.Text, new Guid("0F58336D-F358-4F2F-B13B-515496A28124"), "Attribute9", "ISO");

        //Parameters to facilitate export of data to CII
        public readonly pdef PCF_PIPL_CII_PD = new pdef("PCF_PIPL_CII_PD", "PIPL", "U", pd.Text, new Guid("692e2e97-3b9c-4616-8a03-daa493b01760"), "COMPONENT-ATTRIBUTE1", "CII"); //Design pressure
        public readonly pdef PCF_PIPL_CII_TD = new pdef("PCF_PIPL_CII_TD", "PIPL", "U", pd.Text, new Guid("692e2e97-3b9c-4616-8a03-daa493b01761"), "COMPONENT-ATTRIBUTE2", "CII"); //Max temperature
        public readonly pdef PCF_PIPL_CII_MATNAME = new pdef("PCF_PIPL_CII_MATNAME", "PIPL", "U", pd.Text, new Guid("692e2e97-3b9c-4616-8a03-daa493b01762"), "COMPONENT-ATTRIBUTE3", "CII"); //Material name
        public readonly pdef PCF_ELEM_CII_WALLTHK = new pdef("PCF_ELEM_CII_WALLTHK", "ELEM", "U", pd.Text, new Guid("692e2e97-3b9c-4616-8a03-daa493b01763"), "COMPONENT-ATTRIBUTE4", "CII"); //Wall thickness
        public readonly pdef PCF_PIPL_CII_INSULTHK = new pdef("PCF_PIPL_CII_INSULTHK", "PIPL", "U", pd.Text, new Guid("692e2e97-3b9c-4616-8a03-daa493b01764"), "COMPONENT-ATTRIBUTE5", "CII"); //Insulation thickness
        public readonly pdef PCF_PIPL_CII_INSULDST = new pdef("PCF_PIPL_CII_INSULDST", "PIPL", "U", pd.Text, new Guid("692e2e97-3b9c-4616-8a03-daa493b01765"), "COMPONENT-ATTRIBUTE6", "CII"); //Insulation density
        public readonly pdef PCF_PIPL_CII_CORRALL = new pdef("PCF_PIPL_CII_CORRALL", "PIPL", "U", pd.Text, new Guid("692e2e97-3b9c-4616-8a03-daa493b01766"), "COMPONENT-ATTRIBUTE7", "CII"); //Corrosion allowance
        public readonly pdef PCF_ELEM_CII_COMPWEIGHT = new pdef("PCF_ELEM_CII_COMPWEIGHT", "ELEM", "U", pd.Text, new Guid("692e2e97-3b9c-4616-8a03-daa493b01767"), "COMPONENT-ATTRIBUTE8", "CII"); //Component weight
        public readonly pdef PCF_PIPL_CII_FLUIDDST = new pdef("PCF_PIPL_CII_FLUIDDST", "PIPL", "U", pd.Text, new Guid("692e2e97-3b9c-4616-8a03-daa493b01768"), "COMPONENT-ATTRIBUTE9", "CII"); //Fluid density
        public readonly pdef PCF_PIPL_CII_HYDROPD = new pdef("PCF_PIPL_CII_HYDROPD", "PIPL", "U", pd.Text, new Guid("692e2e97-3b9c-4616-8a03-daa493b01769"), "COMPONENT-ATTRIBUTE10", "CII"); //Hydro test pressure

        //Pipe Support parameters
        public readonly pdef PCF_ELEM_SUPPORT_NAME = new pdef("PCF_ELEM_SUPPORT_NAME", "ELEM", "U", pd.Text, new Guid("25F67960-3134-4288-B8A1-C1854CF266C5"), "NAME");
        #endregion

        public ParameterList()
        {
            #region ListParametersAll
            //Populate the list with element parameters
            ListParametersAll.Add(PCF_ELEM_TYPE);
            ListParametersAll.Add(PCF_ELEM_SKEY);
            ListParametersAll.Add(PCF_ELEM_SPEC);
            //ListParametersAll.Add(PCF_ELEM_CATEGORY);
            ListParametersAll.Add(PCF_ELEM_END1);
            ListParametersAll.Add(PCF_ELEM_END2);
            ListParametersAll.Add(PCF_ELEM_END3);
            ListParametersAll.Add(PCF_ELEM_BP1);
            //ListParametersAll.Add(PCF_ELEM_STATUS);
            //ListParametersAll.Add(PCF_ELEM_TRACING_SPEC);
            //ListParametersAll.Add(PCF_ELEM_INSUL_SPEC);
            //ListParametersAll.Add(PCF_ELEM_PAINT_SPEC);
            //ListParametersAll.Add(PCF_ELEM_MISC1);
            //ListParametersAll.Add(PCF_ELEM_MISC2);
            //ListParametersAll.Add(PCF_ELEM_MISC3);
            //ListParametersAll.Add(PCF_ELEM_MISC4);
            //ListParametersAll.Add(PCF_ELEM_MISC5);

            ListParametersAll.Add(PCF_MAT_DESCR);

            ListParametersAll.Add(PCF_ELEM_TAP1);
            ListParametersAll.Add(PCF_ELEM_TAP2);
            ListParametersAll.Add(PCF_ELEM_TAP3);
            ListParametersAll.Add(PCF_ELEM_COMPID);
            ListParametersAll.Add(PCF_MAT_ID);

            //Populate the list with pipeline parameters
            ListParametersAll.Add(PCF_PIPL_AREA);
            ListParametersAll.Add(PCF_PIPL_DATE);
            //ListParametersAll.Add(PCF_PIPL_GRAV);
            //ListParametersAll.Add(PCF_PIPL_INSUL);
            //ListParametersAll.Add(PCF_PIPL_JACKET);
            //ListParametersAll.Add(PCF_PIPL_MISC1);
            //ListParametersAll.Add(PCF_PIPL_MISC2);
            //ListParametersAll.Add(PCF_PIPL_MISC3);
            //ListParametersAll.Add(PCF_PIPL_MISC4);
            //ListParametersAll.Add(PCF_PIPL_MISC5);
            //ListParametersAll.Add(PCF_PIPL_NOMCLASS);
            //ListParametersAll.Add(PCF_PIPL_PAINT);
            //ListParametersAll.Add(PCF_PIPL_PREFIX);
            ListParametersAll.Add(PCF_PIPL_PROJID);
            ListParametersAll.Add(PCF_PIPL_REV);
            ListParametersAll.Add(PCF_PIPL_SPEC);
            //ListParametersAll.Add(PCF_PIPL_TEMP);
            //ListParametersAll.Add(PCF_PIPL_TRACING);
            //ListParametersAll.Add(PCF_PIPL_TYPE);
            ListParametersAll.Add(PCF_PIPL_DWGNAME);
            ListParametersAll.Add(PCF_PIPL_AT01);
            ListParametersAll.Add(PCF_PIPL_AT02);
            ListParametersAll.Add(PCF_PIPL_AT03);
            ListParametersAll.Add(PCF_PIPL_AT04);
            ListParametersAll.Add(PCF_PIPL_AT05);
            ListParametersAll.Add(PCF_PIPL_AT06);
            ListParametersAll.Add(PCF_PIPL_AT07);
            ListParametersAll.Add(PCF_PIPL_AT08);
            ListParametersAll.Add(PCF_PIPL_AT09);

            ListParametersAll.Add(PCF_PIPL_CII_PD);
            ListParametersAll.Add(PCF_PIPL_CII_TD);
            ListParametersAll.Add(PCF_PIPL_CII_MATNAME);
            ListParametersAll.Add(PCF_ELEM_CII_WALLTHK);
            ListParametersAll.Add(PCF_PIPL_CII_INSULTHK);
            ListParametersAll.Add(PCF_PIPL_CII_INSULDST);
            ListParametersAll.Add(PCF_PIPL_CII_CORRALL);
            ListParametersAll.Add(PCF_ELEM_CII_COMPWEIGHT);
            ListParametersAll.Add(PCF_PIPL_CII_FLUIDDST);
            ListParametersAll.Add(PCF_PIPL_CII_HYDROPD);

            ListParametersAll.Add(PCF_ELEM_SUPPORT_NAME);

            #endregion
        }
    }

    public static class ParameterData
    {
        #region Parameter Data Entry

        //general values
        public const ParameterType Text = ParameterType.Text;
        public const ParameterType Integer = ParameterType.Integer;
        #endregion

        public static IList<string> parameterNames = new List<string>();
    }
}