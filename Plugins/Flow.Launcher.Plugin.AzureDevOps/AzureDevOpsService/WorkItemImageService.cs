using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using System.Windows.Media;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;
using System.Reflection;
using System.Windows.Threading;
using System.Threading;

namespace Flow.Launcher.Plugin.AzureDevOps.AzureDevOpsService
{
    public class WorkItemImageService
    {
        private readonly WorkItemTypeService _workItemTypeService;
        private readonly ImageSource _defaultIcon;

        public WorkItemImageService(WorkItemTypeService workItemTypeService)
        {
            _workItemTypeService = workItemTypeService;
            //_defaultIcon = makeDefaultIcon();
        }

        public async Task<ImageSource> GetWorkItemImageAsync(WorkItem workItem, CancellationToken cancellationToken)
        {

            try
            {

                var workItemTypes = await _workItemTypeService.GetWorkItemTypes((string)workItem.Fields["System.TeamProject"], cancellationToken);
                var thisWiType = workItemTypes.SingleOrDefault(t => t.Name == workItem.Fields["System.WorkItemType"].ToString());
                var wiIcon =  await GetImageSourceFromSvgUrlCache(thisWiType.Icon.Url, cancellationToken);
                return wiIcon;
            }
            catch (Exception ex)
            {

                throw ex;
            }

       

            //return makeDefaultIcon();
          
        }

        public async Task<ImageSource> GetImageSourceFromSvgUrlCache(string svgUrl, CancellationToken cancellationToken)
        {

            var imageMs = await ConfigService.WorkItemIconCache.GetOrCreateAsync<MemoryStream>(svgUrl, async (e) => {
                var ms = await GetImageMemoryStreamFromSvgUrl(svgUrl, cancellationToken);
                return ms;
            });
            imageMs.Position = 0;

            var imageSource = new BitmapImage { };
            imageSource.BeginInit();
            imageSource.StreamSource = imageMs;
            imageSource.EndInit();
            imageSource.Freeze();
            
            
            return imageSource;
        }

        public async Task<MemoryStream> GetImageMemoryStreamFromSvgUrl(string svgUrl, CancellationToken cancellationToken)
        {
            var svgStream = await ConfigService.HttpClient.GetStreamAsync(new Uri(svgUrl), cancellationToken);
            var imgStream = new MemoryStream();
            var svgConverter = new SharpVectors.Converters.StreamSvgConverter(false, false, null);
            svgConverter.Convert(svgStream, imgStream);

            return imgStream;
        }

        public ImageSource MakeDefaultIcon()
        {
            var thisDllPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            var defaultIconPath = Path.Combine(thisDllPath, "Images", "AzureDevOps.png");
            var defaultIcon = new BitmapImage();
            defaultIcon.BeginInit();
            defaultIcon.UriSource = new Uri(defaultIconPath);
            defaultIcon.EndInit();
            defaultIcon.Freeze();
            var iss = (ImageSource)defaultIcon;
            return iss;
        }

    }
}
