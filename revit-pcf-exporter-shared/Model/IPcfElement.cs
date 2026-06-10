using System.Collections.Generic;
using System.Text;

using Autodesk.Revit.DB;

namespace PcfExporter.Model
{
    /// <summary>
    /// One exported PCF component — physical (backed by a Revit element) or virtual
    /// (derived: gasket, field weld, split point, start point).
    /// </summary>
    public interface IPcfElement
    {
        HashSet<Connector> AllConnectors { get; }
        ElementId ElementId { get; }
        string SystemAbbreviation { get; }
        string GetParameterValue(ParameterDefinition pdef);
        void SetParameterValue(ParameterDefinition pdef, string value);
        bool ParticipateInMaterialTable { get; }
        StringBuilder ToPCFString();
    }
}
