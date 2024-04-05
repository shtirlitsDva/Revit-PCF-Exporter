using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MEPUtils.CreateInstrumentation
{
    static class ListsAndDicts
    {
        public static List<string> Operations()
        {
            return new List<string> {
                "Auto ML (Udlufter)",
                "PT (Tryktransmitter)",
                "PI (Manometer)",
                "TT (Temp. transmitter)",
                "TI (Termometer)",
                "PS (Pressostat)",
                "Pipe" };
        }

        public static List<string> Directions()
        {
            return new List<string> { "Top", "Bottom", "Front", "Back", "Left", "Right" };
        }

        public static Dictionary<string, string> PipeTypeByOlet()
        {
            return new Dictionary<string, string>()
            {
                {"Weldolet", "Stålrør, sømløse weldolet" },
                {"Sockolet", "Stålrør, sømløse sockolet" },
                {"Termolomme", "Stålrør, sømløse, termolomme" },
                {"Tee", "Stålrør, sømløse" }
            };
        }

        public static Dictionary<int, double> StdPipeSchedule()
        {
            return new Dictionary<int, double>()
            {
                [10] = 17.2,
                [15] = 21.3,
                [20] = 26.9,
                [25] = 33.7,
                [32] = 42.4,
                [40] = 48.3,
                [50] = 60.3,
                [65] = 76.1,
                [80] = 88.9,
                [100] = 114.3,
                [125] = 139.7,
                [150] = 168.3,
                [200] = 219.1,
                [250] = 273.0,
                [300] = 323.9,
                [350] = 355.6,
                [400] = 406.4,
                [450] = 457.0,
                [500] = 508.0,
                [600] = 610.0
            };
        }
        /// <summary>
        /// Returns a list of valid general Pipe DNs
        /// </summary>
        /// <returns></returns>
        public static List<string> SizeList()
        {
            return new List<string>()
            {
                {"15"},
                {"20"},
                {"25"},
                {"32"},
                {"40"},
                {"50"},
                {"65"},
                {"80"},
                {"100"},
                {"125"},
                {"150"},
                {"200"},
                {"250"},
                {"300"},
                {"350"},
                {"400"},
                {"450"},
                {"500"},
                {"600"}
            };
        }

        /// <summary>
        /// Returns a list of valid Sockolet NDs
        /// </summary>
        public static List<string> SockoletList()
        {
            return new List<string>()
            {
                {"15" },
                {"20" },
                {"25" }
            };
        }

        /// <summary>
        /// Returns a list of valid Weldolet NDs
        /// </summary>
        public static List<string> WeldoletList()
        {
            return new List<string>()
            {
                {"32" },
                {"40" },
                {"50" },
                {"65" },
                {"80" },
                {"100" }
            };
        }
    }
}
