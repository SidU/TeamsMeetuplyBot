using System.Collections.Generic;
using System.Threading.Tasks;
using System.Web.Hosting;
using System.Web.Http;
using global::MeetupBot.Helpers;
using Microsoft.Azure;

namespace MeetupBot.Controllers
{
    public class ProcessNowController : ApiController
    {
        // GET api/<controller>
        [Route("api/processnow")]
        public List<TeamInstallInfo> Get()
        {
            return MeetupBot.GetTeamsInfoAsync().Result;
        }

        // GET api/<controller>/5
        [Route("api/processnow/{key}")]
        public string Get([FromUri] string key)
        {
            return $"Not Yet Implemented. key: [{key}]";
        }

        // POST api/<controller>/<key>
        [Route("api/processnow/{teamId}")]
        public void Post([FromUri] string teamId)
        {
            System.Diagnostics.Trace.TraceInformation($"In Post");
            if (string.IsNullOrEmpty(teamId))
			{
                System.Diagnostics.Trace.TraceError($"Received Invalid TeamId. Do not do anything.");
            }
            else
			{
                HostingEnvironment.QueueBackgroundWorkItem(ct => MakePairs(teamId));
                return;
            }
        }

        private static async Task<int> MakePairs(string teamId)
        {
            System.Diagnostics.Trace.TraceInformation($"Trigger Pairing and send notifications");
            return await MeetupBot.MakePairsAndNotify(teamId);
        }

    }
}