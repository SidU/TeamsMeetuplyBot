namespace MeetupBot.Helpers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Microsoft.Azure;
    using Microsoft.Azure.Documents.Client;

    public static class MeetupBotDataProvider
    {
        private static DocumentClient documentClient;

        public static void InitDatabase()
        {
            if (documentClient == null)
            {
                var endpointUrl = CloudConfigurationManager.GetSetting("CosmosDBEndpointUrl");
                var primaryKey = CloudConfigurationManager.GetSetting("CosmosDBKey");
                

                documentClient = new DocumentClient(new Uri(endpointUrl), primaryKey);
            }
        }


        public static async Task<TeamInstallInfo> SaveTeamInstallStatus(TeamInstallInfo team, bool installed)
        {
            InitDatabase();

            var databaseName = CloudConfigurationManager.GetSetting("CosmosDBDatabaseName");
            var collectionName = CloudConfigurationManager.GetSetting("CosmosCollectionTeams");

            if (installed)
            {
                var response = await documentClient.UpsertDocumentAsync(
                UriFactory.CreateDocumentCollectionUri(databaseName, collectionName),
                team);
            }
            else
            {
                // query first
                
                // Set some common query options
                FeedOptions queryOptions = new FeedOptions { MaxItemCount = -1 };

                var lookupQuery = documentClient.CreateDocumentQuery<TeamInstallInfo>(
                     UriFactory.CreateDocumentCollectionUri(databaseName, collectionName), queryOptions)
                     .Where(t => t.TeamId == team.TeamId);

                var match = lookupQuery.ToList();

                if (match.Count > 0)
                {
                    var response = documentClient.DeleteDocumentAsync(match.First().SelfLink);
                }
                
            }
            
            return team;
        }

        public static List<TeamInstallInfo> GetInstalledTeams()
        {
            InitDatabase();

            var databaseName = CloudConfigurationManager.GetSetting("CosmosDBDatabaseName");
            var collectionName = CloudConfigurationManager.GetSetting("CosmosCollectionTeams");

            // Set some common query options
            FeedOptions queryOptions = new FeedOptions { MaxItemCount = -1 };

            // Find matching activities
            var lookupQuery = documentClient.CreateDocumentQuery<TeamInstallInfo>(
                UriFactory.CreateDocumentCollectionUri(databaseName, collectionName), queryOptions);

            var match = lookupQuery.ToList();

            return match;
        }

        public static UserOptInInfo GetUserOptInStatus(string tenantId, string userId)
        {
            InitDatabase();

            var databaseName = CloudConfigurationManager.GetSetting("CosmosDBDatabaseName");
            var collectionName = CloudConfigurationManager.GetSetting("CosmosCollectionUsers");

            // Set some common query options
            FeedOptions queryOptions = new FeedOptions { MaxItemCount = -1 };

            // Find matching activities
            var lookupQuery = documentClient.CreateDocumentQuery<UserOptInInfo>(
                UriFactory.CreateDocumentCollectionUri(databaseName, collectionName), queryOptions)
                .Where(f => f.TenantId == tenantId && f.UserId == userId);

            var match = lookupQuery.ToList();

            return match.FirstOrDefault();
        }

        public static async Task<Dictionary<string, UserOptInInfo>> GetUserOptInStatusesAsync(string tenantId)
        {
            InitDatabase();

            var databaseName = CloudConfigurationManager.GetSetting("CosmosDBDatabaseName");
            var collectionName = CloudConfigurationManager.GetSetting("CosmosCollectionUsers");

            // Set some common query options
            FeedOptions queryOptions = new FeedOptions { MaxItemCount = -1 };

            // Find matching activities
            var lookupQuery = documentClient.CreateDocumentQuery<UserOptInInfo>(
                UriFactory.CreateDocumentCollectionUri(databaseName, collectionName), queryOptions)
                .Where(f => f.TenantId == tenantId);

            var result = new Dictionary<string, UserOptInInfo>();
            foreach (var status in lookupQuery)
            {
                result.Add(status.UserId, status);
            }
            return result;
        }

        public static async Task<UserOptInInfo> SetUserOptInStatus(string tenantId, string userId, bool optedIn, string serviceUrl)
        {
            InitDatabase();

            var obj = new UserOptInInfo()
            {
                TenantId = tenantId,
                UserId = userId,
                OptedIn = optedIn,
                RecentPairUps = new List<string>()
            };

            obj = await StoreUserOptInStatus(obj);

            return obj;
        }
       
        public static async Task<bool> StorePairup(string tenantId, Dictionary<string, UserOptInInfo> userOptInInfo, string userId1, string userId2)
        {
            InitDatabase();

            var maxPairUpHistory = Convert.ToInt64(CloudConfigurationManager.GetSetting("MaxPairUpHistory"));

            var user1Info = new UserOptInInfo()
            {
                TenantId = tenantId,
                UserId = userId1,
                OptedIn = true,
            };
            if (userOptInInfo.TryGetValue(userId1, out UserOptInInfo initialUser1Info))
            {
                user1Info.RecentPairUps = initialUser1Info.RecentPairUps ?? new List<string>();
                user1Info.Id = initialUser1Info.Id;
            }
            else
            {
                user1Info.RecentPairUps = new List<string>();
            }
            
            var user2Info = new UserOptInInfo()
            {
                TenantId = tenantId,
                UserId = userId2,
                OptedIn = true,
            };
            if (userOptInInfo.TryGetValue(userId2, out UserOptInInfo initialUser2Info))
            {
                user2Info.RecentPairUps = initialUser2Info.RecentPairUps ?? new List<string>();
                user2Info.Id = initialUser2Info.Id;
            }
            else
            {
                user2Info.RecentPairUps = new List<string>();
            }

            user1Info.RecentPairUps.Add(user2Info.UserId);
            if (user1Info.RecentPairUps.Count > maxPairUpHistory) {
                user1Info.RecentPairUps.RemoveAt(0);
            }

            user2Info.RecentPairUps.Add(user1Info.UserId);
            if (user2Info.RecentPairUps.Count > maxPairUpHistory) {
                user2Info.RecentPairUps.RemoveAt(0);
            }

            var isTesting = Boolean.Parse(CloudConfigurationManager.GetSetting("Testing"));
            if (!isTesting)
            {
                await StoreUserOptInStatus(user1Info);
                await StoreUserOptInStatus(user2Info);
            }
            else
            {
                System.Diagnostics.Trace.TraceInformation($"Skip storing pair {userId1} and {userId2} to Cosmos DB in Testing mode");
            }

            return true;
        }

        private static async Task<UserOptInInfo> StoreUserOptInStatus(UserOptInInfo obj)
        {
            InitDatabase();

            var databaseName = CloudConfigurationManager.GetSetting("CosmosDBDatabaseName");
            var collectionName = CloudConfigurationManager.GetSetting("CosmosCollectionUsers");

            var existingDoc = GetUserOptInStatus(obj.TenantId, obj.UserId);
            if (existingDoc != null)
            {
                // Overwrite the existing document
                obj.Id = existingDoc.Id;
            }

            await documentClient.UpsertDocumentAsync(
                UriFactory.CreateDocumentCollectionUri(databaseName, collectionName),
                obj);
            
            return obj;
        }

    }
}