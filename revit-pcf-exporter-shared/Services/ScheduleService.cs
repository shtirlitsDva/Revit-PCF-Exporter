using System.Collections.Generic;
using System.Linq;

using Autodesk.Revit.DB;

using PcfExporter.Context;

using pdef = PcfExporter.Model.ParameterDefinition;
using plst = PcfExporter.Model.Parameters;

namespace PcfExporter.Services
{
    /// <summary>Creates the three PCF review schedules in the project.</summary>
    public interface IScheduleService
    {
        void CreatePcfSchedules(IRevitContext ctx);
    }

    public sealed class ScheduleService : IScheduleService
    {
        public void CreatePcfSchedules(IRevitContext ctx)
        {
            Document doc = ctx.Doc;

            var sharedParameters = new FilteredElementCollector(doc)
                .OfClass(typeof(SharedParameterElement))
                .Cast<SharedParameterElement>()
                .ToList();

            List<pdef> elemUserParameters = plst.LPAll()
                .Where(p => p.Usage == Model.ParameterUsage.USER && p.Domain == Model.ParameterDomain.ELEM)
                .ToList();

            ctx.RunInTransaction("Create items schedules", () =>
            {
                #region Schedule ALL elements
                ViewSchedule schedAll = ViewSchedule.CreateSchedule(
                    doc, ElementId.InvalidElementId, ElementId.InvalidElementId);
                schedAll.Name = "PCF - ALL Elements";
                schedAll.Definition.IsItemized = false;

                AddFamilyAndTypeSortedField(doc, schedAll);
                foreach (pdef pDef in elemUserParameters)
                    AddParameterField(doc, schedAll, sharedParameters, pDef,
                        addHasParameterFilter: pDef.Name == "PCF_ELEM_TYPE",
                        addNotEmptyFilter: false);
                #endregion

                #region Schedule FILTERED elements
                ViewSchedule schedFilter = ViewSchedule.CreateSchedule(
                    doc, ElementId.InvalidElementId, ElementId.InvalidElementId);
                schedFilter.Name = "PCF - Filtered Elements";
                schedFilter.Definition.IsItemized = false;

                AddFamilyAndTypeSortedField(doc, schedFilter);
                foreach (pdef pDef in elemUserParameters)
                    AddParameterField(doc, schedFilter, sharedParameters, pDef,
                        addHasParameterFilter: pDef.Name == "PCF_ELEM_TYPE",
                        addNotEmptyFilter: pDef.Name == "PCF_ELEM_TYPE");
                #endregion

                #region Schedule Pipelines
                ViewSchedule schedPipeline = ViewSchedule.CreateSchedule(
                    doc, new ElementId(BuiltInCategory.OST_PipingSystem), ElementId.InvalidElementId);
                schedPipeline.Name = "PCF - Pipelines";
                schedPipeline.Definition.IsItemized = false;

                AddFamilyAndTypeSortedField(doc, schedPipeline);

                var pipelineParameters = new List<pdef>
                {
                    plst.PCF_PIPL_LINEID, plst.PCF_PIPL_NOMCLASS, plst.PCF_PIPL_TEMP,
                    plst.PCF_PIPL_AREA, plst.PCF_PIPL_PROJID, plst.PCF_PIPL_DATE,
                    plst.PCF_PIPL_DWGNAME, plst.PCF_PIPL_REV, plst.PCF_PIPL_TEGN,
                    plst.PCF_PIPL_KONTR, plst.PCF_PIPL_GODK
                };
                foreach (pdef pDef in pipelineParameters)
                    AddParameterField(doc, schedPipeline, sharedParameters, pDef,
                        addHasParameterFilter: false, addNotEmptyFilter: false);
                #endregion
            });
        }

        private static void AddFamilyAndTypeSortedField(Document doc, ViewSchedule schedule)
        {
            foreach (SchedulableField schField in schedule.Definition.GetSchedulableFields())
            {
                if (schField.GetName(doc) != "Family and Type") continue;
                ScheduleField field = schedule.Definition.AddField(schField);
                schedule.Definition.AddSortGroupField(new ScheduleSortGroupField(field.FieldId));
            }
        }

        private static void AddParameterField(
            Document doc, ViewSchedule schedule, List<SharedParameterElement> sharedParameters,
            pdef pDef, bool addHasParameterFilter, bool addNotEmptyFilter)
        {
            SharedParameterElement parameter = sharedParameters
                .FirstOrDefault(p => p.GuidValue.CompareTo(pDef.Guid) == 0);
            if (parameter == null)
                throw new System.Exception(
                    $"Shared parameter {pDef.Name} is not present in the project — " +
                    "run 'Import PCF parameters' before creating schedules.");

            SchedulableField queryField = schedule.Definition.GetSchedulableFields()
                .FirstOrDefault(fld => fld.ParameterId.IntegerValue == parameter.Id.IntegerValue);
            if (queryField == null)
                throw new System.Exception(
                    $"Parameter {pDef.Name} exists but is not schedulable for {schedule.Name}.");

            ScheduleField field = schedule.Definition.AddField(queryField);
            if (addHasParameterFilter)
                schedule.Definition.AddFilter(
                    new ScheduleFilter(field.FieldId, ScheduleFilterType.HasParameter));
            if (addNotEmptyFilter)
                schedule.Definition.AddFilter(
                    new ScheduleFilter(field.FieldId, ScheduleFilterType.NotEqual, ""));
        }
    }
}
