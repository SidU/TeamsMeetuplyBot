using System.Net;

namespace TriggerWebJob
{
    class Program
    {
        static void Main(string[] args)
        {
            var webRequest = WebRequest.Create($"https://meetupbotappservice.azurewebsites.net/api/processnow/dd39eaa6-fcae-4ca9-b10f-62570c664e51");
            webRequest.GetResponse();
        }
    }
}
