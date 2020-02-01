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
        public ParameterDefinition(string pName, string pDomain, string pUsage, ParameterType pType, Guid pGuid, string pKeyword = "")
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
        public string Domain { get; } //PIPL = Pipeline, ELEM = Element, SUPP = Support, CTRL = Execution control.
        public string Usage { get; } //U = user defined values, P = programatically defined values.
        public ParameterType Type { get; }
        public Guid Guid { get; }
        public string Keyword { get; } //The keyword as defined in the PCF reference guide.
        public string ExportingTo { get; } = null; //Currently used with CII export to distinguish CII parameters from other PIPL parameters.
    }

    public class ParameterList
    {
        public readonly HashSet<pdef> LPAll = new HashSet<ParameterDefinition>();

        #region Parameter Definition
        //Element parameters user defined
        public readonly pdef PCF_ELEM_TYPE = new pdef("PCF_ELEM_TYPE", "ELEM", "U", pd.Text, new Guid("bfc7b779-786d-47cd-9194-8574a5059ec8"));
        public readonly pdef PCF_ELEM_SKEY = new pdef("PCF_ELEM_SKEY", "ELEM", "U", pd.Text, new Guid("3feebd29-054c-4ce8-bc64-3cff75ed6121"), "SKEY");
        public readonly pdef PCF_ELEM_SPEC = new pdef("PCF_ELEM_SPEC", "ELEM", "U", pd.Text, new Guid("90be8246-25f7-487d-b352-554f810fcaa7"), "PIPING-SPEC");
        //public readonly pdef PCF_ELEM_CATEGORY = new pdef("PCF_ELEM_CATEGORY", "ELEM", "U", pd.Text, new Guid("35efc6ed-2f20-4aca-bf05-d81d3b79dce2"), "CATEGORY");
        public readonly pdef PCF_ELEM_END1 = new pdef("PCF_ELEM_END1", "ELEM", "U", pd.Text, new Guid("cbc10825-c0a1-471e-9902-075a41533738"));
        public readonly pdef PCF_ELEM_END2 = new pdef("PCF_ELEM_END2", "ELEM", "U", pd.Text, new Guid("ecaf3f8a-c28b-4a89-8496-728af3863b09"));
        public readonly pdef PCF_ELEM_END3 = new pdef("PCF_ELEM_END3", "ELEM", "U", pd.Text, new Guid("501E24A0-C23A-43EE-94A0-F6D17960CB78"));
        public readonly pdef PCF_ELEM_BP1 = new pdef("PCF_ELEM_BP1", "ELEM", "U", pd.Text, new Guid("89b1e62e-f9b8-48c3-ab3a-1861a772bda8"));
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
        public readonly pdef PCF_MAT_DESCR = new pdef("PCF_MAT_DESCR", "ELEM", "U", pd.Text, new Guid("d39418f2-fcb3-4dd1-b0be-3d647486ebe6"));

        //Programattically defined
        public readonly pdef PCF_ELEM_TAP1 = new pdef("PCF_ELEM_TAP1", "ELEM", "P", pd.Text, new Guid("5fda303c-5536-429b-9fcc-afb40d14c7b3"));
        public readonly pdef PCF_ELEM_TAP2 = new pdef("PCF_ELEM_TAP2", "ELEM", "P", pd.Text, new Guid("e1e9bc3b-ce75-4f3a-ae43-c270f4fde937"));
        public readonly pdef PCF_ELEM_TAP3 = new pdef("PCF_ELEM_TAP3", "ELEM", "P", pd.Text, new Guid("12693653-8029-4743-be6a-310b1fbc0620"));
        public readonly pdef PCF_ELEM_COMPID = new pdef("PCF_ELEM_COMPID", "ELEM", "P", pd.Integer, new Guid("876d2334-f860-4b5a-8c24-507e2c545fc0"));
        public readonly pdef PCF_MAT_ID = new pdef("PCF_MAT_ID", "ELEM", "P", pd.Integer, new Guid("fc5d3b19-af5b-47f6-a269-149b701c9364"), "MATERIAL-IDENTIFIER");

        //Pipeline parameters
        public readonly pdef PCF_PIPL_LINEID = new pdef("PCF_LINEID", "PIPL", "U", pd.Text, new Guid("A12D0564-A8D9-451A-9800-5704EB1E7B75"), "LINE-ID");
        public readonly pdef PCF_PIPL_AREA = new pdef("PCF_AREA", "PIPL", "U", pd.Text, new Guid("DE15EB1B-CFD1-418B-80F2-D24D321130BC"), "AREA");
        public readonly pdef PCF_PIPL_DATE = new pdef("PCF_DATE", "PIPL", "U", pd.Text, new Guid("6F6D1903-CC39-4353-85ED-0AA5AEF1C815"), "DATE-DMY");
        //public readonly pdef PCF_PIPL_GRAV = new pdef("PCF_PIPL_GRAV", "PIPL", "U", pd.Text, new Guid("a32c0713-a6a5-4e6c-9a6b-d96e82159611"), "SPECIFIC-GRAVITY");
        //public readonly pdef PCF_PIPL_INSUL = new pdef("PCF_PIPL_INSUL", "PIPL", "U", pd.Text, new Guid("d0c429fe-71db-4adc-b54a-58ae2fb4e127"), "INSULATION-SPEC");
        //public readonly pdef PCF_PIPL_JACKET = new pdef("PCF_PIPL_JACKET", "PIPL", "U", pd.Text, new Guid("a810b6b8-17da-4191-b408-e046c758b289"), "JACKET-SPEC");
        //public readonly pdef PCF_PIPL_MISC1 = new pdef("PCF_PIPL_MISC1", "PIPL", "U", pd.Text, new Guid("22f1dbed-2978-4474-9a8a-26fd14bc6aac"), "MISC-SPEC1");
        //public readonly pdef PCF_PIPL_MISC2 = new pdef("PCF_PIPL_MISC2", "PIPL", "U", pd.Text, new Guid("6492e7d8-cbc3-42f8-86c0-0ba9000d65ca"), "MISC-SPEC2");
        //public readonly pdef PCF_PIPL_MISC3 = new pdef("PCF_PIPL_MISC3", "PIPL", "U", pd.Text, new Guid("680bac72-0a1c-44a9-806d-991401f71912"), "MISC-SPEC3");
        //public readonly pdef PCF_PIPL_MISC4 = new pdef("PCF_PIPL_MISC4", "PIPL", "U", pd.Text, new Guid("6f904559-568b-4eff-a016-9c81e3a6c3ab"), "MISC-SPEC4");
        //public readonly pdef PCF_PIPL_MISC5 = new pdef("PCF_PIPL_MISC5", "PIPL", "U", pd.Text, new Guid("c375351b-b585-4fb1-92f7-abcdc10fd53a"), "MISC-SPEC5");
        public readonly pdef PCF_PIPL_NOMCLASS = new pdef("PCF_NOMCLASS", "PIPL", "U", pd.Text, new Guid("A5CB2A32-19E2-4536-838C-BE5253A5D301"), "NOMINAL-CLASS");
        //public readonly pdef PCF_PIPL_PAINT = new pdef("PCF_PIPL_PAINT", "PIPL", "U", pd.Text, new Guid("e440ed45-ce29-4b42-9a48-238b62b7522e"), "PAINTING-SPEC");
        //public readonly pdef PCF_PIPL_PREFIX = new pdef("PCF_PIPL_PREFIX", "PIPL", "U", pd.Text, new Guid("c7136bbc-4b0d-47c6-95d1-8623ad015e8f"), "SPOOL-PREFIX");
        public readonly pdef PCF_PIPL_PROJID = new pdef("PCF_PROJID", "PIPL", "U", pd.Text, new Guid("EBFC0D5E-170B-41E7-B8B3-CF68D8402773"), "PROJECT-IDENTIFIER");
        public readonly pdef PCF_PIPL_REV = new pdef("PCF_REV", "PIPL", "U", pd.Text, new Guid("17DA8F27-5B5F-4C18-BA13-30CDD07E60DC"), "REVISION");
        public readonly pdef PCF_PIPL_SPEC = new pdef("PCF_PIPL_SPEC", "PIPL", "U", pd.Text, new Guid("7b0c932b-2ebe-495f-9d2e-effc350e8a59"), "PIPING-SPEC");
        public readonly pdef PCF_PIPL_TEMP = new pdef("PCF_TEMP", "PIPL", "U", pd.Text, new Guid("4CF40C73-7631-40B3-BA98-03994F3E0FD0"), "PIPELINE-TEMP");
        //public readonly pdef PCF_PIPL_TRACING = new pdef("PCF_PIPL_TRACING", "PIPL", "U", pd.Text, new Guid("9d463d11-c9e8-4160-ac55-578795d11b1d"), "TRACING-SPEC");
        //public readonly pdef PCF_PIPL_TYPE = new pdef("PCF_PIPL_TYPE", "PIPL", "U", pd.Text, new Guid("af00ee7d-cfc0-4e1c-a2cf-1626e4bb7eb0"), "PIPELINE-TYPE");
        public readonly pdef PCF_PIPL_DWGNAME = new pdef("PCF_DWGNAME","PIPL","U",pd.Text, new Guid("DBD769B8-2535-481A-A602-BC0B8D8C7A16"), "DRAWINGNAME", "ISO");
        public readonly pdef PCF_PIPL_AT01 = new pdef("PCF_TEGN", "PIPL", "U", pd.Text, new Guid("718A6AF7-8068-40C0-94FD-B2D387DBBE87"), "Attribute1", "ISO");
        public readonly pdef PCF_PIPL_AT02 = new pdef("PCF_KONTR", "PIPL", "U", pd.Text, new Guid("DCD496AC-ACFD-4492-A7D9-EBC562559E2A"), "Attribute2", "ISO");
        public readonly pdef PCF_PIPL_AT03 = new pdef("PCF_GODK", "PIPL", "U", pd.Text, new Guid("A0A067D4-49BB-4E4B-91FB-F30F9E189005"), "Attribute3", "ISO");
        public readonly pdef PCF_PIPL_AT04 = new pdef("PCF_REVA", "REVS", "U", pd.Text, new Guid("FC1DCFE6-8405-6198-97DB-E7F522868F2B"), "Attribute4", "ISO");
        public readonly pdef PCF_PIPL_AT05 = new pdef("PCF_ADESCR", "REVS", "U", pd.Text, new Guid("AA7A134E-03F1-068E-462A-5B04443CA53F"), "Attribute5", "ISO");
        public readonly pdef PCF_PIPL_AT06 = new pdef("PCF_ADATO", "REVS", "U", pd.Text, new Guid("20ECCDC8-9E07-159C-50B3-5900F75E8F36"), "Attribute6", "ISO");
        public readonly pdef PCF_PIPL_AT07 = new pdef("PCF_ADRWN", "REVS", "U", pd.Text, new Guid("E3D89E2C-375F-69F9-5E4C-37674BDC0546"), "Attribute7", "ISO");
        public readonly pdef PCF_PIPL_AT08 = new pdef("PCF_AKONTR", "REVS", "U", pd.Text, new Guid("87EE5BEB-52CD-8B7B-7963-B1BB6763A007"), "Attribute8", "ISO");
        public readonly pdef PCF_PIPL_AT09 = new pdef("PCF_AGODK", "REVS", "U", pd.Text, new Guid("03D39B26-1F31-6E81-327C-E5B344C29BD9"), "Attribute9", "ISO");
        public readonly pdef PCF_PIPL_AT10 = new pdef("PCF_REVB", "REVS", "U", pd.Text, new Guid("110DE80D-360A-9293-7515-03C1B496143C"), "Attribute10", "ISO");
        public readonly pdef PCF_PIPL_AT11 = new pdef("PCF_BDESCR", "REVS", "U", pd.Text, new Guid("52780ECE-7EE3-0254-4CB9-92AC69C749AE"), "Attribute11", "ISO");
        public readonly pdef PCF_PIPL_AT12 = new pdef("PCF_BDATO", "REVS", "U", pd.Text, new Guid("68CDE5EF-9CCE-4A20-0017-16956B0A7E5C"), "Attribute12", "ISO");
        public readonly pdef PCF_PIPL_AT13 = new pdef("PCF_BDRWN", "REVS", "U", pd.Text, new Guid("0FBE846A-25A0-15C2-7E32-2C7A06E89656"), "Attribute13", "ISO");
        public readonly pdef PCF_PIPL_AT14 = new pdef("PCF_BKONTR", "REVS", "U", pd.Text, new Guid("08CF4E97-9777-7EE3-5369-A597D596150C"), "Attribute14", "ISO");
        public readonly pdef PCF_PIPL_AT15 = new pdef("PCF_BGODK", "REVS", "U", pd.Text, new Guid("73183AF1-3342-1F3E-1044-64A7CD2840A7"), "Attribute15", "ISO");
        public readonly pdef PCF_PIPL_AT16 = new pdef("PCF_REVC", "REVS", "U", pd.Text, new Guid("4232556D-9B80-5928-868D-1D3F168E7139"), "Attribute16", "ISO");
        public readonly pdef PCF_PIPL_AT17 = new pdef("PCF_CDESCR", "REVS", "U", pd.Text, new Guid("64433BF1-1805-098E-0361-7323D6FE6216"), "Attribute17", "ISO");
        public readonly pdef PCF_PIPL_AT18 = new pdef("PCF_CDATO", "REVS", "U", pd.Text, new Guid("D83D6886-841C-5E97-67E6-0932887121BD"), "Attribute18", "ISO");
        public readonly pdef PCF_PIPL_AT19 = new pdef("PCF_CDRWN", "REVS", "U", pd.Text, new Guid("ACCEC147-5F77-040D-937F-DA6C30081DCB"), "Attribute19", "ISO");
        public readonly pdef PCF_PIPL_AT20 = new pdef("PCF_CKONTR", "REVS", "U", pd.Text, new Guid("0A20F983-A55B-4B33-1011-4D3E4C79096E"), "Attribute20", "ISO");
        public readonly pdef PCF_PIPL_AT21 = new pdef("PCF_CGODK", "REVS", "U", pd.Text, new Guid("22D5D680-0B98-596D-4584-06CBE5DBA411"), "Attribute21", "ISO");
        public readonly pdef PCF_PIPL_AT22 = new pdef("PCF_REVD", "REVS", "U", pd.Text, new Guid("8D9C0F8D-49E3-1820-65AC-B07AFBE15856"), "Attribute22", "ISO");
        public readonly pdef PCF_PIPL_AT23 = new pdef("PCF_DDESCR", "REVS", "U", pd.Text, new Guid("41599864-887D-54C1-001D-245CE6C33732"), "Attribute23", "ISO");
        public readonly pdef PCF_PIPL_AT24 = new pdef("PCF_DDATO", "REVS", "U", pd.Text, new Guid("2704C8DF-64C5-5366-8F96-1610BACA81CF"), "Attribute24", "ISO");
        public readonly pdef PCF_PIPL_AT25 = new pdef("PCF_DDRWN", "REVS", "U", pd.Text, new Guid("4119601D-39B0-3E2E-1B24-8FFD5A1AA4AA"), "Attribute25", "ISO");
        public readonly pdef PCF_PIPL_AT26 = new pdef("PCF_DKONTR", "REVS", "U", pd.Text, new Guid("6834749E-22B7-11F2-97FF-353282B10E6F"), "Attribute26", "ISO");
        public readonly pdef PCF_PIPL_AT27 = new pdef("PCF_DGODK", "REVS", "U", pd.Text, new Guid("E5D8981A-38F6-2BD7-6A63-5EFA8C888A68"), "Attribute27", "ISO");
        public readonly pdef PCF_PIPL_AT28 = new pdef("PCF_REVE", "REVS", "U", pd.Text, new Guid("2C0CAE27-40F1-21A1-4557-719CC3829339"), "Attribute28", "ISO");
        public readonly pdef PCF_PIPL_AT29 = new pdef("PCF_EDESCR", "REVS", "U", pd.Text, new Guid("5006E5DB-16C7-1048-9A00-0FE0298C1BFE"), "Attribute29", "ISO");
        public readonly pdef PCF_PIPL_AT30 = new pdef("PCF_EDATO", "REVS", "U", pd.Text, new Guid("D9EC385C-0167-2FA6-740A-B85A3D160498"), "Attribute30", "ISO");
        public readonly pdef PCF_PIPL_AT31 = new pdef("PCF_EDRWN", "REVS", "U", pd.Text, new Guid("76848C88-431B-9C8F-0CE5-44C0B4039A2C"), "Attribute31", "ISO");
        public readonly pdef PCF_PIPL_AT32 = new pdef("PCF_EKONTR", "REVS", "U", pd.Text, new Guid("B73D39A7-3E36-0434-A2C7-1D06A3FE2CF5"), "Attribute32", "ISO");
        public readonly pdef PCF_PIPL_AT33 = new pdef("PCF_EGODK", "REVS", "U", pd.Text, new Guid("82D47BAC-6CB1-3AEC-6DB8-67A8CFE08D5A"), "Attribute33", "ISO");
        public readonly pdef PCF_PIPL_AT34 = new pdef("PCF_REVF", "REVS", "U", pd.Text, new Guid("9E9AF496-83BE-97AB-4F39-F16DFAE28023"), "Attribute34", "ISO");
        public readonly pdef PCF_PIPL_AT35 = new pdef("PCF_FDESCR", "REVS", "U", pd.Text, new Guid("F77744FD-544A-825B-488A-ACC0BA102374"), "Attribute35", "ISO");
        public readonly pdef PCF_PIPL_AT36 = new pdef("PCF_FDATO", "REVS", "U", pd.Text, new Guid("7961EC15-31B9-46BC-0983-9FFB7A5F2507"), "Attribute36", "ISO");
        public readonly pdef PCF_PIPL_AT37 = new pdef("PCF_FDRWN", "REVS", "U", pd.Text, new Guid("D87FA74E-39A7-0AE5-56D8-E28F39EF9166"), "Attribute37", "ISO");
        public readonly pdef PCF_PIPL_AT38 = new pdef("PCF_FKONTR", "REVS", "U", pd.Text, new Guid("5ECCAA78-4ECF-54B4-4230-1095ED393676"), "Attribute38", "ISO");
        public readonly pdef PCF_PIPL_AT39 = new pdef("PCF_FGODK", "REVS", "U", pd.Text, new Guid("54D718FE-79DE-20A8-2A2A-F715ADFD41DE"), "Attribute39", "ISO");
        public readonly pdef PCF_PIPL_AT40 = new pdef("PCF_REVG", "REVS", "U", pd.Text, new Guid("1A32EF91-0D3B-44E0-5BF8-7CAF55A12E1B"), "Attribute40", "ISO");
        public readonly pdef PCF_PIPL_AT41 = new pdef("PCF_GDESCR", "REVS", "U", pd.Text, new Guid("10452FA4-9561-6A8E-4FA0-AAB0F6C7684A"), "Attribute41", "ISO");
        public readonly pdef PCF_PIPL_AT42 = new pdef("PCF_GDATO", "REVS", "U", pd.Text, new Guid("E6AF3063-6FD0-08E1-1321-822A32C07C85"), "Attribute42", "ISO");
        public readonly pdef PCF_PIPL_AT43 = new pdef("PCF_GDRWN", "REVS", "U", pd.Text, new Guid("4E654963-9990-1226-9BD6-8E2E27027C6C"), "Attribute43", "ISO");
        public readonly pdef PCF_PIPL_AT44 = new pdef("PCF_GKONTR", "REVS", "U", pd.Text, new Guid("A632D42C-7227-98D7-3FEA-15708ACB8CF9"), "Attribute44", "ISO");
        public readonly pdef PCF_PIPL_AT45 = new pdef("PCF_GGODK", "REVS", "U", pd.Text, new Guid("A4A440FD-323C-33EE-1100-A190C2CA64FF"), "Attribute45", "ISO");
        public readonly pdef PCF_PIPL_AT46 = new pdef("PCF_REVH", "REVS", "U", pd.Text, new Guid("7B1CC759-82D0-03C9-34D3-C80BA600476D"), "Attribute46", "ISO");
        public readonly pdef PCF_PIPL_AT47 = new pdef("PCF_HDESCR", "REVS", "U", pd.Text, new Guid("469CB1C9-7059-15FB-0CBE-898024381A7D"), "Attribute47", "ISO");
        public readonly pdef PCF_PIPL_AT48 = new pdef("PCF_HDATO", "REVS", "U", pd.Text, new Guid("7F43FFB6-A16F-5E57-3137-E89B3E68874A"), "Attribute48", "ISO");
        public readonly pdef PCF_PIPL_AT49 = new pdef("PCF_HDRWN", "REVS", "U", pd.Text, new Guid("FD540AAD-3760-1104-24A2-74A7C2153F99"), "Attribute49", "ISO");
        public readonly pdef PCF_PIPL_AT50 = new pdef("PCF_HKONTR", "REVS", "U", pd.Text, new Guid("554155E9-4003-6135-886E-434329528997"), "Attribute50", "ISO");
        public readonly pdef PCF_PIPL_AT51 = new pdef("PCF_HGODK", "REVS", "U", pd.Text, new Guid("23D54F0D-32DF-2B6E-6EFA-DBE89C4E7AF5"), "Attribute51", "ISO");
        public readonly pdef PCF_PIPL_AT52 = new pdef("PCF_REVI", "REVS", "U", pd.Text, new Guid("D661CBE2-8A8F-765B-582D-A57C4DB83154"), "Attribute52", "ISO");
        public readonly pdef PCF_PIPL_AT53 = new pdef("PCF_IDESCR", "REVS", "U", pd.Text, new Guid("34EB515F-5FE6-7765-6D9D-E8D7A93E757B"), "Attribute53", "ISO");
        public readonly pdef PCF_PIPL_AT54 = new pdef("PCF_IDATO", "REVS", "U", pd.Text, new Guid("612AF597-0CA9-0480-5103-F8348B9CA2CC"), "Attribute54", "ISO");
        public readonly pdef PCF_PIPL_AT55 = new pdef("PCF_IDRWN", "REVS", "U", pd.Text, new Guid("A337D8A2-31CD-893A-7ABF-293D863181CF"), "Attribute55", "ISO");
        public readonly pdef PCF_PIPL_AT56 = new pdef("PCF_IKONTR", "REVS", "U", pd.Text, new Guid("8E3F477F-A3D0-9B93-3676-B9BD7068A749"), "Attribute56", "ISO");
        public readonly pdef PCF_PIPL_AT57 = new pdef("PCF_IGODK", "REVS", "U", pd.Text, new Guid("E6D442E1-87E3-39DC-435E-B5EFB0B41350"), "Attribute57", "ISO");

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

        //Usability parameters
        public readonly pdef PCF_ELEM_EXCL = new pdef("PCF_ELEM_EXCL", "CTRL", "U", pd.YesNo, new Guid("CC8EC292-226C-4677-A32D-10B9736BFC1A"));
        //If guid changes can break other methods!!
        //Shared.MepUtils.GetDistinctPhysicalPipingSystemTypeNames(Document doc) uses this guid!!!
        public readonly pdef PCF_PIPL_EXCL = new pdef("PCF_PIPL_EXCL", "CTRL", "U", pd.YesNo, new Guid("C1C2C9FE-2634-42BA-89D0-5AF699F54D4C"));
        #endregion

        public ParameterList()
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
            //ListParametersAll.Add(PCF_ELEM_STATUS);
            //ListParametersAll.Add(PCF_ELEM_TRACING_SPEC);
            //ListParametersAll.Add(PCF_ELEM_INSUL_SPEC);
            //ListParametersAll.Add(PCF_ELEM_PAINT_SPEC);
            //ListParametersAll.Add(PCF_ELEM_MISC1);
            //ListParametersAll.Add(PCF_ELEM_MISC2);
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
            LPAll.Add(PCF_PIPL_AT01);
            LPAll.Add(PCF_PIPL_AT02);
            LPAll.Add(PCF_PIPL_AT03);
            LPAll.Add(PCF_PIPL_AT04);
            LPAll.Add(PCF_PIPL_AT05);
            LPAll.Add(PCF_PIPL_AT06);
            LPAll.Add(PCF_PIPL_AT07);
            LPAll.Add(PCF_PIPL_AT08);
            LPAll.Add(PCF_PIPL_AT09);
            LPAll.Add(PCF_PIPL_AT10);
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
            #endregion
        }
    }

    public static class ParameterData
    {
        #region Parameter Data Entry

        //general values
        public const ParameterType Text = ParameterType.Text;
        public const ParameterType Integer = ParameterType.Integer;
        public const ParameterType YesNo = ParameterType.YesNo;
        #endregion

        public static IList<string> parameterNames = new List<string>();
    }
}