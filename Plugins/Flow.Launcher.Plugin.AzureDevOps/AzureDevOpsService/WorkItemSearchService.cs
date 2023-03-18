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
    public class WorkItemSearchService
    {
        
     
        private List<string> _workItemFields = new List<string>() { "System.Title", "System.TeamProject", "System.WorkItemType" };
        private WorkItemTrackingHttpClient _workItemClient;
        private ProjectService _projectService;
        

        public WorkItemSearchService(ConfigService settings, ProjectService projectService)
        {

            _workItemClient = settings.WorkItemClient;
            _projectService = projectService;
            

        }

        public VssConnection VssConnection { get; }
        public AzureDevOpsSettings Settings { get; }


        

        public async IAsyncEnumerable<WorkItem> SearchWorkItems(string search, CancellationToken cancellationToken = default)
        {
            var allProjects = _projectService.ListProjectsAsync(cancellationToken);
            var allProjectsEnum = allProjects.GetAsyncEnumerator(cancellationToken);
            while (await allProjectsEnum.MoveNextAsync())
            {
                var workItems = SearchWorkItems(search, allProjectsEnum.Current.Id, cancellationToken);
                var workItemsEnum = workItems.GetAsyncEnumerator(cancellationToken);
                while (await workItemsEnum.MoveNextAsync())
                {
                    yield return workItemsEnum.Current;
                }
            }
        }

        public async IAsyncEnumerable<WorkItem> SearchWorkItems(string search, IEnumerable<Guid> projectIds, CancellationToken cancellationToken = default)
        {
            foreach (var project in projectIds)
            {
                var pjtWorkItems = SearchWorkItems(search, project, cancellationToken);
                var pjtEnum = pjtWorkItems.GetAsyncEnumerator(cancellationToken);
                while (await pjtEnum.MoveNextAsync())
                {
                    yield return pjtEnum.Current;
                }
            }
        }

        public async IAsyncEnumerable<WorkItem> SearchWorkItems(string search, IEnumerable<string> projects, CancellationToken cancellationToken = default)
        {
            foreach (var project in projects)
            {
                var pjtWorkItems = SearchWorkItems(search, project, cancellationToken);
                var pjtEnum = pjtWorkItems.GetAsyncEnumerator(cancellationToken);
                while (await pjtEnum.MoveNextAsync())
                {
                    yield return pjtEnum.Current;
                }
            }
        }

        public async IAsyncEnumerable<WorkItem> SearchWorkItems(string search, string project, CancellationToken cancellationToken = default)
        {


            var wiql = new Wiql();
            wiql.Query = WiqlService.BuildWiql(search);

            var workItemsQueryResult = await _workItemClient.QueryByWiqlAsync(wiql, project: project, top: 10, cancellationToken: cancellationToken);

            foreach (var wi in workItemsQueryResult.WorkItems)
            {
                var workItem = await _workItemClient.GetWorkItemAsync(project, wi.Id, fields: _workItemFields , cancellationToken: cancellationToken);
                yield return workItem;
            }
        }

        public async IAsyncEnumerable<WorkItem> SearchWorkItems(string search, Guid projectId, CancellationToken cancellationToken = default)
        {
            var wiql = new Wiql();
            wiql.Query = WiqlService.BuildWiql(search);

            var workItemsQueryResult = await _workItemClient.QueryByWiqlAsync(wiql,  project: projectId, top: 10, cancellationToken: cancellationToken);

            foreach (var wi in workItemsQueryResult.WorkItems)
            {
                var workItem = await _workItemClient.GetWorkItemAsync(projectId, wi.Id, fields: _workItemFields, cancellationToken: cancellationToken);
                yield return workItem;
            }
        }

   

        
     

        
    }



}
