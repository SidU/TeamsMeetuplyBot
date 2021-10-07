namespace MeetupBot
{
    using Helpers;
    using Helpers.AdaptiveCards;
    using Microsoft.Azure;
    using Microsoft.Bot.Connector;
    using Microsoft.Bot.Connector.Teams;
    using Microsoft.Bot.Connector.Teams.Models;
    using Newtonsoft.Json;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    public static class MeetupBot
    {
        public static async Task<int> MakePairsAndNotify()
        {
            // Recall all the teams where we have been added
            // For each team where I have been added:
            //     Pull the roster of each team where I have been added
            //     Remove the members who have opted out of pairs
            //     Match each member with someone else
            //     Save this pair
            // Now notify each pair found in 1:1 and ask them to reach out to the other person
            // When contacting the user in 1:1, give them the button to opt-out.

            var teams = MeetupBotDataProvider.GetInstalledTeams();

            var countPairsNotified = 0;
            var maxPairUpsPerTeam = Convert.ToInt32(CloudConfigurationManager.GetSetting("MaxPairUpsPerTeam"));

            foreach (var team in teams)
            {
                try
                {
                    var optInStatuses = await MeetupBotDataProvider.GetUserOptInStatusesAsync(team.TenantId);
                    var optedInUsers = await GetOptedInUsers(team, optInStatuses);

                    var teamName = await GetTeamNameAsync(team.ServiceUrl, team.TeamId);

                    foreach (var pair in MakePairs(optedInUsers, optInStatuses).Take(maxPairUpsPerTeam))
                    {
                        await NotifyPair(team.ServiceUrl, team.TenantId, teamName, pair);
                        await MeetupBotDataProvider.StorePairup(team.TenantId, optInStatuses, pair.Item1.ObjectId, pair.Item2.ObjectId);

                        countPairsNotified++;
                    }
                }
                catch (UnauthorizedAccessException uae)
                {
                    System.Diagnostics.Trace.TraceError($"Failed to process a team: {team.ToString()} due to error {uae.ToString()}");
                }
            }

            System.Diagnostics.Trace.TraceInformation($"{countPairsNotified} pairs notified");

            return countPairsNotified;
        }

        private static async Task<string> GetTeamNameAsync(string serviceUrl, string teamId)
        {
            using (var client = new ConnectorClient(new Uri(serviceUrl)))
            {
                var teamsConnectorClient = client.GetTeamsConnectorClient();
                var teamDetailsResult = await teamsConnectorClient.Teams.FetchTeamDetailsAsync(teamId);
                return teamDetailsResult.Name;
            }
        }

        private static async Task NotifyPair(string serviceUrl, string tenantId, string teamName, Tuple<TeamsChannelAccount, TeamsChannelAccount> pair)
        {
            var teamsPerson1 = pair.Item1.AsTeamsChannelAccount();
            var teamsPerson2 = pair.Item2.AsTeamsChannelAccount();

            // Fill in person1's info in the card for person2
            var cardForPerson2 = PairUpNotificationAdaptiveCard.GetCard(teamName, teamsPerson1.Name, teamsPerson1.GivenName, teamsPerson2.GivenName, teamsPerson1.UserPrincipalName);

            // Fill in person2's info in the card for person1
            var cardForPerson1 = PairUpNotificationAdaptiveCard.GetCard(teamName, teamsPerson2.Name, teamsPerson2.GivenName, teamsPerson1.GivenName, teamsPerson2.UserPrincipalName);

            await NotifyUser(serviceUrl, cardForPerson1, teamsPerson1, tenantId);
            await NotifyUser(serviceUrl, cardForPerson2, teamsPerson2, tenantId);
        }

        public static async Task WelcomeUser(string serviceUrl, string memberAddedId, string tenantId, string teamId)
        {
            var teamName = await GetTeamNameAsync(serviceUrl, teamId);
            
            var allMembers = await GetTeamMembers(serviceUrl, teamId, tenantId);

            TeamsChannelAccount userThatJustJoined = null;

            foreach (var m in allMembers)
            {
                // both values are 29: values
                if (m.Id == memberAddedId)
                {
                    userThatJustJoined = m;
                }
            }

            var welcomeMessageCard = WelcomeNewMemberCard.GetCard(teamName, userThatJustJoined.Name);

            if (userThatJustJoined != null)
            {
                await NotifyUser(serviceUrl, welcomeMessageCard, userThatJustJoined, tenantId);
            }
            
        }

        private static async Task NotifyUser(string serviceUrl, string cardToSend, ChannelAccount user, string tenantId)
        {
            var me = new ChannelAccount()
            {
                Id = CloudConfigurationManager.GetSetting("MicrosoftAppId"),
                Name = "MeetupBot"
            };

            MicrosoftAppCredentials.TrustServiceUrl(serviceUrl);

            // Create 1:1 with user
            using (var connectorClient = new ConnectorClient(new Uri(serviceUrl)))
            {
                // ensure conversation exists
                var response = connectorClient.Conversations.CreateOrGetDirectConversation(me, user, tenantId);

                // construct the activity we want to post
                var activity = new Activity()
                {
                    Type = ActivityTypes.Message,
                    Conversation = new ConversationAccount()
                    {
                        Id = response.Id,
                    },
                    Attachments = new List<Attachment>()
                    {
                        new Attachment()
                        {
                            ContentType = "application/vnd.microsoft.card.adaptive",
                            Content = JsonConvert.DeserializeObject(cardToSend),
                        }
                    }
                };

                var isTesting = Boolean.Parse(CloudConfigurationManager.GetSetting("Testing"));

                if (! isTesting)
                {
                    // shoot the activity over
                    // added try catch because if user has set "Block conversations with bots"
                    try
                    {
                        await connectorClient.Conversations.SendToConversationAsync(activity, response.Id);
                    }
                    catch (UnauthorizedAccessException uae)
                    {
                        System.Diagnostics.Trace.TraceError($"Failed to notify user due to error {uae.ToString()}");
                    }
                }
                
            }
        }

        public static async Task SaveAddedToTeam(string serviceUrl, string teamId, string tenantId)
        {
            await MeetupBotDataProvider.SaveTeamInstallStatus(new TeamInstallInfo() { ServiceUrl = serviceUrl, TeamId = teamId, TenantId = tenantId }, true);
        }

        public static async Task SaveRemoveFromTeam(string serviceUrl, string teamId, string tenantId)
        {
            await MeetupBotDataProvider.SaveTeamInstallStatus(new TeamInstallInfo() { ServiceUrl = serviceUrl, TeamId = teamId, TenantId = tenantId }, false);
        }

        public static async Task OptOutUser(string tenantId, string userId, string serviceUrl)
        {
            await MeetupBotDataProvider.SetUserOptInStatus(tenantId, userId, false, serviceUrl);
        }

        public static async Task OptInUser(string tenantId, string userId, string serviceUrl)
        {
            await MeetupBotDataProvider.SetUserOptInStatus(tenantId, userId, true, serviceUrl);
        }

        private static async Task<TeamsChannelAccount[]> GetTeamMembers(string serviceUrl, string teamId, string tenantId)
        {
            MicrosoftAppCredentials.TrustServiceUrl(serviceUrl);

            using (var connector = new ConnectorClient(new Uri(serviceUrl)))
            {
                // Pull the roster of specified team and then remove everyone who has opted out explicitly
#pragma warning disable CS0618 // Type or member is obsolete
                var members = await connector.Conversations.GetTeamsConversationMembersAsync(teamId, tenantId);
#pragma warning restore CS0618 // Type or member is obsolete
                return members;
            }
        }

        private static async Task<List<TeamsChannelAccount>> GetOptedInUsers(TeamInstallInfo teamInfo, Dictionary<string, UserOptInInfo> optInInfo)
        {
            var optedInUsers = new List<TeamsChannelAccount>();

            var members = await GetTeamMembers(teamInfo.ServiceUrl, teamInfo.TeamId, teamInfo.TenantId);

            foreach (var member in members)
            {
                var isBot = string.IsNullOrEmpty(member.Surname);
                optInInfo.TryGetValue(member.ObjectId, out UserOptInInfo optInStatus);

                if ((optInStatus == null || optInStatus.OptedIn) && !isBot)
                {
                    optedInUsers.Add(member);
                }
            }

            return optedInUsers;
        }

        private static List<Tuple<TeamsChannelAccount, TeamsChannelAccount>> MakePairs(List<TeamsChannelAccount> incomingUsers, Dictionary<string, UserOptInInfo> optInInfo)
        {
            int attempts = 0;
            while (attempts < 5)
            {
                attempts++;
                // Deep copy the users so we can mess with the list
                var users = JsonConvert.DeserializeObject<List<TeamsChannelAccount>>(JsonConvert.SerializeObject(incomingUsers));
                var pairs = MakePairsInternal(users, optInInfo);
                if (pairs != null)
                {
                    return pairs;
                }
            }

            return MakePairsInternal(incomingUsers, optInInfo, canTryAgain: false);
        }

        private static List<Tuple<TeamsChannelAccount, TeamsChannelAccount>> MakePairsInternal(List<TeamsChannelAccount> users, Dictionary<string, UserOptInInfo> optInInfo, bool canTryAgain = true)
        {
            var pairs = new List<Tuple<TeamsChannelAccount, TeamsChannelAccount>>();

            Randomize<TeamsChannelAccount>(users);

            while (users.Count > 1)
            {
                TeamsChannelAccount user1 = users[0];
                optInInfo.TryGetValue(user1.ObjectId, out var user1OptIn);
                TeamsChannelAccount user2 = null;
                int pointer = 1;
                // Find the first person they haven't been paired with recently.
                while (user2 == null && pointer < users.Count)
                {
                    if (user1OptIn?.RecentPairUps == null || !user1OptIn.RecentPairUps.Contains(users[pointer].ObjectId))
                    {
                        user2 = users[pointer];
                    }
                    pointer++;
                }

                // Pair these two and remove them from the people to pair. If we didn't find someone to pair user1 with, give up on them for this week.
                if (user2 != null)
                {
                    pairs.Add(new Tuple<TeamsChannelAccount, TeamsChannelAccount>(user1, user2));
                    users.Remove(user2);
                }
                else if (canTryAgain)
                {
                    // Didn't find anyone to pair them with. Try again.
                    return null;
                }
                users.Remove(user1);
            }
            
            return pairs;
        }

        public static void Randomize<T>(IList<T> items)
        {
            Random rand = new Random(Guid.NewGuid().GetHashCode());

            // For each spot in the array, pick
            // a random item to swap into that spot.
            for (int i = 0; i < items.Count - 1; i++)
            {
                int j = rand.Next(i, items.Count);
                T temp = items[i];
                items[i] = items[j];
                items[j] = temp;
            }
        }
    }
    
}