using System.Collections.Generic;

using Autodesk.Revit.DB;

using Shared;

namespace PcfExporter.Writer
{
    /// <summary>
    /// Composes one keyworded line from the values of several element parameters,
    /// joined by underscores (e.g. "    TAG value1_value2"). Empty when no values.
    /// </summary>
    public static class TagWriter
    {
        public static string Line(string keyword, string[] parameterNames, Element element)
        {
            var values = new List<string>();
            foreach (string name in parameterNames)
            {
                string value = element.LookupParameter(name)?.ToValueString();
                if (!value.IsNullOrEmpty()) values.Add(value);
            }
            if (values.Count < 1) return "";
            return $"    {keyword} {string.Join("_", values)}\n";
        }
    }
}
