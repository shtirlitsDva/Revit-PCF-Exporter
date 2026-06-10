using System;
using System.Collections.Generic;

using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace PcfExporter.Context
{
    /// <summary>
    /// The Revit world as seen by one operation. Constructed fresh from the active
    /// UIApplication every time work runs, so a stale Document is impossible by construction.
    /// </summary>
    public interface IRevitContext
    {
        UIApplication UiApplication { get; }
        UIDocument UiDocument { get; }
        Document Doc { get; }
        ICollection<ElementId> SelectedIds { get; }
        void SetSelection(IEnumerable<ElementId> ids);
        /// <summary>Runs work inside a named transaction and commits it. Rolls back on exception.</summary>
        void RunInTransaction(string name, Action work);
        T RunInTransaction<T>(string name, Func<T> work);
    }
}
