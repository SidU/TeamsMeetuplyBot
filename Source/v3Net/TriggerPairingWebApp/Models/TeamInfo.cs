using System.Collections.Generic;
using System.IO;
using System.Net;
using Microsoft.Azure.Documents;
using Newtonsoft.Json;

namespace TriggerPairingWebApp.Models
{
    
    public class TeamInfo : Document
    {
        [JsonProperty("teamId")]
        public string TeamId { get; set; }
        
        [JsonProperty("teamName")]
        public string Teamname { get; set; }

        [JsonProperty("tenantId")]
        public string TenantId { get; set; }

        [JsonProperty("serviceUrl")]
        public string ServiceUrl { get; set; }

        public override string ToString()
        {
            return $"Name = {this.Teamname}, Id = {this.Id}";
        }

        public static List<TeamInfo> GetAllTeams()
		{
            // get all 
            WebRequest webRequest = WebRequest.Create($"https://meetupbotappservice.azurewebsites.net/api/processnow");
            webRequest.Method = "GET";
            var response = webRequest.GetResponse();

            using (StreamReader reader = new StreamReader(response.GetResponseStream()))
			{
                string json = reader.ReadToEnd();
                return JsonConvert.DeserializeObject<List<TeamInfo>>(json);
			}
        }
    }
}