#nullable enable
using Norsyn.CommandPalette.ViewModels;

namespace Norsyn.CommandPalette.Design
{
    // x:Static hook for the XAML designer's d:DataContext, so the pane previews
    // with the 38 sample commands. Not used at run time.
    public static class DesignData
    {
        public static PaletteViewModel Palette { get; } = SampleData.DesignViewModel();
        public static FavoritesViewModel Favorites { get; } = SampleData.DesignFavoritesViewModel();
    }
}
