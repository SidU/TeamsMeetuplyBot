using System.Net;

namespace TriggerEDUChatRouletteBot
{
    class Program
    {
        static void Main(string[] args)
        {
            var webRequest = WebRequest.Create($"https://<your_bot>.azurewebsites.net/api/processnow/<key>");
            webRequest.GetResponse();
        }
    }
}
