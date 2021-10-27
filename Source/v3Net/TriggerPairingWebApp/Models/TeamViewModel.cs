using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Newtonsoft.Json;

namespace TriggerPairingWebApp.Models
{
	public class TeamViewModel
	{
		public string SelectedTeamId { get; set; }
		public SelectList AllTeams { get; set; }
		public IEnumerable<TeamInfo> AllTeamsInfo { get; set; }
	}

	public class TeamInfo
	{
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("teamName")]
        public string Teamname { get; set; }

        [JsonProperty("pairingStatus")]
        public string PairingStatus { get; set; }

        [JsonProperty("lastPairedAtUTC")]
        public string LastPairedAtUTC { get; set; }

        [JsonProperty("memberCount")]
        public string MemberCount { get; set; } = "TODO";
    }
}