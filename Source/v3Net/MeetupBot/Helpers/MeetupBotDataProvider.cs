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

        public static async Task<UserOptInInfo> SetUserOptInStatus(string tenantId, string userId, bool optedIn, string serviceUrl)
        {
            InitDatabase();

            var obj = new UserOptInInfo()
            {
                TenantId = tenantId,
                UserId = userId,
                OptedIn = optedIn,
                ServiceUrl = serviceUrl
            };

            obj = await StoreUserOptInStatus(obj);

            return obj;
        }
       
        public static async Task<bool> StorePairup(string tenantId, string user1Id, string user2Id)
        {
            InitDatabase();

            var maxPairUpHistory = Convert.ToInt64(CloudConfigurationManager.GetSetting("MaxPairUpHistory"));

            var user1Info = GetUserOptInStatus(tenantId, user1Id);
            var user2Info = GetUserOptInStatus(tenantId, user2Id);

            user1Info.RecentPairUps.Add(user2Info);
            if (user1Info.RecentPairUps.Count >= maxPairUpHistory) {
                user1Info.RecentPairUps.RemoveAt(0);
            }

            await StoreUserOptInStatus(user1Info);

            user2Info.RecentPairUps.Add(user1Info);
            if (user2Info.RecentPairUps.Count >= maxPairUpHistory) {
                user2Info.RecentPairUps.RemoveAt(0);
            }

            await StoreUserOptInStatus(user2Info);

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
                // update
                var response = await documentClient.DeleteDocumentAsync(existingDoc.SelfLink);
            }
            else
            {
                // Insert
                var response = await documentClient.UpsertDocumentAsync(
                UriFactory.CreateDocumentCollectionUri(databaseName, collectionName),
                obj);

            }
            
            return obj;
        }

    }
}