﻿using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.Identity;
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
        private AzureDevOpsService? _devOpsService;
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

            var (configOk, message) = await AzureDevOpsService.CheckConfig(_azureDevOpsSettings);
            if (configOk) { 
                _devOpsService = new AzureDevOpsService(_azureDevOpsSettings);
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

            
            if (_devOpsService != null)
            {
                var workItems = _devOpsService.SearchWorkItems(devOpsSearch, cancellationToken);

                var workItemsEnum = workItems.GetAsyncEnumerator(cancellationToken);

                while (await workItemsEnum.MoveNextAsync())
                {
                    var workItem = workItemsEnum.Current;
                    var workItemTypes = await _devOpsService.GetWorkItemTypes((string)workItem.Fields["System.TeamProject"]);
                    var thisWiType = workItemTypes.SingleOrDefault(t => t.Name == workItem.Fields["System.WorkItemType"].ToString());



                    results.Add(new Result
                    {
                        Title = (string)workItem.Fields["System.Title"],
                        SubTitle = (string)workItem.Fields["System.TeamProject"],
                        Icon = new Result.IconDelegate(() =>
                        {
                            var icon = _devOpsService.GetSvgAsImageSource(thisWiType.Icon.Url);
                            return icon;
                        }),
                        Action = e =>
                        {
                            _context.API.OpenUrl(workItem.Links.Links["html"].ToString());
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
