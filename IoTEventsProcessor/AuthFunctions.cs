using System.IO;
using System.Security.Claims;
using System.Threading.Tasks;
using IoTEventsProcessor.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace IoTEventsProcessor
{
    public static class AuthFunctions
    {
        static AuthService authService = new AuthService();

        [FunctionName("GetToken")]
        public static async Task<IActionResult> GetToken(
            [HttpTrigger(AuthorizationLevel.User, new[] { "post" }, Route = "token")] HttpRequest req,
            ILogger log)
        {
            var request = JsonConvert.DeserializeObject<TokenRequest>(new StreamReader(req.Body).ReadToEnd());
            if (!string.IsNullOrEmpty(request.username) && !string.IsNullOrEmpty(request.password))
            {
                if (request.username == request.password) //fake user validation
                {
                    return new OkObjectResult(authService.GenerateToken(payload: new []{
                        new Claim(ClaimTypes.Name, request.username)
                    }));
                }
            }
            return new UnauthorizedResult();
        }
    }
}
