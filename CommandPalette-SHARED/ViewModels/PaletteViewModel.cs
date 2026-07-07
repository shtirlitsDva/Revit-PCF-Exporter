#nullable enable
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using System.Windows.Media;

using Norsyn.CommandPalette.Model;
using Norsyn.CommandPalette.Mvvm;
using Norsyn.CommandPalette.Services;

namespace Norsyn.CommandPalette.ViewModels
{
    public enum PaletteView { Category, Source, Az }

    // The Hybrid pane's view-model: one search field, a Category/Source/A–Z view
    // toggle, collapsible groups with Expand/Collapse-all, and favorite stars.
    // Grouping is done here (explicit group→item tree) so collapse state, header
    // colour, and counts are fully under our control; search force-expands.
    public sealed class PaletteViewModel : ObservableObject
    {
        private readonly FavoritesStore _favorites;
        private readonly Action<PaletteCommand> _run;
        private readonly Brush _sourceAccent;

        private List<CommandItemViewModel> _all = new List<CommandItemViewModel>();
        private readonly HashSet<string> _collapsed = new HashSet<string>();
        private string _searchText = "";
        private PaletteView _view = PaletteView.Category;
        private int _visibleCount;

        public PaletteViewModel(FavoritesStore favorites, Action<PaletteCommand> run)
        {
            _favorites = favorites;
            _run = run;
            _sourceAccent = MakeFrozen("#52667A");

            SetCategoryViewCommand = new RelayCommand(() => View = PaletteView.Category);
            SetSourceViewCommand = new RelayCommand(() => View = PaletteView.Source);
            SetAzViewCommand = new RelayCommand(() => View = PaletteView.Az);
            ExpandAllCommand = new RelayCommand(ExpandAll);
            CollapseAllCommand = new RelayCommand(CollapseAll);
            // Assigned by the façade to open the favorites pane; no-op until then
            // (and in design-time / standalone preview).
            ToggleFavoritesCommand = new RelayCommand(() => { });

            _favorites.Changed += RefreshFavoriteFlags;
        }

        public ObservableCollection<CategoryGroupViewModel> Groups { get; } =
            new ObservableCollection<CategoryGroupViewModel>();

        public string SearchText
        {
            get => _searchText;
            set { if (SetProperty(ref _searchText, value ?? "")) Rebuild(); }
        }

        public PaletteView View
        {
            get => _view;
            set
            {
                if (SetProperty(ref _view, value))
                {
                    OnPropertyChanged(nameof(IsCategoryView));
                    OnPropertyChanged(nameof(IsSourceView));
                    OnPropertyChanged(nameof(IsAzView));
                    Rebuild();
                }
            }
        }

        public bool IsCategoryView => _view == PaletteView.Category;
        public bool IsSourceView => _view == PaletteView.Source;
        public bool IsAzView => _view == PaletteView.Az;

        public int VisibleCount
        {
            get => _visibleCount;
            private set => SetProperty(ref _visibleCount, value);
        }

        // True while a search is active — the view then shows every group expanded
        // regardless of the collapse set, and hides the Expand/Collapse-all row.
        public bool IsSearching => _searchText.Trim().Length > 0;

        public ICommand SetCategoryViewCommand { get; }
        public ICommand SetSourceViewCommand { get; }
        public ICommand SetAzViewCommand { get; }
        public ICommand ExpandAllCommand { get; }
        public ICommand CollapseAllCommand { get; }
        // Opens the favorites mini-pane; set by the façade.
        public ICommand ToggleFavoritesCommand { get; set; }

        // Called by the façade when the registry changes (and once at startup).
        public void SetCommands(IReadOnlyList<PaletteCommand> commands)
        {
            _all = commands
                .Select(c => new CommandItemViewModel(c, this) { IsFavorite = _favorites.IsFavorite(c.Id) })
                .ToList();
            Rebuild();
        }

        public void Run(CommandItemViewModel item) => _run(item.Model);

        public void ToggleFavorite(CommandItemViewModel item)
        {
            _favorites.Toggle(item.Id);
            item.IsFavorite = _favorites.IsFavorite(item.Id);
        }

        // Persist the collapse of a single group (only meaningful when not searching).
        public void OnGroupToggled(CategoryGroupViewModel group)
        {
            if (IsSearching) return;
            if (group.IsExpanded) _collapsed.Remove(group.Key);
            else _collapsed.Add(group.Key);
        }

        private void ExpandAll()
        {
            _collapsed.Clear();
            foreach (var g in Groups) g.IsExpanded = true;
        }

        private void CollapseAll()
        {
            _collapsed.Clear();
            foreach (var g in Groups)
            {
                if (!g.HasHeader) continue; // headerless A–Z bucket can't collapse
                _collapsed.Add(g.Key);
                g.IsExpanded = false;
            }
        }

        private void RefreshFavoriteFlags()
        {
            foreach (var item in _all)
                item.IsFavorite = _favorites.IsFavorite(item.Id);
        }

        private void Rebuild()
        {
            OnPropertyChanged(nameof(IsSearching));
            string q = _searchText.Trim();
            IEnumerable<CommandItemViewModel> filtered = _all;
            if (q.Length > 0)
                filtered = _all.Where(i => Matches(i, q));

            var items = filtered.ToList();
            VisibleCount = items.Count;

            Groups.Clear();
            foreach (var g in BuildGroups(items))
                Groups.Add(g);
        }

        private static bool Matches(CommandItemViewModel i, string q) =>
            i.Label.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0 ||
            (i.Tooltip != null && i.Tooltip.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0);

        private IEnumerable<CategoryGroupViewModel> BuildGroups(List<CommandItemViewModel> items)
        {
            bool searching = IsSearching;

            if (_view == PaletteView.Az)
            {
                // Single headerless, always-expanded, alphabetical bucket.
                var az = items.OrderBy(i => i.Label, StringComparer.OrdinalIgnoreCase);
                yield return new CategoryGroupViewModel(
                    "az", "", Brushes.Transparent, hasHeader: false, az, this)
                { IsExpanded = true };
                yield break;
            }

            if (_view == PaletteView.Source)
            {
                var bySource = items
                    .GroupBy(i => i.Source)
                    .OrderBy(g => g.Key, StringComparer.OrdinalIgnoreCase);
                foreach (var g in bySource)
                    yield return MakeGroup(g.Key, g.Key, _sourceAccent, g, searching);
                yield break;
            }

            // Category view: fixed spec order, then name.
            var byCat = items
                .GroupBy(i => i.Category)
                .OrderBy(g => PaletteCategory.OrderOf(g.Key))
                .ThenBy(g => g.Key, StringComparer.OrdinalIgnoreCase);
            foreach (var g in byCat)
                yield return MakeGroup(g.Key, g.Key, PaletteBrushes.ForCategory(g.Key), g, searching);
        }

        private CategoryGroupViewModel MakeGroup(
            string key, string header, Brush accent,
            IEnumerable<CommandItemViewModel> members, bool searching)
        {
            var ordered = members.OrderBy(i => i.Label, StringComparer.OrdinalIgnoreCase);
            bool expanded = searching || !_collapsed.Contains(key);
            return new CategoryGroupViewModel(key, header, accent, hasHeader: true, ordered, this)
            {
                IsExpanded = expanded
            };
        }

        private static Brush MakeFrozen(string hex)
        {
            var b = new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex));
            b.Freeze();
            return b;
        }
    }
}
