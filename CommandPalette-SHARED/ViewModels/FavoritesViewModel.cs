#nullable enable
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;

using Norsyn.CommandPalette.Model;
using Norsyn.CommandPalette.Mvvm;
using Norsyn.CommandPalette.Services;

namespace Norsyn.CommandPalette.ViewModels
{
    // The floating favorites pane: only starred commands, in the user's arranged
    // order, with a labels-on/off density toggle. Rebuilds whenever favorites or
    // the command set change.
    public sealed class FavoritesViewModel : ObservableObject
    {
        private readonly FavoritesStore _favorites;
        private readonly Func<IReadOnlyList<PaletteCommand>> _source;
        private readonly Action<PaletteCommand> _run;

        public FavoritesViewModel(
            FavoritesStore favorites,
            Func<IReadOnlyList<PaletteCommand>> source,
            Action<PaletteCommand> run)
        {
            _favorites = favorites;
            _source = source;
            _run = run;
            ToggleLabelsCommand = new RelayCommand(() => LabelsOn = !LabelsOn);
            _favorites.Changed += Rebuild;
            Rebuild();
        }

        public ObservableCollection<FavoriteItemViewModel> Items { get; } =
            new ObservableCollection<FavoriteItemViewModel>();

        public bool HasFavorites => Items.Count > 0;
        public bool IsEmpty => Items.Count == 0;

        public bool LabelsOn
        {
            get => _favorites.LabelsOn;
            set
            {
                if (_favorites.LabelsOn == value) return;
                _favorites.LabelsOn = value; // persists + raises Changed → Rebuild
                OnPropertyChanged();
                OnPropertyChanged(nameof(LabelsOff));
            }
        }

        public bool LabelsOff => !LabelsOn;

        public ICommand ToggleLabelsCommand { get; }

        // Called when the command registry changes (a source loaded/unloaded).
        public void Refresh() => Rebuild();

        // Drag-reorder from the pane: move <dragged> to <target>'s slot. Persists
        // the new arrangement, which raises Changed → Rebuild so Items re-sync.
        public void MoveItem(FavoriteItemViewModel dragged, FavoriteItemViewModel target)
        {
            if (dragged == null || target == null || ReferenceEquals(dragged, target)) return;
            int from = Items.IndexOf(dragged);
            int to = Items.IndexOf(target);
            if (from < 0 || to < 0 || from == to) return;

            var ids = Items.Select(i => i.Model.Id).ToList();
            ids.RemoveAt(from);
            ids.Insert(to, dragged.Model.Id);
            _favorites.SetOrder(ids);
        }

        // Drop onto empty space → send <dragged> to the end.
        public void MoveToEnd(FavoriteItemViewModel dragged)
        {
            if (dragged == null) return;
            int from = Items.IndexOf(dragged);
            if (from < 0 || from == Items.Count - 1) return;

            var ids = Items.Select(i => i.Model.Id).ToList();
            ids.RemoveAt(from);
            ids.Add(dragged.Model.Id);
            _favorites.SetOrder(ids);
        }

        private void Rebuild()
        {
            var byId = _source().ToDictionary(c => c.Id, c => c);
            Items.Clear();
            foreach (string id in _favorites.Order)
                if (byId.TryGetValue(id, out var cmd))
                    Items.Add(new FavoriteItemViewModel(cmd, _run));

            OnPropertyChanged(nameof(HasFavorites));
            OnPropertyChanged(nameof(IsEmpty));
            OnPropertyChanged(nameof(LabelsOn));
            OnPropertyChanged(nameof(LabelsOff));
        }
    }
}
