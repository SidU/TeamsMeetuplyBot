using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Hosting;
using System.Web.Http;
using global::MeetupBot.Helpers;

namespace MeetupBot.Controllers
{
    public class ProcessNowController : ApiController
    {
        // GET api/<controller>
        [Route("api/processnow")]
        public List<TeamInstallInfo> Get()
        {
            return MeetupBot.GetAllTeamsInfoAsync().Result;
        }

        // GET api/<controller>/5
        [Route("api/processnow/{teamId}")]
        public Task<TeamInstallInfo> Get([FromUri] string teamId)
        {
            if (string.IsNullOrEmpty(teamId))
            {
                System.Diagnostics.Trace.TraceError($"Received Invalid TeamId. Do not do anything.");
                return null;
            }
            else
            {
                return MeetupBot.GetTeamInfoAsync(teamId);
            }
        }

        // POST api/<controller>/<key>
        [Route("api/processnow/{teamId}")]
        public void Post([FromUri] string teamId)
        {
            if (string.IsNullOrEmpty(teamId))
			{
                System.Diagnostics.Trace.TraceError($"Received Invalid TeamId. Do not do anything.");
            }
            else
			{
                HostingEnvironment.QueueBackgroundWorkItem(ct => MakePairsAsync(teamId));
                return;
            }
        }

        private static async Task<int> MakePairsAsync(string teamId)
        {
            return await MeetupBot.MakePairsAndNotifyAsync(teamId);
        }

    }
}