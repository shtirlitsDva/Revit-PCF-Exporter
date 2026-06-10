using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Plumbing;

using PcfExporter.Configuration;
using PcfExporter.Context;
using PcfExporter.ElementSource;

using Shared;

using pdef = PcfExporter.Model.ParameterDefinition;
using plst = PcfExporter.Model.Parameters;

namespace PcfExporter.Services
{
    /// <summary>
    /// Fills PCF parameter values from the Excel configuration workbooks:
    /// element values from the Elements sheet (keyed by Family and Type),
    /// pipeline values from the Pipelines sheet (keyed by project id + line name).
    /// </summary>
    public interface IParameterPopulationService
    {
        string PopulateElements(IRevitContext ctx, PcfConfiguration cfg, DataTable elementsTable);
        string PopulatePipelines(IRevitContext ctx, PcfConfiguration cfg, DataTable pipelinesTable);
    }

    public sealed class ParameterPopulationService : IParameterPopulationService
    {
        public string PopulateElements(IRevitContext ctx, PcfConfiguration cfg, DataTable dataTable)
        {
            Document doc = ctx.Doc;

            List<string> parameterNames = dataTable.Columns.Cast<DataColumn>()
                .Select(dc => dc.ColumnName).Skip(1).ToList();
            if (parameterNames.Count == 0)
                throw new Exception("Parameter names are incorrectly defined. Please reselect the EXCEL workbook.");

            //One row per Family and Type (first column); first match wins.
            Dictionary<string, DataRow> rowsByFamilyType = dataTable.AsEnumerable()
                .GroupBy(r => r.Field<string>(0))
                .Where(g => g.Key != null)
                .ToDictionary(g => g.Key, g => g.First());

            Dictionary<string, Guid> guidByName = plst.LPAll()
                .Where(p => p.Domain == Model.ParameterDomain.ELEM)
                .ToDictionary(p => p.Name, p => p.Guid);

            var source = new RevitElementSource(ctx);
            HashSet<Element> colElements = source.Collect(cfg);

            int pNumber = 0, fNumber = 0, aNumber = 0;
            var skippedFamilyTypes = new HashSet<string>();
            Element elementRefForFeedback = null;

            try
            {
                ctx.RunInTransaction("Initialize PCF parameters", () =>
                {
                    foreach (Element element in colElements)
                    {
                        elementRefForFeedback = element;

                        //Filter out elements in ARGD (Rigids) system type
                        Cons cons = new Cons(element);
                        if (cons.Primary.MEPSystemAbbreviation(doc, true) == "ARGD") continue;

                        switch (element.Category.Name)
                        {
                            case "Pipes": pNumber++; break;
                            case "Pipe Fittings": fNumber++; break;
                            case "Pipe Accessories": aNumber++; break;
                        }

                        string familyType = element.get_Parameter(
                            BuiltInParameter.ELEM_FAMILY_AND_TYPE_PARAM).AsValueString();
                        if (!rowsByFamilyType.TryGetValue(familyType, out DataRow row))
                        {
                            //Expected during define-as-you-go workflow — but counted,
                            //never silent (user decision 2026-06-10).
                            skippedFamilyTypes.Add(familyType);
                            continue;
                        }

                        foreach (string parameterName in parameterNames)
                        {
                            string parameterValue = row.Field<string>(parameterName);
                            if (string.IsNullOrEmpty(parameterValue)) continue;

                            if (!guidByName.TryGetValue(parameterName, out Guid parGuid))
                                throw new Exception(
                                    $"Column {parameterName} is not an ELEMENT parameter. " +
                                    "Select an ELEMENT parameter workbook.");

                            //Each elbow angle gets a unique description so differing
                            //angles land in differing material groups.
                            if (parameterName == "PCF_MAT_DESCR" &&
                                row.Field<string>("PCF_ELEM_TYPE") == Model.PcfElementTypes.Elbow)
                            {
                                Parameter par = element.LookupParameter("Angle")
                                    ?? element.LookupParameter("angle");
                                if (par == null) throw new Exception(
                                    $"Angle parameter on elbow {element.Id} does not exist or is named differently!");
                                parameterValue +=
                                    $", {Conversion.RadianToDegree(par.AsDouble()):0}°";
                            }

                            Parameter target = element.get_Parameter(parGuid);
                            if (target == null)
                                throw new Exception(
                                    $"Element {element.Id} does not have parameter {parameterName}.\n" +
                                    "Run 'Import PCF parameters' first.");

                            //Overwrite or append (only fill empty) per configuration
                            if (cfg.Overwrite) target.Set(parameterValue);
                            else if (string.IsNullOrEmpty(target.ToValueString())) target.Set(parameterValue);
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                if (elementRefForFeedback != null)
                    ctx.SetSelection(new[] { elementRefForFeedback.Id });
                throw new Exception(
                    $"Population of parameters failed for element {elementRefForFeedback?.Id} " +
                    "(selected in the model).\n" + ex.Message, ex);
            }

            string feedback =
                pNumber + " Pipes initialized.\n" +
                fNumber + " Pipe fittings initialized.\n" +
                aNumber + " Pipe accessories initialized.";
            if (skippedFamilyTypes.Count > 0)
                feedback += $"\n\n{skippedFamilyTypes.Count} element type(s) had no workbook row and were skipped:\n" +
                            string.Join("\n", skippedFamilyTypes.OrderBy(x => x));
            return feedback;
        }

        public string PopulatePipelines(IRevitContext ctx, PcfConfiguration cfg, DataTable dataTable)
        {
            Document doc = ctx.Doc;

            List<string> parameterNames = dataTable.Columns.Cast<DataColumn>()
                .Select(dc => dc.ColumnName).ToList();
            if (parameterNames.Count == 0)
                throw new Exception("Parameter names are incorrectly defined. Please reselect the EXCEL workbook.");

            //Distinct PipingSystemTypes present in the model (by name)
            List<Element> systemTypes = new FilteredElementCollector(doc)
                .OfClass(typeof(PipingSystem))
                .Cast<PipingSystem>()
                .Select(sys => doc.GetElement(sys.GetTypeId()))
                .GroupBy(st => st.Name)
                .Select(g => g.First())
                .ToList();

            //LDT parameters are read directly from EXCEL into the PCF file — not populated.
            Dictionary<string, Guid> guidByName = plst.LPAll()
                .Where(p => p.Domain == Model.ParameterDomain.PIPL &&
                            p.ExportingTo != Model.ExportingTo.LDT)
                .ToDictionary(p => p.Name, p => p.Guid);

            //Rows keyed by (project identifier, system abbreviation)
            Dictionary<(string, string), DataRow> rowsByKey = dataTable.AsEnumerable()
                .GroupBy(r => (r.Field<string>(0), r.Field<string>(1)))
                .ToDictionary(g => g.Key, g => g.First());

            int sNumber = 0;
            var skippedSystems = new List<string>();

            ctx.RunInTransaction("Initialize Pipeline PCF parameters", () =>
            {
                foreach (Element systemType in systemTypes)
                {
                    sNumber++;
                    string sysAbbr = systemType.get_Parameter(
                        BuiltInParameter.RBS_SYSTEM_ABBREVIATION_PARAM).AsString();

                    if (!rowsByKey.TryGetValue((cfg.ProjectIdentifier, sysAbbr), out DataRow row))
                    {
                        //Counted, never silent (user decision 2026-06-10).
                        skippedSystems.Add(sysAbbr);
                        continue;
                    }

                    foreach (string parameterName in parameterNames)
                    {
                        object raw = row[parameterName];
                        string parameterValue = raw == DBNull.Value ? "" : Convert.ToString(raw);
                        if (parameterValue == null) continue;

                        if (!guidByName.TryGetValue(parameterName, out Guid parGuid)) continue;
                        Parameter par = systemType.get_Parameter(parGuid);
                        if (par == null) continue;
                        par.Set(parameterValue);
                    }
                }
            });

            string feedback = sNumber + " Pipe Systems (Pipelines) initialized.\n";
            if (skippedSystems.Count > 0)
                feedback += $"\n{skippedSystems.Count} pipeline(s) had no LDT row for PROJECT-IDENTIFIER " +
                            $"'{cfg.ProjectIdentifier}' and were skipped:\n" +
                            string.Join("\n", skippedSystems.OrderBy(x => x));
            return feedback;
        }
    }
}
