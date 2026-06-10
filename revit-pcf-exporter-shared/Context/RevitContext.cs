using System;
using System.Collections.Generic;
using System.Linq;

using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace PcfExporter.Context
{
    public sealed class RevitContext : IRevitContext
    {
        public RevitContext(UIApplication uiApplication)
        {
            UiApplication = uiApplication ?? throw new ArgumentNullException(nameof(uiApplication));
            UiDocument = uiApplication.ActiveUIDocument
                ?? throw new InvalidOperationException("No active document in Revit.");
            Doc = UiDocument.Document;
        }

        public UIApplication UiApplication { get; }
        public UIDocument UiDocument { get; }
        public Document Doc { get; }

        public ICollection<ElementId> SelectedIds => UiDocument.Selection.GetElementIds();

        public void SetSelection(IEnumerable<ElementId> ids) =>
            UiDocument.Selection.SetElementIds(ids.ToList());

        public void RunInTransaction(string name, Action work) =>
            RunInTransaction<object>(name, () => { work(); return null; });

        public T RunInTransaction<T>(string name, Func<T> work)
        {
            using (var tx = new Transaction(Doc, name))
            {
                tx.Start();
                try
                {
                    T result = work();
                    tx.Commit();
                    return result;
                }
                catch
                {
                    if (tx.GetStatus() == TransactionStatus.Started) tx.RollBack();
                    throw;
                }
            }
        }
    }
}
