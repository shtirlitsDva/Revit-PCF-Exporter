using System.Windows;

using Autodesk.Revit.UI;

using PCF_Exporter.ViewModels;

namespace PCF_Exporter.UI
{
    public partial class PcfExporterWindow : Window
    {
        PcfExporterViewModel vm = new();
        public PcfExporterWindow(ExternalCommandData cData)
        {
            InitializeComponent();
            vm.UIApp = cData.Application;
            DataContext = vm;
        }
    }
}
