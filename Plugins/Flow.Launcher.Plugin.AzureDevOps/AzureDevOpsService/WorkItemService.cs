
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;

namespace Flow.Launcher.Plugin.AzureDevOps.AzureDevOpsService
{
    public class WorkItemService
    {
        private List<string> _workItemFields = new List<string>() { "System.Title", "System.TeamProject", "System.TeamProjectId", "System.WorkItemType" };
        private WorkItemTrackingHttpClient _workItemClient;
        private ProjectService _projectService;


        public WorkItemService(ConfigService settings)
        {
            _workItemClient = settings.WorkItemClient;
        }

        public async Task<WorkItem> GetWorkItemByIdAsync(string project, int id, CancellationToken cancellationToken = default)
        {
            var workItem = await _workItemClient.GetWorkItemAsync(project: project, id: id, fields: _workItemFields, cancellationToken: cancellationToken);
            return workItem;
        }

        public async Task<WorkItem> GetWorkItemByIdAsync(Guid projectId, int id, CancellationToken cancellationToken = default)
        {
            var workItem = await _workItemClient.GetWorkItemAsync(project: projectId, id: id, fields: _workItemFields, cancellationToken: cancellationToken);
            return workItem;
        }
    }
}
