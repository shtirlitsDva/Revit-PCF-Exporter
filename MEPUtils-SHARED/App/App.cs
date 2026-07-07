using Autodesk.Revit.UI;

namespace MEPUtils.App
{
    /// <summary>
    /// Lifecycle host for MEPUtils — the Revit analog of AutoCAD's
    /// Initialize/Terminate. DevReload runs OnShutdown before every
    /// unload/reload, so everything a generation of this addin holds (currently
    /// the modeless 3D-rotation window and its ExternalEvent) must be released
    /// here; the next generation then starts clean instead of leaving a stale
    /// window running old code. The NorsynApps release host does not
    /// instantiate this — it only reflects the [DevReloadButton] commands.
    /// </summary>
    public class App : IExternalApplication
    {
        public Result OnStartup(UIControlledApplication application)
        {
            return Result.Succeeded;
        }

        public Result OnShutdown(UIControlledApplication application)
        {
            MEPUtils.Element3DRotation.Element3DRotationApp.Shutdown();
            return Result.Succeeded;
        }
    }
}
