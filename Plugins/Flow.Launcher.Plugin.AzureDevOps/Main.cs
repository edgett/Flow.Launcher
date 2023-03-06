using Flow.Launcher.Plugin.AzureDevOps.AzureDevOpsService;
using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.Identity;
using Microsoft.VisualStudio.Services.WebApi;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Flow.Launcher.Plugin.AzureDevOps
{
    public class Main : IAsyncPlugin, ISettingProvider, IResultUpdated
    {
        private PluginInitContext _context;
        private ProjectService _projectService;
        private WorkItemSearchService? _workItemSearchService;
        private WorkItemTypeService _workItemTypeService;
        private WorkItemImageService _workItemImageService;
        private WorkItemService _workItemService;
        private AzureDevOpsSettings _azureDevOpsSettings;

        public Main()
        {
            //Work around for Devops Auth Issue: https://github.com/microsoft/azure-devops-dotnet-samples/issues/57
            TypeDescriptor.AddAttributes(typeof(IdentityDescriptor), new TypeConverterAttribute(typeof(IdentityDescriptorConverter).FullName));
            TypeDescriptor.AddAttributes(typeof(SubjectDescriptor), new TypeConverterAttribute(typeof(SubjectDescriptorConverter).FullName));
        }

        public event ResultUpdatedEventHandler ResultsUpdated;

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

        public async Task InitAsync(PluginInitContext context)
        {
            _context = context;
            _azureDevOpsSettings = _context.API.LoadSettingJsonStorage<AzureDevOpsSettings>();

            var (configOk, message) = await ConfigService.CheckConfig(_azureDevOpsSettings);
            if (configOk) {
                var azureDevOpsConfg = new ConfigService(_azureDevOpsSettings);
                _projectService = new ProjectService(azureDevOpsConfg);
                _workItemSearchService = new WorkItemSearchService(azureDevOpsConfg, _projectService);
                _workItemTypeService = new WorkItemTypeService(azureDevOpsConfg);
                _workItemImageService = new WorkItemImageService(_workItemTypeService);
                _workItemService = new WorkItemService(azureDevOpsConfg);
                
            }
        }

        public async Task<List<Result>> QueryAsync(Query query, CancellationToken cancellationToken)
         {
            var results = new List<Result>();
            var userInputSplit = query.Search.Split(" ");
            var devOpsSearch = "";
            //Clean out the 
            if (_context.CurrentPluginMetadata.ActionKeywords.Contains(userInputSplit[0]))
            {
                devOpsSearch = (userInputSplit.Length > 1) ? string.Join(" ", userInputSplit.Skip(1).Where(inputToken => !string.IsNullOrWhiteSpace(inputToken))) : userInputSplit[0];
            }
            else 
            {
                devOpsSearch = string.Join(" ", userInputSplit.Where(inputToken => !string.IsNullOrWhiteSpace(inputToken)));
            }

            if (string.IsNullOrWhiteSpace(devOpsSearch))
            {
                return new List<Result>();
            }

            
            if (_workItemService != null)
            {
                var workItems = _workItemSearchService.SearchWorkItems(devOpsSearch, cancellationToken);

                var workItemsEnum = workItems.GetAsyncEnumerator(cancellationToken);

                while (await workItemsEnum.MoveNextAsync())
                {
                    var workItem = workItemsEnum.Current;
                    var wiTitle = (string)workItem.Fields["System.Title"];
                    var wiProjectName = (string)workItem.Fields["System.TeamProject"];
                    

                    results.Add(new Result
                    {
                        Title = wiTitle,
                        SubTitle = wiProjectName,
                        Icon = new Result.IconDelegate(() =>
                        {
                            var icon = _workItemImageService.GetWorkItemImage(workItemsEnum.Current);
                            return icon;
                        }),
                        Action = e =>
                        {
                            var wiLink = workItem.Links.Links["html"] as ReferenceLink;
                            _context.API.OpenUrl(wiLink.Href);
                            return true;
                        }
                    });

                    var resultUpdateArgs = new ResultUpdatedEventArgs();
                    resultUpdateArgs.Results = results;
                    resultUpdateArgs.Query = query;
                    ResultsUpdated.Invoke(this, resultUpdateArgs);
                }
            }
                
            return results;
        }

       
    }
}
