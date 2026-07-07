#nullable enable
using System.Windows.Controls;

namespace Norsyn.CommandPalette.Views
{
    // The floating favorites pane content. DataContext is a FavoritesViewModel,
    // supplied by the façade at run time or the sample data at design time.
    public partial class FavoritesControl : UserControl
    {
        public FavoritesControl()
        {
            InitializeComponent();
        }
    }
}
