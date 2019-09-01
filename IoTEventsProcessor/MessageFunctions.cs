using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using IoTEventsProcessor.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Extensions.SignalRService;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace IoTEventsProcessor
{
    public static class MessageFunctions
    {
        static AuthService authService = new AuthService();
        
        [FunctionName("Messages")]
        public static async Task<IActionResult> SendMessage(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "messages")] HttpRequest req,
            [SignalR(HubName = "%AzureSignalRHubName%", ConnectionStringSetting = "AzureSignalRConnectionString")] IAsyncCollector<SignalRMessage> signalRMessages,
            ILogger log)
        {
            var target = req.Query["target"];
            var json = await new StreamReader(req.Body).ReadToEndAsync();
            var request = JsonConvert.DeserializeObject<MessageRequest>(json);
            log.LogInformation($"Sending to target: {target}");
            log.LogInformation($"Sending to recipient: {request.recipient}");
            log.LogInformation($"Sending body: {request.message}");

            var token = req.Headers["Authorization"].FirstOrDefault()?.Split(' ').Last();
            var user = authService.ValidateToken(token);
            if (user.Identity.IsAuthenticated)
            {
                await signalRMessages.AddAsync(
                    new SignalRMessage
                    {
                        UserId = request.recipient,
                        Target = target,
                        Arguments = new[] { new {
                            sender = user.Identity.Name,
                            text = request.message
                        } }
                    });
                return new OkResult();
            }
            else
            {
                log.LogError(new UnauthorizedAccessException(), "User is not authorized");
                return new UnauthorizedResult();
            }
        }
    }
}
