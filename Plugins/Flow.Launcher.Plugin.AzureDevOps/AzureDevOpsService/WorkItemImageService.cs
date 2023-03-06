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

        public ImageSource GetWorkItemImage(WorkItem workItem)
        {


            var workItemTypes = _workItemTypeService.GetWorkItemTypes((string)workItem.Fields["System.TeamProject"]).GetAwaiter().GetResult();
            var thisWiType = workItemTypes.SingleOrDefault(t => t.Name == workItem.Fields["System.WorkItemType"].ToString());
            var wiIcon = GetImageSourceFromSvgUrlCache(thisWiType.Icon.Url).GetAwaiter().GetResult();
            return wiIcon;

            
          
        }

        public async Task<ImageSource> GetImageSourceFromSvgUrlCache(string svgUrl)
        {

            var imgSrc = ConfigService.WorkItemIconCache.GetOrCreate<ImageSource>(svgUrl, (e) => {
                return GetImageSourceFromSvgUrl(svgUrl);
            });


            return imgSrc;
        }

        public ImageSource GetImageSourceFromSvgUrl(string svgUrl)
        {
            var svgStream = ConfigService.HttpClient.GetStreamAsync(new Uri(svgUrl)).GetAwaiter().GetResult();
            var imgStream = new MemoryStream();
            var svgConverter = new SharpVectors.Converters.StreamSvgConverter(false, false, null);
            svgConverter.Convert(svgStream, imgStream);

            var imageSource = new BitmapImage { CacheOption = BitmapCacheOption.OnLoad };
            imageSource.BeginInit();
            imageSource.StreamSource = imgStream;
            imageSource.EndInit();
            imageSource.Freeze();

            return imageSource;
        }

        private ImageSource makeDefaultIcon()
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
