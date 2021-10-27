namespace MeetupBot
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Threading.Tasks;
    using System.Web.Http;
	using global::MeetupBot.Helpers;
	using Microsoft.Bot.Connector;
    using Microsoft.Bot.Connector.Teams;
    using Microsoft.Bot.Connector.Teams.Models;
    using Properties;  

    [BotAuthentication]
    public class MessagesController : ApiController
    {
        /// <summary>
        /// POST: api/Messages
        /// Receive a message from a user and reply to it
        /// </summary>
        public async Task<HttpResponseMessage> Post([FromBody]Activity activity)
        {
            System.Diagnostics.Trace.TraceInformation($"Bot posted a message for Activity : {activity.Type}");

            if (activity.Type == ActivityTypes.Message)
            {
                string replyText = null;

                var optOutRequst = false;

                if (activity.Value != null && ((dynamic)activity.Value).optout == true)
                {
                    optOutRequst = true;
                }

                try
                {
                    var senderInfo = activity.From.AsTeamsChannelAccount();
                    var senderAadId = senderInfo.Properties["aadObjectId"].ToString();
                    var senderName = senderInfo.Name;

                    if (optOutRequst || string.Equals(activity.Text, "optout", StringComparison.InvariantCultureIgnoreCase))
                    {
                        System.Diagnostics.Trace.TraceInformation($"Received an Opt-out request");

                        await MeetupBot.OptOutUser(activity.GetChannelData<TeamsChannelData>().Tenant.Id, senderAadId, senderName);
                        replyText = Resources.OptOutConfirmation;
                    }
                    else if (string.Equals(activity.Text, "optin", StringComparison.InvariantCultureIgnoreCase))
                    {
                        System.Diagnostics.Trace.TraceInformation($"Received an Opt-in request");

                        await MeetupBot.OptInUser(activity.GetChannelData<TeamsChannelData>().Tenant.Id, senderAadId, senderName);
                        replyText = Resources.OptInConfirmation;
                    }
                    else
                    {
                        replyText = Resources.IDontKnow;
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Trace.TraceError(ex.ToString());
                    replyText = Resources.ErrorOccured;
                }

                using (var connectorClient = new ConnectorClient(new Uri(activity.ServiceUrl)))
                {
                    var replyActivity = activity.CreateReply(replyText);

                    await connectorClient.Conversations.ReplyToActivityAsync(replyActivity);
                }

            }
            else
            {
                await HandleSystemMessage(activity);
            }

            var response = Request.CreateResponse(HttpStatusCode.OK);
            return response;
        }

        private async Task<Activity> HandleSystemMessage(Activity message)
        {
            System.Diagnostics.Trace.TraceInformation($"Processing system message for Activity : {message.Type}");

            try
            {
                var channelData = message.GetChannelData<TeamsChannelData>();

                if (message.Type == ActivityTypes.ConversationUpdate)
                {
                    // conversation-update fires whenever a new 1:1 gets created between us and someone else as well
                    // only process the Teams ones.
                    var teamsChannelData = message.GetChannelData<TeamsChannelData>();

                    if (teamsChannelData.Team == null || string.IsNullOrEmpty(teamsChannelData.Team.Id))
                    {
                        System.Diagnostics.Trace.TraceInformation($"Conversation-update is for 1:1 chat. Just ignore.");
                        return null;
                    }

                    string myId = message.Recipient.Id;

                    if (message.MembersAdded.Count > 0)
                    {
                        System.Diagnostics.Trace.TraceInformation($"Members to be added : {message.MembersAdded.Count}");

                        foreach (var member in message.MembersAdded)
                        {
                            var memberId = member.Id;

                            if (memberId.Equals(myId))
                            {
                                // we were just added to team
                                System.Diagnostics.Trace.TraceInformation($"Bot added to the Team: [{channelData.Team.Name}]");

                                var teamInfo = new TeamInstallInfo()
                                {
                                    TeamId = message.Conversation.Id,
                                    Teamname = channelData.Team.Name,
                                    TenantId = channelData.Tenant.Id,
                                    ServiceUrl = message.ServiceUrl,
                                    PairingStatus = PairingStatus.New.ToString(),
                                    LastPairedAtUTC = "New Team"
                                };
                                await MeetupBot.SaveTeam(teamInfo, TeamUpdateType.Add);
                                // TODO: post activity.from has who added the bot. Can record it in schema.
                            }
                            else if (!string.IsNullOrEmpty(memberId)) // If I wasn't added or removed, then someome else must have been added to team
                            {
                                // someone else was added
                                // send them a welcome message
                                await MeetupBot.WelcomeUser(message.ServiceUrl, memberId, channelData.Tenant.Id, channelData.Team.Id);
                            }
                        }
                    }

                    if (message.MembersRemoved.Count > 0)
                    {
                        System.Diagnostics.Trace.TraceInformation($"Members to be removed : {message.MembersRemoved.Count}");

                        foreach (var member in message.MembersRemoved)
                        {
                            var memberId = member.Id;

                            if (memberId.Equals(myId))
                            {
                                // we were just removed from a team
                                System.Diagnostics.Trace.TraceInformation($"Bot removed from the team: [{channelData.Team.Name}]");
                                var teamInfo = new TeamInstallInfo()
                                {
                                    TeamId = message.Conversation.Id,
                                    Teamname = channelData.Team.Name,
                                    TenantId = channelData.Tenant.Id,
                                    ServiceUrl = message.ServiceUrl,
                                };
                                await MeetupBot.SaveTeam(teamInfo, TeamUpdateType.Remove);
                            }
                        }
                    }
                }

                return null;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.TraceError(ex.Message);
                throw;
            }
        }
    }
}