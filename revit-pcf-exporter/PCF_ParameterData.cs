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
        public ParameterDefinition(string pName, string pDomain, string pUsage, ForgeTypeId pType, Guid pGuid, string pKeyword = "")
        {
            Name = pName;
            Domain = pDomain;
            Usage = pUsage; //U = user, P = programmatic
            Type = pType;
            Guid = pGuid;
            Keyword = pKeyword;
        }

        public ParameterDefinition(string pName, string pDomain, string pUsage, ForgeTypeId pType, Guid pGuid, string pKeyword, string pExportingTo) :
            this(pName, pDomain, pUsage, pType, pGuid, pKeyword)
        {
            ExportingTo = pExportingTo;
        }

        public ParameterDefinition(string pName, string pDomain, string pUsage, ForgeTypeId pType, Guid pGuid,
            BuiltInParameterGroup pGroup) :
            this(pName, pDomain, pUsage, pType, pGuid)
        {
            ParameterGroup = pGroup;
        }

        public string Name { get; }
        public string Domain { get; } //PIPL = Pipeline, ELEM = Element, SUPP = Support, CTRL = Execution control.
        public string Usage { get; } //U = user defined values, P = programatically defined values.
        public ForgeTypeId Type { get; }
        public Guid Guid { get; }
        public string Keyword { get; } //The keyword as defined in the PCF reference guide.
        public string ExportingTo { get; } = null; //CII export to CII, LDT export to ISOGEN
        public BuiltInParameterGroup ParameterGroup { get; } = BuiltInParameterGroup.PG_ANALYTICAL_MODEL;
    }

    public static class ParameterList
    {
        public static readonly HashSet<pdef> LPAll = new HashSet<ParameterDefinition>();

        #region Parameter Definition
        //Element parameters user defined
        public static readonly pdef PCF_ELEM_TYPE = new pdef("PCF_ELEM_TYPE", "ELEM", "U", pd.Text, new Guid("bfc7b779-786d-47cd-9194-8574a5059ec8"));
        public static readonly pdef PCF_ELEM_SKEY = new pdef("PCF_ELEM_SKEY", "ELEM", "U", pd.Text, new Guid("3feebd29-054c-4ce8-bc64-3cff75ed6121"), "SKEY");
        public static readonly pdef PCF_ELEM_SPEC = new pdef("PCF_ELEM_SPEC", "ELEM", "U", pd.Text, new Guid("90be8246-25f7-487d-b352-554f810fcaa7"), "PIPING-SPEC");
        //public static readonly pdef PCF_ELEM_CATEGORY = new pdef("PCF_ELEM_CATEGORY", "ELEM", "U", pd.Text, new Guid("35efc6ed-2f20-4aca-bf05-d81d3b79dce2"), "CATEGORY");
        public static readonly pdef PCF_ELEM_END1 = new pdef("PCF_ELEM_END1", "ELEM", "U", pd.Text, new Guid("cbc10825-c0a1-471e-9902-075a41533738"));
        public static readonly pdef PCF_ELEM_END2 = new pdef("PCF_ELEM_END2", "ELEM", "U", pd.Text, new Guid("ecaf3f8a-c28b-4a89-8496-728af3863b09"));
        public static readonly pdef PCF_ELEM_END3 = new pdef("PCF_ELEM_END3", "ELEM", "U", pd.Text, new Guid("501E24A0-C23A-43EE-94A0-F6D17960CB78"));
        public static readonly pdef PCF_ELEM_BP1 = new pdef("PCF_ELEM_BP1", "ELEM", "U", pd.Text, new Guid("89b1e62e-f9b8-48c3-ab3a-1861a772bda8"));
        //public static readonly pdef PCF_ELEM_STATUS = new pdef("PCF_ELEM_STATUS", "ELEM", "U", pd.Text, new Guid("c16e4db2-15e8-41ac-9b8f-134e133df8a4"), "STATUS");
        //public static readonly pdef PCF_ELEM_TRACING_SPEC = new pdef("PCF_ELEM_TRACING_SPEC", "ELEM", "U", pd.Text, new Guid("8e1d43fb-9cd2-4591-a1f5-ba392f0a8708"), "TRACING-SPEC");
        //public static readonly pdef PCF_ELEM_INSUL_SPEC = new pdef("PCF_ELEM_INSUL_SPEC", "ELEM", "U", pd.Text, new Guid("d628605e-c0bf-43dc-9f05-e22dbae2022e"), "INSULATION-SPEC");
        //public static readonly pdef PCF_ELEM_PAINT_SPEC = new pdef("PCF_ELEM_PAINT_SPEC", "ELEM", "U", pd.Text, new Guid("b51db394-85ee-43af-9117-bb255ac0aaac"), "PAINTING-SPEC");
        //public static readonly pdef PCF_ELEM_MTLST = new pdef("PCF_ELEM_MTLST", "ELEM", "U", pd.Text, new Guid("ea4315ce-e5f5-4538-a6e9-f548068c3c66"), "MATERIAL-LIST");
        public static readonly pdef PCF_ELEM_REV = new pdef("PCF_ELEM_REV", "ELEM", "U", pd.Text, new Guid("cca78e21-5ed7-44bc-9dab-844997a1b965"), "REVISION");
        public static readonly pdef PCF_ELEM_MSG = new pdef("PCF_ELEM_MSG", "ELEM", "U", pd.Text, new Guid("61367166-7C88-436B-B089-BDA9DB571D6B"), "MESSAGE\n    TEXT");
        //public static readonly pdef PCF_ELEM_MISC3 = new pdef("PCF_ELEM_MISC3", "ELEM", "U", pd.Text, new Guid("0e065f3e-83c8-44c8-a1cb-babaf20476b9"), "MISC-SPEC3");
        //public static readonly pdef PCF_ELEM_MISC4 = new pdef("PCF_ELEM_MISC4", "ELEM", "U", pd.Text, new Guid("3229c505-3802-416c-bf04-c109f41f3ab7"), "MISC-SPEC4");
        //public static readonly pdef PCF_ELEM_MISC5 = new pdef("PCF_ELEM_MISC5", "ELEM", "U", pd.Text, new Guid("692e2e97-3b9c-4616-8a03-dfd493b01762"), "MISC-SPEC5");

        //Material
        public static readonly pdef PCF_MAT_DESCR = new pdef("PCF_MAT_DESCR", "ELEM", "U", pd.Text, new Guid("d39418f2-fcb3-4dd1-b0be-3d647486ebe6"));

        //Programattically defined
        public static readonly pdef PCF_ELEM_TAP1 = new pdef("PCF_ELEM_TAP1", "ELEM", "P", pd.Text, new Guid("5fda303c-5536-429b-9fcc-afb40d14c7b3"));
        public static readonly pdef PCF_ELEM_TAP2 = new pdef("PCF_ELEM_TAP2", "ELEM", "P", pd.Text, new Guid("e1e9bc3b-ce75-4f3a-ae43-c270f4fde937"));
        public static readonly pdef PCF_ELEM_TAP3 = new pdef("PCF_ELEM_TAP3", "ELEM", "P", pd.Text, new Guid("12693653-8029-4743-be6a-310b1fbc0620"));
        public static readonly pdef PCF_ELEM_COMPID = new pdef("PCF_ELEM_COMPID", "ELEM", "P", pd.Text, new Guid("4EAB3C7A-C0BF-4E9A-B7B9-978407C12800"));
        public static readonly pdef PCF_MAT_ID = new pdef("PCF_MAT_ID", "ELEM", "P", pd.Text, new Guid("DE851B73-AFEA-4B38-9BCB-EFC8CBA78B16"), "MATERIAL-IDENTIFIER");

        //Pipeline parameters
        public static readonly pdef PCF_PIPL_LINEID = new pdef("PCF_LINEID", "PIPL", "U", pd.Text, new Guid("A12D0564-A8D9-451A-9800-5704EB1E7B75"), "LINE-ID");
        public static readonly pdef PCF_PIPL_AREA = new pdef("PCF_AREA", "PIPL", "U", pd.Text, new Guid("DE15EB1B-CFD1-418B-80F2-D24D321130BC"), "AREA");
        public static readonly pdef PCF_PIPL_DATE = new pdef("PCF_DATE", "PIPL", "U", pd.Text, new Guid("6F6D1903-CC39-4353-85ED-0AA5AEF1C815"), "DATE-DMY");
        //public static readonly pdef PCF_PIPL_GRAV = new pdef("PCF_PIPL_GRAV", "PIPL", "U", pd.Text, new Guid("a32c0713-a6a5-4e6c-9a6b-d96e82159611"), "SPECIFIC-GRAVITY");
        //public static readonly pdef PCF_PIPL_INSUL = new pdef("PCF_PIPL_INSUL", "PIPL", "U", pd.Text, new Guid("d0c429fe-71db-4adc-b54a-58ae2fb4e127"), "INSULATION-SPEC");
        //public static readonly pdef PCF_PIPL_JACKET = new pdef("PCF_PIPL_JACKET", "PIPL", "U", pd.Text, new Guid("a810b6b8-17da-4191-b408-e046c758b289"), "JACKET-SPEC");
        //public static readonly pdef PCF_PIPL_MISC1 = new pdef("PCF_PIPL_MISC1", "PIPL", "U", pd.Text, new Guid("22f1dbed-2978-4474-9a8a-26fd14bc6aac"), "MISC-SPEC1");
        //public static readonly pdef PCF_PIPL_MISC2 = new pdef("PCF_PIPL_MISC2", "PIPL", "U", pd.Text, new Guid("6492e7d8-cbc3-42f8-86c0-0ba9000d65ca"), "MISC-SPEC2");
        //public static readonly pdef PCF_PIPL_MISC3 = new pdef("PCF_PIPL_MISC3", "PIPL", "U", pd.Text, new Guid("680bac72-0a1c-44a9-806d-991401f71912"), "MISC-SPEC3");
        //public static readonly pdef PCF_PIPL_MISC4 = new pdef("PCF_PIPL_MISC4", "PIPL", "U", pd.Text, new Guid("6f904559-568b-4eff-a016-9c81e3a6c3ab"), "MISC-SPEC4");
        //public static readonly pdef PCF_PIPL_MISC5 = new pdef("PCF_PIPL_MISC5", "PIPL", "U", pd.Text, new Guid("c375351b-b585-4fb1-92f7-abcdc10fd53a"), "MISC-SPEC5");
        public static readonly pdef PCF_PIPL_NOMCLASS = new pdef("PCF_NOMCLASS", "PIPL", "U", pd.Text, new Guid("A5CB2A32-19E2-4536-838C-BE5253A5D301"), "NOMINAL-CLASS");
        //public static readonly pdef PCF_PIPL_PAINT = new pdef("PCF_PIPL_PAINT", "PIPL", "U", pd.Text, new Guid("e440ed45-ce29-4b42-9a48-238b62b7522e"), "PAINTING-SPEC");
        //public static readonly pdef PCF_PIPL_PREFIX = new pdef("PCF_PIPL_PREFIX", "PIPL", "U", pd.Text, new Guid("c7136bbc-4b0d-47c6-95d1-8623ad015e8f"), "SPOOL-PREFIX");
        public static readonly pdef PCF_PIPL_PROJID = new pdef("PCF_PROJID", "PIPL", "U", pd.Text, new Guid("EBFC0D5E-170B-41E7-B8B3-CF68D8402773"), "PROJECT-IDENTIFIER");
        public static readonly pdef PCF_PIPL_REV = new pdef("PCF_REV", "PIPL", "U", pd.Text, new Guid("17DA8F27-5B5F-4C18-BA13-30CDD07E60DC"), "REVISION");
        public static readonly pdef PCF_PIPL_SPEC = new pdef("PCF_PIPL_SPEC", "PIPL", "U", pd.Text, new Guid("7b0c932b-2ebe-495f-9d2e-effc350e8a59"), "PIPING-SPEC");
        public static readonly pdef PCF_PIPL_TEMP = new pdef("PCF_TEMP", "PIPL", "U", pd.Text, new Guid("4CF40C73-7631-40B3-BA98-03994F3E0FD0"), "PIPELINE-TEMP");
        //public static readonly pdef PCF_PIPL_TRACING = new pdef("PCF_PIPL_TRACING", "PIPL", "U", pd.Text, new Guid("9d463d11-c9e8-4160-ac55-578795d11b1d"), "TRACING-SPEC");
        //public static readonly pdef PCF_PIPL_TYPE = new pdef("PCF_PIPL_TYPE", "PIPL", "U", pd.Text, new Guid("af00ee7d-cfc0-4e1c-a2cf-1626e4bb7eb0"), "PIPELINE-TYPE");
        public static readonly pdef PCF_PIPL_DWGNAME = new pdef("PCF_DWGNAME","PIPL","U",pd.Text, new Guid("DBD769B8-2535-481A-A602-BC0B8D8C7A16"), "ATTRIBUTE4");
        public static readonly pdef PCF_PIPL_NDTMSG = new pdef("PCF_NDTMSG", "PIPL", "U", pd.Text, new Guid("C910ACC3-953B-4059-8CB8-4F40905BD837"), "ATTRIBUTE5", "LDT");
        public static readonly pdef PCF_PIPL_CUTOFF = new pdef("PCF_CUTOFF", "PIPL", "U", pd.Text, new Guid("22F17C99-727D-404D-AACD-151A9AEA6728"), "ATTRIBUTE6", "LDT");
        public static readonly pdef PCF_PIPL_TEGN = new pdef("PCF_TEGN", "PIPL", "U", pd.Text, new Guid("718A6AF7-8068-40C0-94FD-B2D387DBBE87"), "ATTRIBUTE7");
        public static readonly pdef PCF_PIPL_KONTR = new pdef("PCF_KONTR", "PIPL", "U", pd.Text, new Guid("DCD496AC-ACFD-4492-A7D9-EBC562559E2A"), "ATTRIBUTE8");
        public static readonly pdef PCF_PIPL_GODK = new pdef("PCF_GODK", "PIPL", "U", pd.Text, new Guid("A0A067D4-49BB-4E4B-91FB-F30F9E189005"), "ATTRIBUTE9");
        public static readonly pdef PCF_PIPL_AT11 = new pdef("PCF_REVA", "PIPL", "U", pd.Text, new Guid("C7780B12-8CBD-41D6-2534-7F99C6510910"), "ATTRIBUTE11", "LDT");
        public static readonly pdef PCF_PIPL_AT12 = new pdef("PCF_ADESCR", "PIPL", "U", pd.Text, new Guid("173B1840-A680-5488-6694-9AF103346395"), "ATTRIBUTE12", "LDT");
        public static readonly pdef PCF_PIPL_AT13 = new pdef("PCF_ADATO", "PIPL", "U", pd.Text, new Guid("BDF51F06-5157-9088-9A15-995664CF319C"), "ATTRIBUTE13", "LDT");
        public static readonly pdef PCF_PIPL_AT14 = new pdef("PCF_ADRWN", "PIPL", "U", pd.Text, new Guid("1B2D7F77-6255-068C-169A-BEA8424923C4"), "ATTRIBUTE14", "LDT");
        public static readonly pdef PCF_PIPL_AT15 = new pdef("PCF_AKONTR", "PIPL", "U", pd.Text, new Guid("079274BA-01F5-3C15-7E71-82DCCA15402F"), "ATTRIBUTE15", "LDT");
        public static readonly pdef PCF_PIPL_AT16 = new pdef("PCF_AGODK", "PIPL", "U", pd.Text, new Guid("8BD6ADC2-9AE2-4011-2187-4DBC355C7623"), "ATTRIBUTE16", "LDT");
        public static readonly pdef PCF_PIPL_AT17 = new pdef("PCF_REVB", "PIPL", "U", pd.Text, new Guid("F2BEA8F2-9B7A-77CC-00C1-5FE098098A53"), "ATTRIBUTE17", "LDT");
        public static readonly pdef PCF_PIPL_AT18 = new pdef("PCF_BDESCR", "PIPL", "U", pd.Text, new Guid("8C864664-2F15-31ED-A54A-FAD70E77829C"), "ATTRIBUTE18", "LDT");
        public static readonly pdef PCF_PIPL_AT19 = new pdef("PCF_BDATO", "PIPL", "U", pd.Text, new Guid("6358B8E3-1BD3-4EC0-67AB-232EBB127635"), "ATTRIBUTE19", "LDT");
        public static readonly pdef PCF_PIPL_AT20 = new pdef("PCF_BDRWN", "PIPL", "U", pd.Text, new Guid("55B5AF79-124C-A5E0-2A1C-D917F11A1574"), "ATTRIBUTE20", "LDT");
        public static readonly pdef PCF_PIPL_AT21 = new pdef("PCF_BKONTR", "PIPL", "U", pd.Text, new Guid("AE5A4402-33F0-518B-7810-90688BD20389"), "ATTRIBUTE21", "LDT");
        public static readonly pdef PCF_PIPL_AT22 = new pdef("PCF_BGODK", "PIPL", "U", pd.Text, new Guid("E3AA91E9-88F1-6273-1A24-7072C4BD12CE"), "ATTRIBUTE22", "LDT");
        public static readonly pdef PCF_PIPL_AT23 = new pdef("PCF_REVC", "PIPL", "U", pd.Text, new Guid("85338580-4E9F-4490-47E1-4A123F325871"), "ATTRIBUTE23", "LDT");
        public static readonly pdef PCF_PIPL_AT24 = new pdef("PCF_CDESCR", "PIPL", "U", pd.Text, new Guid("CAC034B5-8CC2-36B3-7196-BBEAB5B03D62"), "ATTRIBUTE24", "LDT");
        public static readonly pdef PCF_PIPL_AT25 = new pdef("PCF_CDATO", "PIPL", "U", pd.Text, new Guid("21D7BA7D-4511-7994-82A8-8BB1827F0DA9"), "ATTRIBUTE25", "LDT");
        public static readonly pdef PCF_PIPL_AT26 = new pdef("PCF_CDRWN", "PIPL", "U", pd.Text, new Guid("69CE6D6A-67E7-9452-73B1-7AA619818FD3"), "ATTRIBUTE26", "LDT");
        public static readonly pdef PCF_PIPL_AT27 = new pdef("PCF_CKONTR", "PIPL", "U", pd.Text, new Guid("528988A7-1CB1-0255-46AD-A15DB6ED84E9"), "ATTRIBUTE27", "LDT");
        public static readonly pdef PCF_PIPL_AT28 = new pdef("PCF_CGODK", "PIPL", "U", pd.Text, new Guid("E7EFD4A9-57C5-70BC-1B8D-42455B922C7D"), "ATTRIBUTE28", "LDT");
        public static readonly pdef PCF_PIPL_AT29 = new pdef("PCF_REVD", "PIPL", "U", pd.Text, new Guid("38621431-31DF-9335-591B-90B59CDC4D66"), "ATTRIBUTE29", "LDT");
        public static readonly pdef PCF_PIPL_AT30 = new pdef("PCF_DDESCR", "PIPL", "U", pd.Text, new Guid("105EF16F-2A56-1B9D-6A76-FE97D86D6BE9"), "ATTRIBUTE30", "LDT");
        public static readonly pdef PCF_PIPL_AT31 = new pdef("PCF_DDATO", "PIPL", "U", pd.Text, new Guid("1F4178A3-85BC-601D-38AF-082F4B5181A6"), "ATTRIBUTE31", "LDT");
        public static readonly pdef PCF_PIPL_AT32 = new pdef("PCF_DDRWN", "PIPL", "U", pd.Text, new Guid("10EAD0DF-4C7A-29EB-8DCF-C38791223E32"), "ATTRIBUTE32", "LDT");
        public static readonly pdef PCF_PIPL_AT33 = new pdef("PCF_DKONTR", "PIPL", "U", pd.Text, new Guid("30BDED8B-9B34-6097-7468-9D8F9E551361"), "ATTRIBUTE33", "LDT");
        public static readonly pdef PCF_PIPL_AT34 = new pdef("PCF_DGODK", "PIPL", "U", pd.Text, new Guid("3B8A98CF-7037-5059-71CA-5FBC262724E5"), "ATTRIBUTE34", "LDT");
        public static readonly pdef PCF_PIPL_AT35 = new pdef("PCF_REVE", "PIPL", "U", pd.Text, new Guid("CD58BFC3-7B30-1342-81E8-437D86235930"), "ATTRIBUTE35", "LDT");
        public static readonly pdef PCF_PIPL_AT36 = new pdef("PCF_EDESCR", "PIPL", "U", pd.Text, new Guid("CC8588D7-0FF6-3360-7B65-282A0969A09D"), "ATTRIBUTE36", "LDT");
        public static readonly pdef PCF_PIPL_AT37 = new pdef("PCF_EDATO", "PIPL", "U", pd.Text, new Guid("DD0646F7-06A2-2FD6-2136-D76508526432"), "ATTRIBUTE37", "LDT");
        public static readonly pdef PCF_PIPL_AT38 = new pdef("PCF_EDRWN", "PIPL", "U", pd.Text, new Guid("93F0BB1D-6CB0-A608-338F-1BA8D5E152DC"), "ATTRIBUTE38", "LDT");
        public static readonly pdef PCF_PIPL_AT39 = new pdef("PCF_EKONTR", "PIPL", "U", pd.Text, new Guid("EC380881-6257-013B-9334-ABF6474E8C3F"), "ATTRIBUTE39", "LDT");
        public static readonly pdef PCF_PIPL_AT40 = new pdef("PCF_EGODK", "PIPL", "U", pd.Text, new Guid("6F4380C4-300C-9580-1D4B-492BB69B13A1"), "ATTRIBUTE40", "LDT");
        public static readonly pdef PCF_PIPL_AT41 = new pdef("PCF_REVF", "PIPL", "U", pd.Text, new Guid("07AF10BC-6F19-24C7-7039-5A85B3916C74"), "ATTRIBUTE41", "LDT");
        public static readonly pdef PCF_PIPL_AT42 = new pdef("PCF_FDESCR", "PIPL", "U", pd.Text, new Guid("FB007487-69D7-5682-61F8-D8D1203266BD"), "ATTRIBUTE42", "LDT");
        public static readonly pdef PCF_PIPL_AT43 = new pdef("PCF_FDATO", "PIPL", "U", pd.Text, new Guid("6B1AF5AB-57EA-9184-2E00-EF21DF9E3314"), "ATTRIBUTE43", "LDT");
        public static readonly pdef PCF_PIPL_AT44 = new pdef("PCF_FDRWN", "PIPL", "U", pd.Text, new Guid("5A1898C8-5085-8734-9435-29F52A275BD8"), "ATTRIBUTE44", "LDT");
        public static readonly pdef PCF_PIPL_AT45 = new pdef("PCF_FKONTR", "PIPL", "U", pd.Text, new Guid("65D7A90A-79F9-8859-6CE6-D86C373B556C"), "ATTRIBUTE45", "LDT");
        public static readonly pdef PCF_PIPL_AT46 = new pdef("PCF_FGODK", "PIPL", "U", pd.Text, new Guid("8AECB123-457F-786E-A09E-26276613295F"), "ATTRIBUTE46", "LDT");
        public static readonly pdef PCF_PIPL_AT47 = new pdef("PCF_REVG", "PIPL", "U", pd.Text, new Guid("2FE3B9B5-1EA1-1DDA-8A2E-E343A6632D1D"), "ATTRIBUTE47", "LDT");
        public static readonly pdef PCF_PIPL_AT48 = new pdef("PCF_GDESCR", "PIPL", "U", pd.Text, new Guid("33F75B5E-44B8-1DD4-3AAA-A16B91DC8C7E"), "ATTRIBUTE48", "LDT");
        public static readonly pdef PCF_PIPL_AT49 = new pdef("PCF_GDATO", "PIPL", "U", pd.Text, new Guid("D92492C8-8821-5221-A24F-05929CFF3218"), "ATTRIBUTE49", "LDT");
        public static readonly pdef PCF_PIPL_AT50 = new pdef("PCF_GDRWN", "PIPL", "U", pd.Text, new Guid("BB83F44B-0E28-72AB-160B-30CF5C746496"), "ATTRIBUTE50", "LDT");
        public static readonly pdef PCF_PIPL_AT51 = new pdef("PCF_GKONTR", "PIPL", "U", pd.Text, new Guid("678B80FC-88F1-5655-9E29-F3BA201277C3"), "ATTRIBUTE51", "LDT");
        public static readonly pdef PCF_PIPL_AT52 = new pdef("PCF_GGODK", "PIPL", "U", pd.Text, new Guid("3B140316-95CB-1069-40DA-6BC0E6060D2A"), "ATTRIBUTE52", "LDT");
        public static readonly pdef PCF_PIPL_AT53 = new pdef("PCF_REVH", "PIPL", "U", pd.Text, new Guid("BA4E2D62-3430-4F5E-3A9C-F2714FB3444C"), "ATTRIBUTE53", "LDT");
        public static readonly pdef PCF_PIPL_AT54 = new pdef("PCF_HDESCR", "PIPL", "U", pd.Text, new Guid("A654983A-76CC-42E3-47A5-4FF0F7724C4D"), "ATTRIBUTE54", "LDT");
        public static readonly pdef PCF_PIPL_AT55 = new pdef("PCF_HDATO", "PIPL", "U", pd.Text, new Guid("520AFC62-2684-3FFF-99DA-2C94832A08D5"), "ATTRIBUTE55", "LDT");
        public static readonly pdef PCF_PIPL_AT56 = new pdef("PCF_HDRWN", "PIPL", "U", pd.Text, new Guid("3183BA49-A362-56CB-1585-B01793012B00"), "ATTRIBUTE56", "LDT");
        public static readonly pdef PCF_PIPL_AT57 = new pdef("PCF_HKONTR", "PIPL", "U", pd.Text, new Guid("27B05E44-A030-01FA-201A-9E4A5BD43DF9"), "ATTRIBUTE57", "LDT");
        public static readonly pdef PCF_PIPL_AT58 = new pdef("PCF_HGODK", "PIPL", "U", pd.Text, new Guid("4218FBBA-00AA-667D-2EE4-1184404846CB"), "ATTRIBUTE58", "LDT");
        //PCF_PIPL_AT59 is taken by title block attribute SOURCE (attribute to write the file name of the source file).

        //Parameters to facilitate export of data to CII
        public static readonly pdef PCF_PIPL_CII_PD = new pdef("PCF_PIPL_CII_PD", "PIPL", "U", pd.Text, new Guid("692e2e97-3b9c-4616-8a03-daa493b01760"), "COMPONENT-ATTRIBUTE1", "CII"); //Design pressure
        public static readonly pdef PCF_PIPL_CII_TD = new pdef("PCF_PIPL_CII_TD", "PIPL", "U", pd.Text, new Guid("692e2e97-3b9c-4616-8a03-daa493b01761"), "COMPONENT-ATTRIBUTE2", "CII"); //Max temperature
        public static readonly pdef PCF_PIPL_CII_MATNAME = new pdef("PCF_PIPL_CII_MATNAME", "PIPL", "U", pd.Text, new Guid("692e2e97-3b9c-4616-8a03-daa493b01762"), "COMPONENT-ATTRIBUTE3", "CII"); //Material name
        public static readonly pdef PCF_ELEM_CII_WALLTHK = new pdef("PCF_ELEM_CII_WALLTHK", "ELEM", "U", pd.Text, new Guid("692e2e97-3b9c-4616-8a03-daa493b01763"), "COMPONENT-ATTRIBUTE4", "CII"); //Wall thickness
        public static readonly pdef PCF_PIPL_CII_INSULTHK = new pdef("PCF_PIPL_CII_INSULTHK", "PIPL", "U", pd.Text, new Guid("692e2e97-3b9c-4616-8a03-daa493b01764"), "COMPONENT-ATTRIBUTE5", "CII"); //Insulation thickness
        public static readonly pdef PCF_PIPL_CII_INSULDST = new pdef("PCF_PIPL_CII_INSULDST", "PIPL", "U", pd.Text, new Guid("692e2e97-3b9c-4616-8a03-daa493b01765"), "COMPONENT-ATTRIBUTE6", "CII"); //Insulation density
        public static readonly pdef PCF_PIPL_CII_CORRALL = new pdef("PCF_PIPL_CII_CORRALL", "PIPL", "U", pd.Text, new Guid("692e2e97-3b9c-4616-8a03-daa493b01766"), "COMPONENT-ATTRIBUTE7", "CII"); //Corrosion allowance
        public static readonly pdef PCF_ELEM_CII_COMPWEIGHT = new pdef("PCF_ELEM_CII_COMPWEIGHT", "ELEM", "U", pd.Text, new Guid("692e2e97-3b9c-4616-8a03-daa493b01767"), "COMPONENT-ATTRIBUTE8", "CII"); //Component weight
        public static readonly pdef PCF_PIPL_CII_FLUIDDST = new pdef("PCF_PIPL_CII_FLUIDDST", "PIPL", "U", pd.Text, new Guid("692e2e97-3b9c-4616-8a03-daa493b01768"), "COMPONENT-ATTRIBUTE9", "CII"); //Fluid density
        public static readonly pdef PCF_PIPL_CII_HYDROPD = new pdef("PCF_PIPL_CII_HYDROPD", "PIPL", "U", pd.Text, new Guid("692e2e97-3b9c-4616-8a03-daa493b01769"), "COMPONENT-ATTRIBUTE10", "CII"); //Hydro test pressure

        //Pipe Support parameters
        public static readonly pdef PCF_ELEM_SUPPORT_NAME = new pdef("PCF_ELEM_SUPPORT_NAME", "ELEM", "U", pd.Text, new Guid("25F67960-3134-4288-B8A1-C1854CF266C5"), "NAME");

        //Usability parameters
        public static readonly pdef PCF_ELEM_EXCL = new pdef("PCF_ELEM_EXCL", "CTRL", "U", pd.YesNo, new Guid("CC8EC292-226C-4677-A32D-10B9736BFC1A"));
        //If guid changes can break other methods!!
        //Shared.MepUtils.GetDistinctPhysicalPipingSystemTypeNames(Document doc) uses this guid!!!
        public static readonly pdef PCF_PIPL_EXCL = new pdef("PCF_PIPL_EXCL", "CTRL", "U", pd.YesNo, new Guid("C1C2C9FE-2634-42BA-89D0-5AF699F54D4C"));

        //Parameters TAGS
        public static readonly pdef TAG_1 =
            new pdef("TAG 1", "ELEM", "U", pd.Text, new Guid("a93679f7-ca9e-4a1e-bb44-0d890a5b4ba1"), BuiltInParameterGroup.PG_MECHANICAL);
        public static readonly pdef TAG_2 =
            new pdef("TAG 2", "ELEM", "U", pd.Text, new Guid("3b2afba4-447f-422a-8280-fd394718ad4e"), BuiltInParameterGroup.PG_MECHANICAL);
        public static readonly pdef TAG_3 =
            new pdef("TAG 3", "ELEM", "U", pd.Text, new Guid("5c238fab-f1b0-4946-9c92-c3037b8d3b68"), BuiltInParameterGroup.PG_MECHANICAL);
        public static readonly pdef TAG_4 =
            new pdef("TAG 4", "ELEM", "U", pd.Text, new Guid("f96a5688-8dbe-427d-aa62-f8744a6bc3ee"), BuiltInParameterGroup.PG_MECHANICAL);
        #endregion

        static ParameterList()
        {
            #region ListParametersAll
            //Populate the list with element parameters
            LPAll.Add(PCF_ELEM_TYPE);
            LPAll.Add(PCF_ELEM_SKEY);
            LPAll.Add(PCF_ELEM_SPEC);
            //ListParametersAll.Add(PCF_ELEM_CATEGORY);
            LPAll.Add(PCF_ELEM_END1);
            LPAll.Add(PCF_ELEM_END2);
            LPAll.Add(PCF_ELEM_END3);
            LPAll.Add(PCF_ELEM_BP1);
            //LPAll.Add(PCF_ELEM_STATUS);
            //ListParametersAll.Add(PCF_ELEM_TRACING_SPEC);
            //ListParametersAll.Add(PCF_ELEM_INSUL_SPEC);
            //ListParametersAll.Add(PCF_ELEM_PAINT_SPEC);
            //LPAll.Add(PCF_ELEM_MTLST);
            LPAll.Add(PCF_ELEM_REV);
            LPAll.Add(PCF_ELEM_MSG);
            //ListParametersAll.Add(PCF_ELEM_MISC3);
            //ListParametersAll.Add(PCF_ELEM_MISC4);
            //ListParametersAll.Add(PCF_ELEM_MISC5);

            LPAll.Add(PCF_MAT_DESCR);

            LPAll.Add(PCF_ELEM_TAP1);
            LPAll.Add(PCF_ELEM_TAP2);
            LPAll.Add(PCF_ELEM_TAP3);
            LPAll.Add(PCF_ELEM_COMPID);
            LPAll.Add(PCF_MAT_ID);

            //Populate the list with pipeline parameters
            LPAll.Add(PCF_PIPL_LINEID);
            LPAll.Add(PCF_PIPL_AREA);
            LPAll.Add(PCF_PIPL_DATE);
            //ListParametersAll.Add(PCF_PIPL_GRAV);
            //ListParametersAll.Add(PCF_PIPL_INSUL);
            //ListParametersAll.Add(PCF_PIPL_JACKET);
            //ListParametersAll.Add(PCF_PIPL_MISC1);
            //ListParametersAll.Add(PCF_PIPL_MISC2);
            //ListParametersAll.Add(PCF_PIPL_MISC3);
            //ListParametersAll.Add(PCF_PIPL_MISC4);
            //ListParametersAll.Add(PCF_PIPL_MISC5);
            LPAll.Add(PCF_PIPL_NOMCLASS);
            //ListParametersAll.Add(PCF_PIPL_PAINT);
            //ListParametersAll.Add(PCF_PIPL_PREFIX);
            LPAll.Add(PCF_PIPL_PROJID);
            LPAll.Add(PCF_PIPL_REV);
            LPAll.Add(PCF_PIPL_SPEC);
            LPAll.Add(PCF_PIPL_TEMP);
            //ListParametersAll.Add(PCF_PIPL_TRACING);
            //ListParametersAll.Add(PCF_PIPL_TYPE);
            LPAll.Add(PCF_PIPL_DWGNAME);
            LPAll.Add(PCF_PIPL_NDTMSG);
            LPAll.Add(PCF_PIPL_CUTOFF);
            LPAll.Add(PCF_PIPL_TEGN);
            LPAll.Add(PCF_PIPL_KONTR);
            LPAll.Add(PCF_PIPL_GODK);
            LPAll.Add(PCF_PIPL_AT11);
            LPAll.Add(PCF_PIPL_AT12);
            LPAll.Add(PCF_PIPL_AT13);
            LPAll.Add(PCF_PIPL_AT14);
            LPAll.Add(PCF_PIPL_AT15);
            LPAll.Add(PCF_PIPL_AT16);
            LPAll.Add(PCF_PIPL_AT17);
            LPAll.Add(PCF_PIPL_AT18);
            LPAll.Add(PCF_PIPL_AT19);
            LPAll.Add(PCF_PIPL_AT20);
            LPAll.Add(PCF_PIPL_AT21);
            LPAll.Add(PCF_PIPL_AT22);
            LPAll.Add(PCF_PIPL_AT23);
            LPAll.Add(PCF_PIPL_AT24);
            LPAll.Add(PCF_PIPL_AT25);
            LPAll.Add(PCF_PIPL_AT26);
            LPAll.Add(PCF_PIPL_AT27);
            LPAll.Add(PCF_PIPL_AT28);
            LPAll.Add(PCF_PIPL_AT29);
            LPAll.Add(PCF_PIPL_AT30);
            LPAll.Add(PCF_PIPL_AT31);
            LPAll.Add(PCF_PIPL_AT32);
            LPAll.Add(PCF_PIPL_AT33);
            LPAll.Add(PCF_PIPL_AT34);
            LPAll.Add(PCF_PIPL_AT35);
            LPAll.Add(PCF_PIPL_AT36);
            LPAll.Add(PCF_PIPL_AT37);
            LPAll.Add(PCF_PIPL_AT38);
            LPAll.Add(PCF_PIPL_AT39);
            LPAll.Add(PCF_PIPL_AT40);
            LPAll.Add(PCF_PIPL_AT41);
            LPAll.Add(PCF_PIPL_AT42);
            LPAll.Add(PCF_PIPL_AT43);
            LPAll.Add(PCF_PIPL_AT44);
            LPAll.Add(PCF_PIPL_AT45);
            LPAll.Add(PCF_PIPL_AT46);
            LPAll.Add(PCF_PIPL_AT47);
            LPAll.Add(PCF_PIPL_AT48);
            LPAll.Add(PCF_PIPL_AT49);
            LPAll.Add(PCF_PIPL_AT50);
            LPAll.Add(PCF_PIPL_AT51);
            LPAll.Add(PCF_PIPL_AT52);
            LPAll.Add(PCF_PIPL_AT53);
            LPAll.Add(PCF_PIPL_AT54);
            LPAll.Add(PCF_PIPL_AT55);
            LPAll.Add(PCF_PIPL_AT56);
            LPAll.Add(PCF_PIPL_AT57);
            LPAll.Add(PCF_PIPL_AT58);

            LPAll.Add(PCF_PIPL_CII_PD);
            LPAll.Add(PCF_PIPL_CII_TD);
            LPAll.Add(PCF_PIPL_CII_MATNAME);
            LPAll.Add(PCF_ELEM_CII_WALLTHK);
            LPAll.Add(PCF_PIPL_CII_INSULTHK);
            LPAll.Add(PCF_PIPL_CII_INSULDST);
            LPAll.Add(PCF_PIPL_CII_CORRALL);
            LPAll.Add(PCF_ELEM_CII_COMPWEIGHT);
            LPAll.Add(PCF_PIPL_CII_FLUIDDST);
            LPAll.Add(PCF_PIPL_CII_HYDROPD);

            LPAll.Add(PCF_ELEM_SUPPORT_NAME);

            LPAll.Add(PCF_ELEM_EXCL);
            LPAll.Add(PCF_PIPL_EXCL);

            LPAll.Add(TAG_1);
            LPAll.Add(TAG_2);
            LPAll.Add(TAG_3);
            LPAll.Add(TAG_4);
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

        //public static IList<string> parameterNames = new List<string>();
    }
}