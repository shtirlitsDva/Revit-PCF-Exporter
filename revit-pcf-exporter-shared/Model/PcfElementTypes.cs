using System.Collections.Generic;

namespace PcfExporter.Model
{
    /// <summary>
    /// The PCF_ELEM_TYPE vocabulary. The factory maps every value here to an element
    /// class; a sync test asserts the mapping is total.
    /// </summary>
    public static class PcfElementTypes
    {
        public const string Pipe = "PIPE";
        public const string Elbow = "ELBOW";
        public const string Tee = "TEE";
        public const string Filter = "FILTER";
        public const string Gasket = "GASKET";
        public const string ReducerConcentric = "REDUCER-CONCENTRIC";
        public const string ReducerEccentric = "REDUCER-ECCENTRIC";
        public const string Coupling = "COUPLING";
        public const string Union = "UNION";
        public const string PipeBlockFixed = "PIPE-BLOCK-FIXED";
        public const string FlangeBlind = "FLANGE-BLIND";
        public const string Cap = "CAP";
        public const string Flange = "FLANGE";
        public const string TeeStub = "TEE-STUB";
        public const string Olet = "OLET";
        public const string Valve = "VALVE";
        public const string Instrument = "INSTRUMENT";
        public const string MiscComponent = "MISC-COMPONENT";
        public const string Valve3Way = "VALVE-3WAY";
        public const string Instrument3Way = "INSTRUMENT-3WAY";
        public const string ValveAngle = "VALVE-ANGLE";
        public const string InstrumentDial = "INSTRUMENT-DIAL";
        public const string Support = "SUPPORT";
        public const string FloorSymbol = "FLOOR-SYMBOL";
        public const string Tap = "TAP";
        public const string Bolt = "BOLT";

        public static IReadOnlyList<string> All { get; } = new[]
        {
            Pipe, Elbow, Tee, Filter, Gasket, ReducerConcentric, ReducerEccentric,
            Coupling, Union, PipeBlockFixed, FlangeBlind, Cap, Flange, TeeStub, Olet,
            Valve, Instrument, MiscComponent, Valve3Way, Instrument3Way, ValveAngle,
            InstrumentDial, Support, FloorSymbol, Tap, Bolt
        };
    }
}
