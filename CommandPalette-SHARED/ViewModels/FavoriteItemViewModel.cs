#nullable enable
using System;
using System.Windows.Input;
using System.Windows.Media;

using Norsyn.CommandPalette.Model;
using Norsyn.CommandPalette.Mvvm;

namespace Norsyn.CommandPalette.ViewModels
{
    // A single entry in the floating favorites pane. Lighter than the main-pane
    // row: it only needs to display and run (favorites are added/removed via the
    // stars in the main pane).
    public sealed class FavoriteItemViewModel
    {
        public FavoriteItemViewModel(PaletteCommand model, Action<PaletteCommand> run)
        {
            Model = model;
            Label = model.Label;
            Tooltip = model.Tooltip;
            Monogram = model.Monogram;
            CategoryBrush = PaletteBrushes.ForCategory(model.Category);
            Icon = IconLoader.Load(model.OwningAssembly, model.Icon32 ?? model.Icon16);
            RunCommand = new RelayCommand(() => run(model));
        }

        public PaletteCommand Model { get; }
        public string Label { get; }
        public string? Tooltip { get; }
        public string Monogram { get; }
        public Brush CategoryBrush { get; }
        public ImageSource? Icon { get; }
        public bool HasIcon => Icon != null;
        public ICommand RunCommand { get; }
    }
}
