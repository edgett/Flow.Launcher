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
using Microsoft.Extensions.Caching.Memory;
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
    public class AzureDevOpsService
    {
        private readonly ProjectHttpClient _projectClient;
        private readonly WorkItemTrackingHttpClient _workItemClient;
        private readonly HttpClient _httpClient = new HttpClient();
        private static MemoryCache _memoryCache { get;} = new MemoryCache(new MemoryCacheOptions());
        private static MemoryCache _iconCache { get; } = new MemoryCache(new MemoryCacheOptions());

        public AzureDevOpsService(AzureDevOpsSettings settings)
        {
            this.Settings = settings;
            var creds = new VssBasicCredential(string.Empty, settings.DevOpsPat);
            var devOpsUri = new Uri(settings.DevOpsUrl);
            VssConnection = new VssConnection(devOpsUri, creds);
            _projectClient = VssConnection.GetClient<ProjectHttpClient>();
            _workItemClient = VssConnection.GetClient<WorkItemTrackingHttpClient>();

        }

        public VssConnection VssConnection { get; }
        public AzureDevOpsSettings Settings { get; }


        public async IAsyncEnumerable<TeamProjectReference> ListProjectsAsync(CancellationToken cancellationToken = default)
        {
            var projects = await _projectClient.GetProjects();

            foreach (var p in projects)
            {
                yield return p;
            }

            while (!string.IsNullOrWhiteSpace(projects.ContinuationToken))
            {
                projects = await _projectClient.GetProjects(continuationToken: projects.ContinuationToken);

                foreach (var p in projects)
                {
                    yield return p;
                }
            }
        }

        public async IAsyncEnumerable<WorkItem> SearchWorkItems(string search, CancellationToken cancellationToken = default)
        {
            var allProjects = ListProjectsAsync(cancellationToken);
            var allProjectsEnum = allProjects.GetAsyncEnumerator();
            while (await allProjectsEnum.MoveNextAsync())
            {
                var workItems = SearchWorkItems(search, allProjectsEnum.Current.Name, cancellationToken);
                var workItemsEnum = workItems.GetAsyncEnumerator();
                while (await workItemsEnum.MoveNextAsync())
                {
                    yield return workItemsEnum.Current;
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
            wiql.Query = buildWiql(search, project);

            var workItemsQueryResult = await _workItemClient.QueryByWiqlAsync(wiql, top: 10, cancellationToken: cancellationToken);

            foreach (var wi in workItemsQueryResult.WorkItems)
            {
                var workItem = await _workItemClient.GetWorkItemAsync(project, wi.Id, fields: new List<string>() { "System.Title", "System.TeamProject", "System.WorkItemType" }, cancellationToken: cancellationToken);
                yield return workItem;
            }


        }

        private string buildWiql(string search, string project)
        {
            search = escapeWiQl(search);

            var wiqlString = $@"Select [Id]
                    From WorkItems 
                    Where 
                    [System.TeamProject] = '{project}'
                    and
                    (
                        [Title] CONTAINS '{search}'
                        or
                        [Description] Contains Words '{string.Join("*", search.Split(" "))}'
                    )
                    Order By [Changed Date] Desc";


            return wiqlString;
        }

        private string escapeWiQl(string input)
        {
            var output = input.Replace("'", "''");
            output = output.Replace("\\", "\\\\");
            return output;
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
        public async Task<List<WorkItemType>> GetWorkItemTypes(string project, CancellationToken cancellationToken = default)
        {
            var wiTypes = await _memoryCache.GetOrCreateAsync<List<WorkItemType>>("iconList", async (e) =>
            {
                var wiTypes = await _workItemClient.GetWorkItemTypesAsync(project, cancellationToken: cancellationToken);
                return wiTypes;

            });

            return wiTypes;
        }

        public ImageSource GetWorkItemImage(WorkItem workItem)
        {
            var workItemTypes = this.GetWorkItemTypes((string)workItem.Fields["System.TeamProject"]).GetAwaiter().GetResult();
            var thisWiType = workItemTypes.SingleOrDefault(t => t.Name == workItem.Fields["System.WorkItemType"].ToString());
            var wiIcon = GetImageSourceFromSvgUrlCache(thisWiType.Icon.Url).GetAwaiter().GetResult();
            return wiIcon;
        }

        public async Task<ImageSource> GetImageSourceFromSvgUrlCache(string svgUrl)
        {
      
                var imgSrc =  _iconCache.GetOrCreate<ImageSource>(svgUrl, (e)=>{
                    return GetImageSourceFromSvgUrl(svgUrl);
                });


                return imgSrc;
          
        }

        public ImageSource GetImageSourceFromSvgUrl(string svgUrl)
        {
            var svgStream = _httpClient.GetStreamAsync(new Uri(svgUrl)).GetAwaiter().GetResult();
            var imgStream = new MemoryStream();
            var svgConverter = new SharpVectors.Converters.StreamSvgConverter(false, false, null);
            svgConverter.Convert(svgStream, imgStream);

            var imageSource = new BitmapImage { CacheOption = BitmapCacheOption.OnLoad };
            imageSource.BeginInit();
            imageSource.StreamSource = imgStream;
            imageSource.EndInit();

            return imageSource;
        }
    }



}
