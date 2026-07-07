#nullable enable
using System;
using System.Reflection;

using Autodesk.Revit.UI;

using Norsyn.CommandPalette.Execution;
using Norsyn.CommandPalette.Mvvm;
using Norsyn.CommandPalette.Registration;
using Norsyn.CommandPalette.Registry;
using Norsyn.CommandPalette.Services;
using Norsyn.CommandPalette.ViewModels;
using Norsyn.CommandPalette.Views;

namespace Norsyn.CommandPalette
{
    // The public façade every project talks to. Lives in the single shared Core
    // binary, so the pane, its view-model and the command registry are one instance
    // no matter how many projects call in.
    //
    //   OnStartup(app):
    //     CommandPalette.EnsurePane(app);          // first caller creates the pane
    //     CommandPalette.Register(myAssembly);      // publish my [DevReloadButton]s
    //
    // Commands run through a single ExternalEvent (CommandInvoker) using an
    // ExternalCommandData captured from the pane's toggle button.
    public static class CommandPalette
    {
        private static readonly object _lock = new object();
        private static bool _initialised;

        private static CommandInvoker? _invoker;
        private static PaletteViewModel? _viewModel;
        private static CommandPaletteControl? _control;
        private static FavoritesStore? _favorites;
        private static FavoritesViewModel? _favoritesViewModel;
        private static FavoritesControl? _favoritesControl;

        // Build the singletons and register the dockable pane once per session.
        // Safe to call from every project's OnStartup — only the first wins.
        public static void EnsurePane(UIControlledApplication application)
        {
            lock (_lock)
            {
                if (_initialised) return;
                _initialised = true;

                _favorites = new FavoritesStore();
                _invoker = new CommandInvoker();
                _invoker.Attach(); // valid API context: we are in OnStartup

                _viewModel = new PaletteViewModel(_favorites, cmd => _invoker.Run(cmd))
                {
                    ToggleFavoritesCommand = new RelayCommand(ShowFavoritesPane),
                };
                _control = new CommandPaletteControl { DataContext = _viewModel };

                _favoritesViewModel = new FavoritesViewModel(
                    _favorites, () => CommandRegistry.Commands, cmd => _invoker.Run(cmd));
                _favoritesControl = new FavoritesControl { DataContext = _favoritesViewModel };

                CommandRegistry.Changed += OnRegistryChanged;
                _viewModel.SetCommands(CommandRegistry.Commands);

                try
                {
                    application.RegisterDockablePane(
                        PaneIds.Main, "Norsyn Commands",
                        new PaneContentProvider(_control, DockPosition.Right));
                    application.RegisterDockablePane(
                        PaneIds.Favorites, "Norsyn Favorites",
                        new PaneContentProvider(_favoritesControl, DockPosition.Floating));
                }
                catch (Autodesk.Revit.Exceptions.ArgumentException)
                {
                    // Another project registered them first this session — fine.
                }
            }
        }

        // Publish (or refresh) one assembly's [DevReloadButton] commands.
        public static void Register(Assembly assembly) => CommandRegistry.Register(assembly);

        public static void Unregister(Assembly assembly) => CommandRegistry.Unregister(assembly);

        // Called by the toggle command with the live command data, so pane clicks
        // have a context to execute against.
        public static void Capture(ExternalCommandData commandData) =>
            CommandInvoker.Captured = commandData;

        public static void TogglePane(UIApplication uiapp)
        {
            DockablePane pane = uiapp.GetDockablePane(PaneIds.Main);
            if (pane.IsShown()) pane.Hide();
            else pane.Show();
        }

        // Toggle the floating favorites pane. Uses the captured UIApplication, so
        // the main pane must have been opened once (its ribbon button captures it).
        private static void ShowFavoritesPane()
        {
            UIApplication? uiapp = CommandInvoker.Captured?.Application;
            if (uiapp == null) return;
            DockablePane pane = uiapp.GetDockablePane(PaneIds.Favorites);
            if (pane.IsShown()) pane.Hide();
            else pane.Show();
        }

        private static void OnRegistryChanged()
        {
            var control = _control;
            var vm = _viewModel;
            var favVm = _favoritesViewModel;
            if (control == null || vm == null) return;
            // Registry may change off the UI thread (e.g. DevReload) — marshal.
            control.Dispatcher.Invoke(() =>
            {
                vm.SetCommands(CommandRegistry.Commands);
                favVm?.Refresh();
            });
        }
    }
}
