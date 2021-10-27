using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using Newtonsoft.Json;
using TriggerPairingWebApp.Models;

namespace TriggerPairingWebApp.Controllers
{
	public class HomeController : Controller
	{
		// GET: Home
		public ActionResult Index()
		{
			var teamsInfoList = TeamsDataProvider.GetAllTeams();
			var teamsSelectList = teamsInfoList.Select(t => new SelectListItem
			{
				Text = t.Teamname,
				Value = t.Id
			});

			var viewModel = new TeamViewModel
			{
				AllTeams = new SelectList(teamsSelectList, "Value", "Text"),
				AllTeamsInfo = teamsInfoList.Select(t => new TeamInfo
				{
					Teamname = t.Teamname,
					PairingStatus = t.PairingStatus,
					LastPairedAtUTC = t.LastPairedAtUTC
				})
			};

			return View(viewModel);
		}

		[HttpPost]
		public ActionResult Index(TeamViewModel model)
		{
			// Send Web Request to trigger pairing
			var selectedTeamId = model.SelectedTeamId;
			WebRequest webRequest = WebRequest.Create($"https://meetupbotappservice.azurewebsites.net/api/processnow/{selectedTeamId}");
			webRequest.Method = "POST";
			webRequest.ContentLength = 0;
			webRequest.GetResponse();

			// go back to home page
			return Redirect("~/");
		}

		public ActionResult About()
		{
			ViewBag.Message = "Your application description page.";

			return View();
		}

		public ActionResult Contact()
		{
			ViewBag.Message = "Your contact page.";

			return View();
		}
	}
}