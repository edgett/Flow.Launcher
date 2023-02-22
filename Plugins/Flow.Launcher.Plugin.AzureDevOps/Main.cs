using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.Identity;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Controls;


namespace Flow.Launcher.Plugin.AzureDevOps
{
    public class Main : IAsyncPlugin, ISettingProvider
    {
        private PluginInitContext _context;
        private AzureDevOpsSettings _azureDevOpsSettings;

        public Main()
        {
            //Work around for Devops Auth Issue: https://github.com/microsoft/azure-devops-dotnet-samples/issues/57
            TypeDescriptor.AddAttributes(typeof(IdentityDescriptor), new TypeConverterAttribute(typeof(IdentityDescriptorConverter).FullName));
            TypeDescriptor.AddAttributes(typeof(SubjectDescriptor), new TypeConverterAttribute(typeof(SubjectDescriptorConverter).FullName));
        }

        public Control CreateSettingPanel()
        {
            
            var settingsUi = new AzureDevOpsPluginSettings(_azureDevOpsSettings);
            settingsUi.SettingsChanged += (sender, newSettings) =>
            {
                if (!_azureDevOpsSettings.Equals(newSettings))
                {
                    _azureDevOpsSettings.Update(newSettings);
                    _context.API.SaveSettingJsonStorage<AzureDevOpsSettings>();
                }
            };
            return settingsUi;
        }

        public Task InitAsync(PluginInitContext context)
        {
            _azureDevOpsSettings = _context.API.LoadSettingJsonStorage<AzureDevOpsSettings>();
            _context = context;
            return Task.FromResult(true);
        }

        public async Task<List<Result>> QueryAsync(Query query, CancellationToken token)
        {
            var results = new List<Result>();

            results.Add(new Result
            {
                Title = "Test",
                SubTitle = "Test",
                //IcoPath = "Images\\icon.png",
                Action = e =>
                {
                    _context.API.ChangeQuery("Test");
                    return false;
                }
            });


            return results;
            
        }
    }
}
