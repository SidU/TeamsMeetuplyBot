﻿namespace MeetupBot.Helpers
{
    using Microsoft.Azure.Documents;
    using Newtonsoft.Json;
    using System.Collections.Generic;

    public class UserOptInInfo : Document
    {
        [JsonProperty("tenantId")]
        public string TenantId { get; set; }

        [JsonProperty("userId")]
        public string UserId { get; set; }

        [JsonProperty("optedIn")]
        public bool OptedIn { get; set; }

        [JsonProperty("recentPairups")]
        public List<string> RecentPairUps { get; set; }
    }
}