#nullable enable
using System.Windows;

using Autodesk.Revit.UI;

namespace Norsyn.CommandPalette.Registration
{
    // Hands Revit the WPF content for a dockable pane. One instance per pane,
    // created once by whichever project wins the first-til-mølle registration.
    public sealed class PaneContentProvider : IDockablePaneProvider
    {
        private readonly FrameworkElement _content;
        private readonly DockPosition _initial;

        public PaneContentProvider(FrameworkElement content, DockPosition initial)
        {
            _content = content;
            _initial = initial;
        }

        public void SetupDockablePane(DockablePaneProviderData data)
        {
            data.FrameworkElement = _content;
            data.InitialState = new DockablePaneState
            {
                DockPosition = _initial,
            };
        }
    }
}
