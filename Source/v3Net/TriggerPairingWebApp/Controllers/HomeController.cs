using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using TriggerPairingWebApp.Models;

namespace TriggerPairingWebApp.Controllers
{
    public class HomeController : Controller
    {
        private static TeamViewModel ViewModel;

        // GET: Home
        public ActionResult Index()
        {
            var teamsInfoList = TeamInfo.GetAllTeams();
            var teamsSelectList = teamsInfoList.Select(t => new SelectListItem
            {
                Text = t.Teamname,
                Value = t.Id
            });

            ViewModel = new TeamViewModel
            {
                AllTeams = new SelectList(teamsSelectList, "Value", "Text")
            };

            return View(ViewModel);
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

            return View(ViewModel);
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