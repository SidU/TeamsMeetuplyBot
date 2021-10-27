namespace MeetupBot.Helpers
{
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
        public string PairingStatus { get; set; }

        [JsonProperty("lastPairedAtUTC")]
        public string LastPairedAtUTC { get; set; }

		public override string ToString()
		{
			return $"Name = {this.Teamname}, TeamId = {this.TeamId}, Id = {this.Id}, PairingStatus = {this.PairingStatus}, LastPairedOn = {this.LastPairedAtUTC}";
		}
	}

	public enum PairingStatus
	{
		New,
        Pairing,
		Paired
	}
}