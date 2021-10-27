namespace MeetupBot.Helpers
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Threading.Tasks;
	using Microsoft.Azure;
	using Microsoft.Azure.Documents;
	using Microsoft.Azure.Documents.Client;
	
	public static class MeetupBotDataProvider
	{
		private static DocumentClient documentClient;

		public static async Task InitDatabaseAsync()
		{
			if (documentClient == null)
			{
				var endpointUrl = CloudConfigurationManager.GetSetting("CosmosDBEndpointUrl");
				var secretName = CloudConfigurationManager.GetSetting("CosmosDBKeySecretName");
				var primaryKey = await SecretsHelper.GetSecretAsync(secretName).ConfigureAwait(false);

				documentClient = new DocumentClient(new Uri(endpointUrl), primaryKey);
			}
		}

		public static async Task<TeamInstallInfo> SaveTeamStatusAsync(TeamInstallInfo team, TeamUpdateType status)
		{
			System.Diagnostics.Trace.TraceInformation($"Update info for team: [{team}] in DB");
			await InitDatabaseAsync().ConfigureAwait(false);

			var databaseName = CloudConfigurationManager.GetSetting("CosmosDBDatabaseName");
			var collectionName = CloudConfigurationManager.GetSetting("CosmosCollectionTeams");

			switch (status)
			{
				case TeamUpdateType.Add:
					{
						var response = await documentClient.UpsertDocumentAsync(
								UriFactory.CreateDocumentCollectionUri(databaseName, collectionName),
								team);
						System.Diagnostics.Trace.TraceInformation($"Updated info: [{response.Resource}]");
						break;
					}

				case TeamUpdateType.Remove:
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
							System.Diagnostics.Trace.TraceInformation($"Found the team to be deleted. Deleting from DB now.");
							var options = new RequestOptions { PartitionKey = new PartitionKey(team.TeamId) };
							var response = await documentClient.DeleteDocumentAsync(match.First().SelfLink, options).ConfigureAwait(false);
							System.Diagnostics.Trace.TraceInformation($"Deleted team [{team.Teamname}] from DB");
						}
						else
						{
							System.Diagnostics.Trace.TraceInformation($"Did not find team to delete. Skip");
						}

						break;
					}

				case TeamUpdateType.PairingInfo:
					{
						var response = await documentClient.UpsertDocumentAsync(
						   UriFactory.CreateDocumentCollectionUri(databaseName, collectionName),
						   team);

						System.Diagnostics.Trace.TraceInformation($"Updated team info: [{response.Resource}].");

						break;
					}

				default:
					break;
			}

			return team;
		}

		public static async Task<TeamInstallInfo> GetTeamInfoAsync(string teamId)
		{
			System.Diagnostics.Trace.TraceInformation($"Retrieving info for Team: [{teamId}] from DB.");
			await InitDatabaseAsync().ConfigureAwait(false);

			var databaseName = CloudConfigurationManager.GetSetting("CosmosDBDatabaseName");
			var collectionName = CloudConfigurationManager.GetSetting("CosmosCollectionTeams");

			// Set some common query options
			FeedOptions queryOptions = new FeedOptions { MaxItemCount = -1 };

			// Find matching activities
			var lookupQuery = documentClient.CreateDocumentQuery<TeamInstallInfo>(
				UriFactory.CreateDocumentCollectionUri(databaseName, collectionName), queryOptions)
				 .Where(t => t.TeamId == teamId);

			var match = lookupQuery.ToList();
			System.Diagnostics.Trace.TraceInformation($"Found [{match.Count}] matches");
			return match.FirstOrDefault();
		}

		public static async Task<List<TeamInstallInfo>> GetInstalledTeamsAsync()
		{
			await InitDatabaseAsync().ConfigureAwait(false);

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

		public static async Task<UserOptInInfo> GetUserOptInStatusAsync(string tenantId, string userId)
		{
			await InitDatabaseAsync().ConfigureAwait(false);

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
			await InitDatabaseAsync().ConfigureAwait(false);

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

		public static async Task<UserOptInInfo> SetUserOptInStatus(string tenantId, string userId, string userName, bool optedIn)
		{
			await InitDatabaseAsync().ConfigureAwait(false);

			var userInfo = new UserOptInInfo()
			{
				TenantId = tenantId,
				UserId = userId,
				OptedIn = optedIn,
				RecentPairUps = new List<string>(),
				UserFullName = userName
			};

			var isTesting = Boolean.Parse(CloudConfigurationManager.GetSetting("Testing"));
			if (isTesting)
			{
				System.Diagnostics.Trace.TraceInformation($"Skip updating DB in testing mode");
			}
			else
			{
				userInfo = await StoreUserOptInStatus(userInfo);
			}

			return userInfo;
		}

		public static async Task<bool> StorePairup(string tenantId, Dictionary<string, UserOptInInfo> userOptInInfo, string userId1, string userId2, string userFullName1, string userFullName2)
		{
			System.Diagnostics.Trace.TraceInformation($"Storing the pair: [{userFullName1}] and [{userFullName2}]");
			await InitDatabaseAsync().ConfigureAwait(false);

			var maxPairUpHistory = Convert.ToInt64(CloudConfigurationManager.GetSetting("MaxPairUpHistory"));

			var user1Info = new UserOptInInfo()
			{
				TenantId = tenantId,
				UserId = userId1,
				UserFullName = userFullName1,
				OptedIn = true,
			};

			if (userOptInInfo.TryGetValue(userId1, out UserOptInInfo initialUser1Info))
			{
				// User already exists.
				// Get their recent pairs and add the new one to the list.
				System.Diagnostics.Trace.TraceInformation($"User [{userFullName1}] already exists in the system. Get previous pairs.");

				user1Info.RecentPairUps = initialUser1Info.RecentPairUps ?? new List<string>();
				user1Info.Id = initialUser1Info.Id;
			}
			else
			{
				System.Diagnostics.Trace.TraceInformation($"User [{userFullName1}] not found in the system. Create a new Pair list");
				user1Info.RecentPairUps = new List<string>();
			}

			var user2Info = new UserOptInInfo()
			{
				TenantId = tenantId,
				UserId = userId2,
				UserFullName = userFullName2,
				OptedIn = true,
			};

			if (userOptInInfo.TryGetValue(userId2, out UserOptInInfo initialUser2Info))
			{
				// User already exists.
				// Get their recent pairs and add the new one to the list.
				System.Diagnostics.Trace.TraceInformation($"User [{userFullName2}] already exists in the system. Get previous pairs.");

				user2Info.RecentPairUps = initialUser2Info.RecentPairUps ?? new List<string>();
				user2Info.Id = initialUser2Info.Id;
			}
			else
			{
				System.Diagnostics.Trace.TraceInformation($"User [{userFullName2}] not found in the system. Create a new Pair list");
				user2Info.RecentPairUps = new List<string>();
			}

			user1Info.RecentPairUps.Add(user2Info.UserId);
			if (user1Info.RecentPairUps.Count > maxPairUpHistory)
			{
				user1Info.RecentPairUps.RemoveAt(0);
			}

			user2Info.RecentPairUps.Add(user1Info.UserId);
			if (user2Info.RecentPairUps.Count > maxPairUpHistory)
			{
				user2Info.RecentPairUps.RemoveAt(0);
			}

			var isTesting = Boolean.Parse(CloudConfigurationManager.GetSetting("Testing"));
			if (isTesting)
			{
				System.Diagnostics.Trace.TraceInformation($"Skip storing pair [{userId1}] and [{userId2}] to DB in Testing mode");
			}
			else
			{
				await StoreUserOptInStatus(user1Info).ConfigureAwait(false);
				await StoreUserOptInStatus(user2Info).ConfigureAwait(false);
				System.Diagnostics.Trace.TraceInformation($"Updating the pair : [{userFullName1}] and [{userFullName2}] in DB");
			}

			return true;
		}

		private static async Task<UserOptInInfo> StoreUserOptInStatus(UserOptInInfo userInfo)
		{
			await InitDatabaseAsync().ConfigureAwait(false);

			System.Diagnostics.Trace.TraceInformation($"Set Optin info for user: [{userInfo.UserFullName}] to {userInfo.OptedIn} in DB");

			var databaseName = CloudConfigurationManager.GetSetting("CosmosDBDatabaseName");
			var collectionName = CloudConfigurationManager.GetSetting("CosmosCollectionUsers");

			var existingDoc = await GetUserOptInStatusAsync(userInfo.TenantId, userInfo.UserId).ConfigureAwait(false);
			if (existingDoc != null)
			{
				// Overwrite the existing document
				userInfo.Id = existingDoc.Id;
			}

			_ = await documentClient.UpsertDocumentAsync(
				UriFactory.CreateDocumentCollectionUri(databaseName, collectionName),
				userInfo);

			System.Diagnostics.Trace.TraceInformation($"Updated User: [{userInfo.UserFullName}] in DB");
			return userInfo;
		}
	}
}