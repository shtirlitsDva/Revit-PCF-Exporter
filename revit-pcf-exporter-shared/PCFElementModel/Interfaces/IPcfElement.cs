using Autodesk.Revit.DB;

using PCF_Functions;

using System;
using System.Collections.Generic;
using System.Text;

namespace PCF_Model
{
    internal interface IPcfElement
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
