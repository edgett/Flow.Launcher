using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;

namespace Flow.Launcher.Plugin.AzureDevOps.AzureDevOpsService
{
    public class WorkItemTypeService
    {
        private WorkItemTrackingHttpClient _workItemClient;

        public WorkItemTypeService(ConfigService settings)
        {
            _workItemClient = settings.WorkItemClient;
        }


        public async Task<List<WorkItemType>> GetWorkItemTypes(string project, CancellationToken cancellationToken = default)
        {
            var wiTypes = await ConfigService.WorkItemTypeCache.GetOrCreateAsync<List<WorkItemType>>($"iconList-{project}", async (e) =>
            {
                var wiTypesc = await _workItemClient.GetWorkItemTypesAsync(project, cancellationToken: cancellationToken);
                return wiTypesc;

            });

            return wiTypes;
        }

        public async Task<List<WorkItemType>> GetWorkItemTypes(Guid project, CancellationToken cancellationToken = default)
        {
            var wiTypes = await ConfigService.WorkItemTypeCache.GetOrCreateAsync<List<WorkItemType>>($"iconList-{project}", async (e) =>
            {
                var wiTypesc = await _workItemClient.GetWorkItemTypesAsync(project, cancellationToken: cancellationToken);
                return wiTypesc;

            });

            return wiTypes;
        }
    }
}
