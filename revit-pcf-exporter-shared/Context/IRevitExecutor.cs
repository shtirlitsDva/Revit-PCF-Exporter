using System;
using System.Threading.Tasks;

namespace PcfExporter.Context
{
    /// <summary>
    /// Marshals work from the modeless WPF window onto a valid Revit API context.
    /// Implemented with an ExternalEvent: the returned Task completes after Revit
    /// has executed the work (or faulted with the thrown exception).
    /// </summary>
    public interface IRevitExecutor
    {
        Task RunAsync(string name, Action<IRevitContext> work);
        Task<T> RunAsync<T>(string name, Func<IRevitContext, T> work);
    }
}
