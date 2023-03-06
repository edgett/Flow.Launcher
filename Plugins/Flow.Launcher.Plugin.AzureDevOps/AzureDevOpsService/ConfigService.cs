using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.TeamFoundation.Core.WebApi;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi;
using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.WebApi;

namespace Flow.Launcher.Plugin.AzureDevOps.AzureDevOpsService
{
    public class ConfigService
    {

        public AzureDevOpsSettings Settings { get; }
        public VssConnection DevOpsConnection { get; }
        public ProjectHttpClient ProjectClient { get; }
        public WorkItemTrackingHttpClient WorkItemClient { get; }
        public static HttpClient HttpClient { get; } = new HttpClient();
        public static MemoryCache WorkItemTypeCache { get; } = new MemoryCache(new MemoryCacheOptions());
        public static MemoryCache WorkItemIconCache { get; } = new MemoryCache(new MemoryCacheOptions());
        
        public ConfigService(AzureDevOpsSettings settings)
        {
            Settings = settings;
            var creds = new VssBasicCredential(string.Empty, settings.DevOpsPat);
            var devOpsUri = new Uri(settings.DevOpsUrl);
            DevOpsConnection = new VssConnection(devOpsUri, creds);

            ProjectClient = DevOpsConnection.GetClient<ProjectHttpClient>();
            WorkItemClient = DevOpsConnection.GetClient<WorkItemTrackingHttpClient>();
        }
      


        public static async Task<(bool, string)> CheckConfig(AzureDevOpsSettings settings)
        {

            Uri? devOpsUri = null;
            if (!Uri.TryCreate(settings.DevOpsUrl, new UriCreationOptions(), out devOpsUri) || devOpsUri == null)
            {
                return new(false, "Invalid Azure DevOps Url.");
            }

            if (string.IsNullOrWhiteSpace(settings.DevOpsPat))
            {
                return new(false, "Invalid Azure DevOps Personal Access Token.");
            }

            try
            {
                var creds = new VssBasicCredential(string.Empty, settings.DevOpsPat);
                VssConnection vssConnection = new VssConnection(devOpsUri, creds);
                await vssConnection.ConnectAsync();
                return new(true, "Successfully connected to Azure DevOps."); ;
            }
            catch (Exception ex)
            {
                return new(false, ex.ToString()); ;
            }
        }
    }
}
