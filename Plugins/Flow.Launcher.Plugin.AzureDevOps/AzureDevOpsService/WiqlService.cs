using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flow.Launcher.Plugin.AzureDevOps.AzureDevOpsService
{
    public class WiqlService
    {

        /// <summary>
        /// Get the WIQL to search work items.
        /// </summary>
        /// <param name="search">search query, this will be escaped for WIQL</param>
        /// <param name="project">Project name string, or project id Guid.</param>
        /// <returns>A WIQL string</returns>
        public static string BuildWiql(string search)
        {
            search = EscapeWiQl(search);
            var field = "[System.TeamProject]";
            var teamProjectWhere = $"AND {field} = @Project";
            
            var wiqlString = $@"Select [Id]
                    From WorkItems 
                    Where 
                    (
                        [Title] CONTAINS '{search}'
                        or
                        [Description] Contains Words '{string.Join("*", search.Split(" "))}'
                    )
                    {teamProjectWhere}
                    Order By [Changed Date] Desc";


            return wiqlString;
        }

        public static string EscapeWiQl(string input)
        {
            var output = input.Replace("'", "''");
            output = output.Replace("\\", "\\\\");
            return output;
        }
    }
}
