#nullable enable
using System.Windows.Input;
using System.Windows.Media;

using Norsyn.CommandPalette.Model;
using Norsyn.CommandPalette.Mvvm;

namespace Norsyn.CommandPalette.ViewModels
{
    // One command row/tile. Wraps a PaletteCommand and adds the mutable favorite
    // state plus the run / toggle-favorite commands (routed up to the palette VM).
    public sealed class CommandItemViewModel : ObservableObject
    {
        private readonly PaletteViewModel _owner;
        private bool _isFavorite;
        private ImageSource? _icon;
        private bool _iconResolved;

        public CommandItemViewModel(PaletteCommand model, PaletteViewModel owner)
        {
            Model = model;
            _owner = owner;
            CategoryBrush = PaletteBrushes.ForCategory(model.Category);
            RunCommand = new RelayCommand(() => _owner.Run(this));
            ToggleFavoriteCommand = new RelayCommand(() => _owner.ToggleFavorite(this));
        }

        public PaletteCommand Model { get; }

        public string Id => Model.Id;
        public string Label => Model.Label;
        public string? Tooltip => Model.Tooltip;
        public string Source => Model.Source;
        public string Category => Model.Category;
        public string Monogram => Model.Monogram;

        // Category colour for the monogram fallback / accents.
        public Brush CategoryBrush { get; }

        public bool IsFavorite
        {
            get => _isFavorite;
            set => SetProperty(ref _isFavorite, value);
        }

        // Lazily resolved embedded PNG; null → view draws the monogram square.
        public ImageSource? Icon
        {
            get
            {
                if (!_iconResolved)
                {
                    _icon = IconLoader.Load(Model.OwningAssembly, Model.Icon32 ?? Model.Icon16);
                    _iconResolved = true;
                }
                return _icon;
            }
        }

        public bool HasIcon => Icon != null;

        public ICommand RunCommand { get; }
        public ICommand ToggleFavoriteCommand { get; }
    }
}
