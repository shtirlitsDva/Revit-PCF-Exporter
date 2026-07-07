#nullable enable
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows.Input;
using System.Windows.Media;

using Norsyn.CommandPalette.Mvvm;

namespace Norsyn.CommandPalette.ViewModels
{
    // A collapsible group in the pane (a category, a source add-in, or — for the
    // A–Z view — a single headerless bucket). Owns its rows and its expand state.
    public sealed class CategoryGroupViewModel : ObservableObject
    {
        private bool _isExpanded = true;

        public CategoryGroupViewModel(
            string key, string header, Brush accent, bool hasHeader,
            IEnumerable<CommandItemViewModel> items, PaletteViewModel owner)
        {
            Key = key;
            Header = header;
            Accent = accent;
            HasHeader = hasHeader;
            Items = new ObservableCollection<CommandItemViewModel>(items);
            ToggleCommand = new RelayCommand(() =>
            {
                IsExpanded = !IsExpanded;
                owner.OnGroupToggled(this);
            });
        }

        public string Key { get; }
        public string Header { get; }
        public Brush Accent { get; }
        public bool HasHeader { get; }
        public int Count => Items.Count;
        public ObservableCollection<CommandItemViewModel> Items { get; }

        public bool IsExpanded
        {
            get => _isExpanded;
            set => SetProperty(ref _isExpanded, value);
        }

        public ICommand ToggleCommand { get; }
    }
}
