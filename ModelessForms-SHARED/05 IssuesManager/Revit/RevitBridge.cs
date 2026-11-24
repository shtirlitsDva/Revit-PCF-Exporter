using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using ModelessForms.IssuesManager.ViewModels;

namespace ModelessForms.IssuesManager.Revit
{
    /// <summary>
    /// The Issue Tracker's own ExternalEvent. A modeless window may not call the
    /// Revit API directly; requests are queued here and run when Revit raises the
    /// event in a valid API context. Each request completes its own Task, so two
    /// quick requests never overwrite each other (unlike a single shared slot).
    /// </summary>
    public sealed class RevitBridge : IRevitBridge, IExternalEventHandler, IDisposable
    {
        private readonly object _lock = new object();
        private readonly Queue<Action<UIApplication>> _queue = new Queue<Action<UIApplication>>();
        private readonly ExternalEvent _event;

        /// <summary>Must be created in a Revit API context (e.g. inside an IExternalCommand).</summary>
        public RevitBridge()
        {
            _event = ExternalEvent.Create(this);
        }

        public Task<IReadOnlyList<string>> GetSelectedElementUniqueIdsAsync() => Run(app =>
        {
            var uidoc = ActiveDocument(app);
            var doc = uidoc.Document;
            IReadOnlyList<string> ids = uidoc.Selection.GetElementIds()
                .Select(id => doc.GetElement(id))
                .Where(e => e != null)
                .Select(e => e.UniqueId)
                .ToList();
            return ids;
        });

        public Task<int> SelectElementsAsync(IReadOnlyList<string> uniqueIds) => Run(app =>
        {
            var uidoc = ActiveDocument(app);
            var doc = uidoc.Document;
            var found = uniqueIds
                .Select(uid => doc.GetElement(uid))
                .Where(e => e != null)
                .Select(e => e.Id)
                .ToList();

            if (found.Count > 0)
            {
                uidoc.Selection.SetElementIds(found);
                uidoc.ShowElements(found);
            }
            return uniqueIds.Count - found.Count;
        });

        private static UIDocument ActiveDocument(UIApplication app) =>
            app.ActiveUIDocument ?? throw new InvalidOperationException("No Revit document is open. Open a model and try again.");

        private Task<T> Run<T>(Func<UIApplication, T> work)
        {
            var tcs = new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);
            Action<UIApplication> request = app =>
            {
                try { tcs.SetResult(work(app)); }
                catch (Exception ex) { tcs.SetException(ex); }
            };

            lock (_lock) _queue.Enqueue(request);

            var result = _event.Raise();
            if (result != ExternalEventRequest.Accepted && result != ExternalEventRequest.Pending)
            {
                lock (_lock)
                {
                    // Take our request out again, so it can never run later.
                    var remaining = _queue.Where(r => !ReferenceEquals(r, request)).ToList();
                    _queue.Clear();
                    foreach (var r in remaining) _queue.Enqueue(r);
                }
                tcs.TrySetException(new InvalidOperationException(
                    $"Revit did not accept the request ({result}). Finish the current Revit command and try again."));
            }
            return tcs.Task;
        }

        public void Execute(UIApplication app)
        {
            while (true)
            {
                Action<UIApplication> request;
                lock (_lock)
                {
                    if (_queue.Count == 0) return;
                    request = _queue.Dequeue();
                }
                request(app); // never throws: each request completes its own Task
            }
        }

        public string GetName() => "Issue Tracker";

        public void Dispose() => _event.Dispose();
    }
}
