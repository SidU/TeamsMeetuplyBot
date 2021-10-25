using System.Net;

namespace TriggerWebJob
{
    class Program
    {
        static void Main(string[] args)
        {
            // trigger pairing for LetsMeethackathon Team
            var webRequest = WebRequest.Create($"https://meetupbotappservice.azurewebsites.net/api/processnow/19:bptvDEOTBztyG4t6Z-QFg95s61PV6Vb7Y6E5GECNb2E1@thread.tacv2");
            webRequest.Method = "POST";
            webRequest.GetResponse();
        }
    }
}
