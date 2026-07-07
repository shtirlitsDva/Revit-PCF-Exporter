#nullable enable
using System;

using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

using Shared;

namespace MEPUtils.App
{
    // Thin [DevReloadButton] wrappers for MEPUtils utilities whose entry point
    // is a plain helper (Func<UIApplication,Result> / Func<ExternalCommandData,
    // Result>) rather than an IExternalCommand. Each helper opens its own
    // transaction and returns a Result, so these wrappers only adapt the
    // IExternalCommand signature and normalise cancellation/exception handling.
    //
    // Commands sharing a Group collapse into one pulldown ("category flyout");
    // the six pre-existing dedicated buttons live in App/RibbonButtons.cs and
    // stay flat on the "MEP" panel. Icons are generated monogram PNGs under
    // App/Resources (ImgMU*.png).

    internal static class WrapperResult
    {
        // Every wrapper funnels through here for identical cancel/fail handling.
        public static Result Run(ref string message, Func<Result> body)
        {
            try { return body(); }
            catch (Autodesk.Revit.Exceptions.OperationCanceledException) { return Result.Cancelled; }
            catch (Exception ex) { message = ex.Message; return Result.Failed; }
        }
    }

    // ---------------------------------------------------------------- Insulation
    [Transaction(TransactionMode.Manual)]
    [DevReloadButton(Text = "Create all insulation", Tooltip = "Create insulation on all pipes and accessories.",
        Group = "Insulation", Panel = "MEP", Order = 100,
        Icon16 = "ImgMUInsCreateAll16.png", Icon32 = "ImgMUInsCreateAll32.png")]
    public class InsulationCreateAllCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData cData, ref string message, ElementSet elements)
            => WrapperResult.Run(ref message,
                () => MEPUtils.InsulationHandler.InsulationHandler.CreateAllInsulation(cData.Application));
    }

    [Transaction(TransactionMode.Manual)]
    [DevReloadButton(Text = "Delete all insulation", Tooltip = "Delete all pipe insulation from the model.",
        Group = "Insulation", Panel = "MEP", Order = 101,
        Icon16 = "ImgMUInsDeleteAll16.png", Icon32 = "ImgMUInsDeleteAll32.png")]
    public class InsulationDeleteAllCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData cData, ref string message, ElementSet elements)
            => WrapperResult.Run(ref message,
                () => MEPUtils.InsulationHandler.InsulationHandler.DeleteAllPipeInsulation(cData.Application));
    }

    [Transaction(TransactionMode.Manual)]
    [DevReloadButton(Text = "Insulation settings", Tooltip = "Edit insulation settings.",
        Group = "Insulation", Panel = "MEP", Order = 102,
        Icon16 = "ImgMUInsSettings16.png", Icon32 = "ImgMUInsSettings32.png")]
    public class InsulationSettingsCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData cData, ref string message, ElementSet elements)
            => WrapperResult.Run(ref message,
                () => new MEPUtils.InsulationHandler.InsulationHandler().ExecuteInsulationSettings(cData.Application));
    }

    // ------------------------------------------------------------ Pipe & Geometry
    [Transaction(TransactionMode.Manual)]
    [DevReloadButton(Text = "Create flanges", Tooltip = "Create flanges for the selected elements.",
        Group = "Pipe & Geometry", Panel = "MEP", Order = 110,
        Icon16 = "ImgMUFlanges16.png", Icon32 = "ImgMUFlanges32.png")]
    public class FlangeCreatorCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData cData, ref string message, ElementSet elements)
            => WrapperResult.Run(ref message,
                () => MEPUtils.FlangeCreator.CreateFlangeForElements(cData.Application));
    }

    [Transaction(TransactionMode.Manual)]
    [DevReloadButton(Text = "Pipe from connector", Tooltip = "Draw a pipe from a picked connector.",
        Group = "Pipe & Geometry", Panel = "MEP", Order = 111,
        Icon16 = "ImgMUPipeFromConn16.png", Icon32 = "ImgMUPipeFromConn32.png")]
    public class PipeCreatorCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData cData, ref string message, ElementSet elements)
            => WrapperResult.Run(ref message,
                () => MEPUtils.PipeCreator.CreatePipeFromConnector(cData.Application));
    }

    [Transaction(TransactionMode.Manual)]
    [DevReloadButton(Text = "Move to distance", Tooltip = "Move an element to a typed distance (special-cases Olets).",
        Group = "Pipe & Geometry", Panel = "MEP", Order = 112,
        Icon16 = "ImgMUMoveToDist16.png", Icon32 = "ImgMUMoveToDist32.png")]
    public class MoveToDistanceCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData cData, ref string message, ElementSet elements)
            => WrapperResult.Run(ref message,
                () => MEPUtils.MoveToDistance.MoveToDistance.Move(cData.Application));
    }

    // ----------------------------------------------------------- Instrumentation
    [Transaction(TransactionMode.Manual)]
    [DevReloadButton(Text = "Create instrument", Tooltip = "Place instrumentation families along pipes.",
        Group = "Instrumentation", Panel = "MEP", Order = 120,
        Icon16 = "ImgMUInstr16.png", Icon32 = "ImgMUInstr32.png")]
    public class CreateInstrumentationCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData cData, ref string message, ElementSet elements)
            => WrapperResult.Run(ref message,
                () => MEPUtils.CreateInstrumentation.StartCreatingInstrumentation.StartCreating(cData.Application));
    }

    [Transaction(TransactionMode.Manual)]
    [DevReloadButton(Text = "Create instrument (NN)", Tooltip = "Place instrumentation families (NN / 2022 generation).",
        Group = "Instrumentation", Panel = "MEP", Order = 121,
        Icon16 = "ImgMUInstrNN16.png", Icon32 = "ImgMUInstrNN32.png")]
    public class CreateInstrumentationNNCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData cData, ref string message, ElementSet elements)
            => WrapperResult.Run(ref message,
                () => MEPUtils.CreateInstrumentation.StartCreatingInstrumentationNN.StartCreating(cData.Application));
    }

    // ------------------------------------------------------------- Piping Systems
    [Transaction(TransactionMode.Manual)]
    [DevReloadButton(Text = "Add PS view-filters", Tooltip = "Add view-filters for all piping-system types to the current view.",
        Group = "Piping Systems", Panel = "MEP", Order = 141,
        Icon16 = "ImgMUPsAddFilters16.png", Icon32 = "ImgMUPsAddFilters32.png")]
    public class AddPsFiltersCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData cData, ref string message, ElementSet elements)
            => WrapperResult.Run(ref message,
                () => new MEPUtils.PipingSystemsAndFilters.AddAllPipingSystemTypesFiltersToView().Execute(cData.Application));
    }

    [Transaction(TransactionMode.Manual)]
    [DevReloadButton(Text = "Isolate selected PS", Tooltip = "Isolate the piping systems of the selected elements.",
        Group = "Piping Systems", Panel = "MEP", Order = 142,
        Icon16 = "ImgMUPsIsolate16.png", Icon32 = "ImgMUPsIsolate32.png")]
    public class IsolatePsCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData cData, ref string message, ElementSet elements)
            => WrapperResult.Run(ref message,
                () => new MEPUtils.PipingSystemsAndFilters.IsolatePipingSystemsOfSelectedElements().Execute(cData.Application));
    }

    [Transaction(TransactionMode.Manual)]
    [DevReloadButton(Text = "Hide selected PS", Tooltip = "Hide the piping systems of the selected elements.",
        Group = "Piping Systems", Panel = "MEP", Order = 143,
        Icon16 = "ImgMUPsHide16.png", Icon32 = "ImgMUPsHide32.png")]
    public class HidePsCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData cData, ref string message, ElementSet elements)
            => WrapperResult.Run(ref message,
                () => new MEPUtils.PipingSystemsAndFilters.HidePipingSystemsOfSelectedElements().Execute(cData.Application));
    }

    // -------------------------------------------------------- Parameters & Tagging
    [Transaction(TransactionMode.Manual)]
    [DevReloadButton(Text = "(Re-)Number", Tooltip = "(Re-)number families by TAG rules.",
        Group = "Parameters & Tagging", Panel = "MEP", Order = 151,
        Icon16 = "ImgMUNumberStuff16.png", Icon32 = "ImgMUNumberStuff32.png")]
    public class NumberStuffCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData cData, ref string message, ElementSet elements)
            => WrapperResult.Run(ref message,
                () => new MEPUtils.NumberStuff.NumberStuff().NumberStuffMethod(cData.Application));
    }

    [Transaction(TransactionMode.Manual)]
    [DevReloadButton(Text = "Copy flow data", Tooltip = "Copy flow data across fittings (elbow/tee/etc.).",
        Group = "Parameters & Tagging", Panel = "MEP", Order = 155,
        Icon16 = "ImgMUFlowCopy16.png", Icon32 = "ImgMUFlowCopy32.png")]
    public class FlowCopyCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData cData, ref string message, ElementSet elements)
            => WrapperResult.Run(ref message,
                () => new MEPUtils.FlowCopy.FlowCopy().FlowCopyMethod(cData));
    }

    // ------------------------------------------------------------- Analysis & QA
    [Transaction(TransactionMode.Manual)]
    [DevReloadButton(Text = "Total length", Tooltip = "Sum total length of pipe lines/systems (report only).",
        Group = "Analysis & QA", Panel = "MEP", Order = 180,
        Icon16 = "ImgMUTotalLength16.png", Icon32 = "ImgMUTotalLength32.png")]
    public class TotalLineLengthCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData cData, ref string message, ElementSet elements)
            => WrapperResult.Run(ref message,
                () => MEPUtils.TotalLineLength.TotalLineLengths(cData.Application));
    }

    [Transaction(TransactionMode.Manual)]
    [DevReloadButton(Text = "Count welds", Tooltip = "Count welds along the selected piping (report only).",
        Group = "Analysis & QA", Panel = "MEP", Order = 181,
        Icon16 = "ImgMUCountWelds16.png", Icon32 = "ImgMUCountWelds32.png")]
    public class CountWeldsCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData cData, ref string message, ElementSet elements)
            => WrapperResult.Run(ref message,
                () => new MEPUtils.CountWelds.CountWelds().CountWeldsMethod(cData.Application));
    }

    // -------------------------------------------------------- Flat (single) button
    [Transaction(TransactionMode.Manual)]
    [DevReloadButton(Text = "Support Tools", Tooltip = "Open the support-tools menu (heights, loads, R2 sync, place supports).",
        Panel = "MEP", Order = 7,
        Icon16 = "ImgMUSupportToolsMenu16.png", Icon32 = "ImgMUSupportToolsMenu32.png")]
    public class SupportToolsMenuCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData cData, ref string message, ElementSet elements)
            => WrapperResult.Run(ref message,
                () => MEPUtils.SupportTools.SupportToolsMain.CallForm(cData.Application));
    }
}
