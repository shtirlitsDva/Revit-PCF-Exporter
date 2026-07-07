#nullable enable
using System;

using Autodesk.Revit.UI;

namespace Norsyn.CommandPalette.Registration
{
    // Stable, shared pane GUIDs. Same values in every project that references Core,
    // so RegisterDockablePane dedupes correctly (first-til-mølle) and GetDockablePane
    // resolves the same pane everywhere.
    public static class PaneIds
    {
        public static readonly DockablePaneId Main =
            new DockablePaneId(new Guid("6F2B7B54-1C3A-4E9D-8B21-4C9E2A1D7F01"));

        public static readonly DockablePaneId Favorites =
            new DockablePaneId(new Guid("6F2B7B54-1C3A-4E9D-8B21-4C9E2A1D7F02"));
    }
}
