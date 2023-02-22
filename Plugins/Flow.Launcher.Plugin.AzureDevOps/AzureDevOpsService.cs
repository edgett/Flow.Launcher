using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Documents;
using Microsoft.TeamFoundation.Core.WebApi;
using Microsoft.TeamFoundation.Framework.Common;
using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.WebApi;

namespace Flow.Launcher.Plugin.AzureDevOps
{
    public class AzureDevOpsService
    {
        public AzureDevOpsService(AzureDevOpsSettings settings)
        {
            this.Settings = settings;
            var creds = new VssBasicCredential(string.Empty, settings.DevOpsPat);
            var devOpsUri = new Uri(settings.DevOpsUrl);
            VssConnection = new VssConnection(devOpsUri, creds);

        }

        public VssConnection VssConnection { get; }
        public AzureDevOpsSettings Settings { get; }


        public async IAsyncEnumerable<TeamProjectReference> ListProjectsAsync()
        {
            var projectClient = await VssConnection.GetClientAsync<ProjectHttpClient>();
            var projects = await projectClient.GetProjects();

            foreach (var p in projects)
            {
                yield return p;   
            }

            while (!string.IsNullOrWhiteSpace(projects.ContinuationToken))
            {
                projects = await projectClient.GetProjects(continuationToken: projects.ContinuationToken);

                foreach (var p in projects)
                {
                    yield return p;
                }
            }
        }

        public static async Task<(bool, string)> CheckConfig(AzureDevOpsSettings settings)
        {

            Uri? devOpsUri = null;
            if (!Uri.TryCreate(settings.DevOpsUrl, new UriCreationOptions(), out devOpsUri) || devOpsUri == null)
            {
                return new (false, "Invalid Azure DevOps Url.");
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
