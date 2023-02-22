using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Flow.Launcher.Plugin.AzureDevOps
{
    /// <summary>
    /// Interaction logic for AzureDevOpsPluginSettings.xaml
    /// </summary>
    public partial class AzureDevOpsPluginSettings : UserControl
    {
        private AzureDevOpsPluginSettingsViewModel ViewModel { get; set; } = new AzureDevOpsPluginSettingsViewModel();
        
        public AzureDevOpsPluginSettings(AzureDevOpsSettings azureDevopsSettings)
        {
            InitializeComponent();
            dpSettings.DataContext = ViewModel;
            ViewModel.DevOpsUrl = azureDevopsSettings.DevOpsUrl;
            ViewModel.DevOpsPat = azureDevopsSettings.DevOpsPat;
            ViewModel.PropertyChanged += ViewModel_PropertyChanged;
        }

        private void ViewModel_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            var newSettings = ViewModel.GetAzureDevopsSettings();
            SettingsChanged.Invoke(this, newSettings);
        }

        private async void btnCheckToken_Click(object sender, RoutedEventArgs e)
        {
            await ViewModel?.CheckPat();
        }

        public delegate void SettingsChangedEventHandler(object sender, AzureDevOpsSettings e);

        public event SettingsChangedEventHandler? SettingsChanged;




    }
}
