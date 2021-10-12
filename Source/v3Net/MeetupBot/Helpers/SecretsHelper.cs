using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;

using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Microsoft.Azure;

namespace MeetupBot.Helpers
{
    public class SecretsHelper
    {
        internal static async Task<string> GetSecretAsync(string secretName)
        {
            string value = String.Empty;
            var kvUri = CloudConfigurationManager.GetSetting("KeyVaultURI");

            System.Diagnostics.Trace.TraceInformation($"Retrieving secret: {secretName} from KeyVault: {kvUri}");

            try
            {
                var client = new SecretClient(new Uri(kvUri), new DefaultAzureCredential());
                var secret = await client.GetSecretAsync(secretName).ConfigureAwait(false);
                value = secret.Value.Value;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.TraceInformation($"Got Exception retrieving Secret. Details: {ex}");
            }

            System.Diagnostics.Trace.TraceInformation($"Got secret: {secretName} from KeyVault: {kvUri}");
            return value;
        }
    }
}