using System.Windows;

using PcfExporter.UI.ViewModels;

namespace PcfExporter.UI.Views
{
    public partial class MainWindow : Window
    {
        private readonly MainViewModel _viewModel;

        public MainWindow(MainViewModel viewModel)
        {
            InitializeComponent();
            DarkTitleBar.Apply(this);
            _viewModel = viewModel;
            DataContext = viewModel;
            Loaded += async (s, e) => await _viewModel.InitializeAsync();
        }
    }
}
