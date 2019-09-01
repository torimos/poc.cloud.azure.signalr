using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;

namespace IoTEventsProcessor
{    
    /// Open http://localhost:7071/api/events?target=iotevents&from=cli&to=cli1
    public static class EventsViewerFunction
    {
        [FunctionName("EventsViewer")]
        public static HttpResponseMessage GetChatPage(
               [HttpTrigger(AuthorizationLevel.Anonymous, new[] { "get" }, Route = "events")] HttpRequest req,
               ILogger log)
        {
            var response = new HttpResponseMessage(HttpStatusCode.OK);
            var stream = new FileStream(@"wwwroot/eventsViewer.html", FileMode.Open);
            response.Content = new StreamContent(stream);
            response.Content.Headers.ContentType = new MediaTypeHeaderValue("text/html");
            return response;
        }
    }
}
