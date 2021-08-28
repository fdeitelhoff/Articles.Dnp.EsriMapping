using System.Windows;

namespace Twainsoft.Articles.Dnp.EsriMapping
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            
            Esri.ArcGISRuntime.ArcGISRuntimeEnvironment.ApiKey
                = "AAPK565a27264a19496bb396bb79163e25f4-G4RBUukyZqSlJXU5Td4KkMDwOVMNuCJPcSnVDnhvHw7zj8CNjPRfOSv6j2G5YfB";
        }
    }
}