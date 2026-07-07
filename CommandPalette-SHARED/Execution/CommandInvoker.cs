#nullable enable
using System;
using System.Collections.Concurrent;

using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

using Norsyn.CommandPalette.Model;

namespace Norsyn.CommandPalette.Execution
{
    // Runs a pane-clicked command in a valid Revit API context. A dockable pane is
    // not inside a command, so it has no ExternalCommandData of its own — we reuse
    // the freshest one captured from the pane's toggle ribbon button (the proven
    // RevitAddInManager / DevReload pattern) and marshal the call onto the UI thread
    // via an ExternalEvent.
    public sealed class CommandInvoker : IExternalEventHandler
    {
        private readonly ConcurrentQueue<PaletteCommand> _queue =
            new ConcurrentQueue<PaletteCommand>();
        private ExternalEvent? _event;

        // The most recently captured live command data (from the toggle button).
        public static ExternalCommandData? Captured { get; set; }

        // Must be called in API context (OnStartup / a command). ExternalEvent.Create
        // throws elsewhere.
        public void Attach() => _event = ExternalEvent.Create(this);

        public void Run(PaletteCommand command)
        {
            _queue.Enqueue(command);
            _event?.Raise();
        }

        public void Execute(UIApplication app)
        {
            while (_queue.TryDequeue(out var command))
                RunOne(app, command);
        }

        private static void RunOne(UIApplication app, PaletteCommand command)
        {
            var data = Captured;
            if (data == null)
            {
                TaskDialog.Show("Norsyn Commands",
                    "Open the palette from its ribbon button once before running a " +
                    "command, so Revit can hand over a command context.");
                return;
            }

            try
            {
                Type? type = command.OwningAssembly.GetType(command.FullClassName);
                if (type == null ||
                    Activator.CreateInstance(type) is not IExternalCommand external)
                {
                    TaskDialog.Show("Norsyn Commands",
                        $"Could not create command '{command.Label}'.");
                    return;
                }

                string message = "";
                ElementSet elements = app.Application.Create.NewElementSet();
                Result result = external.Execute(data, ref message, elements);
                if (result == Result.Failed && !string.IsNullOrWhiteSpace(message))
                    TaskDialog.Show("Norsyn Commands", message);
            }
            catch (Autodesk.Revit.Exceptions.OperationCanceledException)
            {
                // user cancelled — silent
            }
            catch (Exception ex)
            {
                TaskDialog.Show("Norsyn Commands",
                    $"'{command.Label}' failed:\n{ex.Message}");
            }
        }

        public string GetName() => "Norsyn.CommandPalette.Invoker";
    }
}
