using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NTR_Functions
{
    public static class InputVars
    {
        //Scope control
        public static bool ExportAllOneFile = false;
        public static bool ExportAllSepFiles = false;
        public static bool ExportSpecificPipeLine = false;
        public static bool ExportSelection = false;

        //Current SystemAbbreviation
        public static string SysAbbr = null;

        //File handling
        public static string OutputDirectoryFilePath = "";

        //Diameter limit
        public static double DiameterLimit = 0;

        //Wall thickness
        public static bool WriteWallThickness = false;

        //Export options
        public static bool ExportToPlant3DIso = false;
        public static bool ExportToCII = false;

        //Read configuration
        public static string ExcelSheet = "";

        //GEN: General settings
        public static string NTR_GEN_TMONT = "10";
        public static string NTR_GEN_UNITKT = "MM";
        public static string NTR_GEN_CODE = "EN13480";

        //AUFT: Project name
        public static string NTR_AUFT_TEXT = "Project";

        //TEXT: User defined text
        public static string NTR_TEXT_TEXT1 = "";
        public static string NTR_TEXT_TEXT2 = "";
    }
}
