#nullable enable
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

using Norsyn.CommandPalette.ViewModels;

namespace Norsyn.CommandPalette.Views
{
    // The floating favorites pane content. DataContext is a FavoritesViewModel,
    // supplied by the façade at run time or the sample data at design time.
    //
    // Favorites can be drag-reordered: press on a row/icon, drag past the system
    // threshold to begin, drop on another item to take its slot (or on empty space
    // to send it to the end). The new order is persisted by the view-model.
    public partial class FavoritesControl : UserControl
    {
        private Point _pressOrigin;
        private FavoriteItemViewModel? _dragCandidate;

        public FavoritesControl()
        {
            InitializeComponent();
        }

        // Tunnels in before the item Buttons handle the click, so we can note a
        // potential drag without stealing plain clicks (those still fire if the
        // pointer never moves past the drag threshold).
        private void OnItemsPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _pressOrigin = e.GetPosition(null);
            _dragCandidate = ItemUnder(e.OriginalSource);
        }

        private void OnItemsPreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton != MouseButtonState.Pressed || _dragCandidate == null) return;

            Vector moved = _pressOrigin - e.GetPosition(null);
            if (Math.Abs(moved.X) < SystemParameters.MinimumHorizontalDragDistance &&
                Math.Abs(moved.Y) < SystemParameters.MinimumVerticalDragDistance)
                return;

            FavoriteItemViewModel item = _dragCandidate;
            _dragCandidate = null; // a drag is now in flight; don't re-trigger
            var data = new DataObject(typeof(FavoriteItemViewModel), item);
            DragDrop.DoDragDrop((DependencyObject)sender, data, DragDropEffects.Move);
        }

        private void OnItemsDragOver(object sender, DragEventArgs e)
        {
            e.Effects = e.Data.GetDataPresent(typeof(FavoriteItemViewModel))
                ? DragDropEffects.Move
                : DragDropEffects.None;
            e.Handled = true;
        }

        private void OnItemsDrop(object sender, DragEventArgs e)
        {
            if (!(DataContext is FavoritesViewModel vm)) return;
            if (!e.Data.GetDataPresent(typeof(FavoriteItemViewModel))) return;

            var dragged = (FavoriteItemViewModel)e.Data.GetData(typeof(FavoriteItemViewModel));
            FavoriteItemViewModel? target = ItemUnder(e.OriginalSource);

            if (target == null) vm.MoveToEnd(dragged);
            else vm.MoveItem(dragged, target);
        }

        // Walk up the visual tree from the event source to the nearest element
        // whose DataContext is a favorite item.
        private static FavoriteItemViewModel? ItemUnder(object source)
        {
            DependencyObject? d = source as DependencyObject;
            while (d != null)
            {
                if (d is FrameworkElement fe && fe.DataContext is FavoriteItemViewModel vm)
                    return vm;
                d = VisualTreeHelper.GetParent(d);
            }
            return null;
        }
    }
}
