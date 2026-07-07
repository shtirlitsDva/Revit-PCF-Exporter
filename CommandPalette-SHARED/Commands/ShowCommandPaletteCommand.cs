#nullable enable
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace Norsyn.CommandPalette.Commands
{
    // The ribbon button that shows/hides the pane. Also the single point where a
    // live ExternalCommandData is captured for the pane's out-of-band command runs.
    // Hosts create a PushButton pointing at this class; it carries NO
    // [DevReloadButton], so it never lists itself inside the pane.
    [Transaction(TransactionMode.Manual)]
    public sealed class ShowCommandPaletteCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            CommandPalette.Capture(commandData);
            CommandPalette.TogglePane(commandData.Application);
            return Result.Succeeded;
        }
    }
}
