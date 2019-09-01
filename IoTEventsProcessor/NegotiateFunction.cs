using System;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Extensions.SignalRService;
using Microsoft.Extensions.Logging;

namespace IoTEventsProcessor
{
    public static class NegotiateFunction
    {
        static AuthService authService = new AuthService();

        [FunctionName("Negotiate")]
        public static IActionResult GetSignalRInfo(
            [HttpTrigger(AuthorizationLevel.Anonymous, new[] { "get", "post" }, Route = "negotiate")] HttpRequest req,
            IBinder binder,
            ILogger log)
        {
            var token = req.Headers["Authorization"].FirstOrDefault()?.Split(' ').Last();
            var user = authService.ValidateToken(token);
            if (user.Identity.IsAuthenticated)
            {
                var connectionInfo = binder.Bind<SignalRConnectionInfo>(
                    new SignalRConnectionInfoAttribute
                    {
                        HubName = "%AzureSignalRHubName%",
                        ConnectionStringSetting = "AzureSignalRConnectionString",
                        UserId = user.Identity.Name
                    });

                log.LogInformation($"Negotiation Url: {connectionInfo.Url}");
                log.LogInformation($"Negotiation AccessToken: {connectionInfo.AccessToken}");
                return new OkObjectResult(connectionInfo);
            }
            else
            {
                log.LogError(new UnauthorizedAccessException(), "User is not authorized");
                return new UnauthorizedResult();
            }
        }
    }
}
