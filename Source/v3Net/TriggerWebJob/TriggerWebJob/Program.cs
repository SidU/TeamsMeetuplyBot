using System.Net;

namespace TriggerWebJob
{
    class Program
    {
        static void Main(string[] args)
        {
            // trigger pairing for LetsMeethackathon Team
            var webRequest = WebRequest.Create($"https://meetupbotappservice.azurewebsites.net/api/processnow/4c9762ae-d73a-4aa1-b04f-72aaf8325e3a");
            webRequest.Method = "POST";
            webRequest.GetResponse();
        }
    }
}
