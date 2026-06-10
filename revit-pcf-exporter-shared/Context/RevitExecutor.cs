using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;

using Autodesk.Revit.UI;

namespace PcfExporter.Context
{
    /// <summary>
    /// ExternalEvent-based executor. Must be constructed in a valid Revit API context
    /// (e.g. inside IExternalCommand.Execute) because ExternalEvent.Create requires it.
    /// Thereafter the modeless UI can post work at any time.
    /// </summary>
    public sealed class RevitExecutor : IRevitExecutor, IExternalEventHandler, IDisposable
    {
        private sealed class WorkItem
        {
            public string Name;
            public Func<IRevitContext, object> Work;
            public TaskCompletionSource<object> Completion;
        }

        private readonly ConcurrentQueue<WorkItem> _queue = new ConcurrentQueue<WorkItem>();
        private readonly ExternalEvent _externalEvent;
        private volatile string _currentName = "PCF Exporter";
        private volatile bool _disposed;

        public RevitExecutor()
        {
            _externalEvent = ExternalEvent.Create(this);
        }

        public Task RunAsync(string name, Action<IRevitContext> work) =>
            RunAsync<object>(name, ctx => { work(ctx); return null; });

        public Task<T> RunAsync<T>(string name, Func<IRevitContext, T> work)
        {
            if (_disposed) throw new ObjectDisposedException(nameof(RevitExecutor),
                "The exporter window was closed; its Revit executor is gone.");
            var tcs = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);
            _queue.Enqueue(new WorkItem
            {
                Name = name,
                Work = ctx => work(ctx),
                Completion = tcs
            });
            _externalEvent.Raise();
            return Cast<T>(tcs.Task);
        }

        private static async Task<T> Cast<T>(Task<object> task) => (T)await task.ConfigureAwait(true);

        public void Execute(UIApplication app)
        {
            while (_queue.TryDequeue(out WorkItem item))
            {
                _currentName = item.Name;
                try
                {
                    var ctx = new RevitContext(app);
                    item.Completion.TrySetResult(item.Work(ctx));
                }
                catch (Exception ex)
                {
                    item.Completion.TrySetException(ex);
                }
            }
        }

        public string GetName() => _currentName;

        public void Dispose()
        {
            _disposed = true;
            //Work that was queued but never executed (window closed before Revit
            //processed the event) must not dangle forever — cancel it loudly.
            while (_queue.TryDequeue(out WorkItem item))
                item.Completion.TrySetCanceled();
            _externalEvent.Dispose();
        }
    }
}
