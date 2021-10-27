using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Web;
using Newtonsoft.Json;

namespace TriggerPairingWebApp.Models
{
	public class TeamsDataProvider
	{
        public static List<TeamInstallInfo> GetAllTeams()
        {
            // get all 
            WebRequest webRequest = WebRequest.Create($"https://meetupbotappservice.azurewebsites.net/api/processnow");
            webRequest.Method = "GET";
            var response = webRequest.GetResponse();

            using (StreamReader reader = new StreamReader(response.GetResponseStream()))
            {
                string json = reader.ReadToEnd();
                return JsonConvert.DeserializeObject<List<TeamInstallInfo>>(json);
            }
        }
    }
}