using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace TriggerPairingWebApp.Models
{
	public class TeamViewModel
	{
		public string SelectedTeamId { get; set; }
		public SelectList AllTeams { get; set; }
	}
}