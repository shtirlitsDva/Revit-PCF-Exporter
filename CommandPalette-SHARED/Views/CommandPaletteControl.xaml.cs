#nullable enable
using System.Windows.Controls;

namespace Norsyn.CommandPalette.Views
{
    // The Hybrid pane. All colours are self-contained in the XAML to match the
    // design handoff exactly; the DataContext (a PaletteViewModel) is supplied by
    // the dockable-pane provider at run time, or by the design-time sample data.
    public partial class CommandPaletteControl : UserControl
    {
        public CommandPaletteControl()
        {
            InitializeComponent();
        }
    }
}
