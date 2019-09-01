using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.EventHubs;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.SignalRService;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace IoTEventsProcessor
{

    /// <summary>
    /// Read https://docs.microsoft.com/en-us/azure/azure-signalr/signalr-quickstart-azure-functions-csharp
    /// Based on https://github.com/Azure-Samples/signalr-service-quickstart-serverless-chat
    /// </summary>
    public static class EventsFunction
    {
        static AuthService authService = new AuthService();

        [FunctionName("EventsHook")]
        public static async Task Run(
            [EventHubTrigger("%AzureEventsHubName%", Connection = "AzureEventsHubConnectionString")] EventData[] events,
            [SignalR(HubName = "%AzureSignalRHubName%", ConnectionStringSetting = "AzureSignalRConnectionString")] IAsyncCollector<SignalRMessage> signalRMessages,
            ILogger log)
        {
            var exceptions = new List<Exception>();

            foreach (EventData eventData in events)
            {
                try
                {
                    string messageBody = Encoding.UTF8.GetString(eventData.Body.Array, eventData.Body.Offset, eventData.Body.Count);
                    log.LogInformation($"Event: {messageBody}");
                    log.LogInformation($"EnqueuedTimeUtc={eventData.SystemProperties.EnqueuedTimeUtc}");
                    log.LogInformation($"SequenceNumber={eventData.SystemProperties.SequenceNumber}");
                    log.LogInformation($"Offset={eventData.SystemProperties.Offset}");
                    var message = JsonConvert.SerializeObject(new { Body = messageBody, eventData.Properties, eventData.SystemProperties });
                    await signalRMessages.AddAsync(
                        new SignalRMessage
                        {
                            UserId = "cli",
                            Target = "iotevents",
                            Arguments = new[] { new {
                                sender = "cloud",
                                text = message
                            } }
                        });

                    await Task.Yield();
                }
                catch (Exception e)
                {
                    // We need to keep processing the rest of the batch - capture this exception and continue.
                    // Also, consider capturing details of the message that failed processing so it can be processed again later.
                    exceptions.Add(e);
                }
            }

            // Once processing of the batch is complete, if any messages in the batch failed processing throw an exception so that there is a record of the failure.

            if (exceptions.Count > 1)
                throw new AggregateException(exceptions);

            if (exceptions.Count == 1)
                throw exceptions.Single();
        }
    }
}
