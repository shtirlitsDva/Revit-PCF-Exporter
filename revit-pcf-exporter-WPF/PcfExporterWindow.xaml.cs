using System.Windows;

namespace PCF_Exporter
{
    public partial class PcfExporterWindow : Window
    {
        public PcfExporterWindow(PcfExporterViewModel vm)
        {
            InitializeComponent();
            DataContext = vm;
        }
    }
}
