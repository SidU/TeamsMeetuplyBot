using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LetsMeetPairingFunctionApp
{
    internal class TeamInfo
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("teamName")]
        public string Teamname { get; set; }

        public override string ToString()
        {
            return $"Team Name: {Teamname}, Team Id: {Id}";
        }
    }
}
