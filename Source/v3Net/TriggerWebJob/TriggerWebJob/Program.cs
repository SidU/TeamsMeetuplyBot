using System.Net;

namespace TriggerWebJob
{
    class Program
    {
        static void Main(string[] args)
        {
            // trigger pairing for LetsMeethackathon Team
            var webRequest = WebRequest.Create($"https://meetupbotappservice.azurewebsites.net/api/processnow/0dace592-0613-467f-a46e-d1f0905b0770");
            webRequest.Method = "POST";
            webRequest.GetResponse();
        }
    }
}
