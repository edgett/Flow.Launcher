using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Flow.Launcher.Plugin.AzureDevOps.AzureDevOpsService;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.TeamFoundation.Build.WebApi;
using Microsoft.TeamFoundation.Core.WebApi;
using Microsoft.TeamFoundation.Framework.Common;
using Microsoft.TeamFoundation.Work.WebApi;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;
using Microsoft.VisualBasic;
using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.WebApi;

namespace Flow.Launcher.Plugin.AzureDevOps
{
    public class ProjectService
    {
        private ProjectHttpClient _projectClient;

        public ProjectService(ConfigService settings)
        {
            _projectClient = settings.ProjectClient;
        }

        public async IAsyncEnumerable<TeamProjectReference> ListProjectsAsync(CancellationToken cancellationToken = default)
        {
            var projects = await _projectClient.GetProjects();

            foreach (var p in projects)
            {
                yield return p;
            }

            while (!string.IsNullOrWhiteSpace(projects.ContinuationToken) && !cancellationToken.IsCancellationRequested)
            {
                projects = await _projectClient.GetProjects(continuationToken: projects.ContinuationToken);

                foreach (var p in projects)
                {
                    yield return p;
                }
            }
        }
    }



}
