using Microsoft.VisualStudio.Services.Common;

namespace Flow.Launcher.Plugin.AzureDevOps
{
    public class AzureDevOpsSettings
    {
        public string DevOpsUrl { get; set; } = "";
        public string DevOpsPat { get; set; } = "";

        public override bool Equals(object obj)
        {
            if (obj is AzureDevOpsSettings settings)
            {
                return DevOpsUrl == settings.DevOpsUrl && DevOpsPat == settings.DevOpsPat;
            }
            return false;
        }

        public void Update(AzureDevOpsSettings newSettings)
        {
            newSettings.GetType().GetProperties().ForEach(p =>
            {
                p.SetValue(this, p.GetValue(newSettings));
            });
        }
    }
}
