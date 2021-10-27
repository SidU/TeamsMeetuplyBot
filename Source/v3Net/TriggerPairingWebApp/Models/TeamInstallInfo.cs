namespace TriggerPairingWebApp.Models
{
    using System;
	using System.Collections.Generic;
	using System.IO;
	using System.Net;
	using Microsoft.Azure.Documents;
    using Newtonsoft.Json;

    public class TeamInstallInfo : Document
    {
        [JsonProperty("teamId")]
        public string TeamId { get; set; }

        [JsonProperty("teamName")]
        public string Teamname { get; set; }

        [JsonProperty("tenantId")]
        public string TenantId { get; set; }

        [JsonProperty("serviceUrl")]
        public string ServiceUrl { get; set; }

        [JsonProperty("pairingStatus")]
        public PairingStatus PairingStatus { get; set; }

        [JsonProperty("lastPairedOn")]
        public DateTime LastPairedOn { get; set; }

        public override string ToString()
        {
            return $"Name = {this.Teamname}, TeamId = {this.TeamId}, Id = {this.Id}, LastPairedOn = {this.LastPairedOn}";
        }
    }

    public enum PairingStatus
    {
        Pairing,
        Paired
    }
}