using Microsoft.AspNetCore.SignalR.Client;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace EventsListener
{
    public class SignalRService
    {
        private string hostName;
        private string methodName;

        public delegate void MessageReceivedHandler(object sender, dynamic message);
        public delegate void ConnectionHandler(object sender, bool successful, string message);

        public event MessageReceivedHandler NewMessageReceived;
        public event ConnectionHandler Connected;
        public event ConnectionHandler ConnectionFailed;
        public bool IsConnected { get; private set; }
        public bool IsBusy { get; private set; }

        public SignalRService(string hostName, string methodName)
        {
            this.hostName = hostName;
            this.methodName = methodName;
        }

        public async Task SendMessageAsync(string authToken, string recipient, string message)
        {
            IsBusy = true;

            var newMessage = new
            {
                recipient,
                message
            };
            var client = new HttpClient();
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {authToken}");
            var result = await client.PostAsync($"{hostName}/api/messages?target={methodName}", AsJson(newMessage));

            IsBusy = false;
        }

        public async Task ConnectAsync(string authToken)
        {
            try
            {
                IsBusy = true;
                var client = new HttpClient();
                client.DefaultRequestHeaders.Add("Authorization", $"Bearer {authToken}");
                string negotiateJson = await client.GetStringAsync($"{hostName}/api/negotiate");
                NegotiateInfo negotiate = JsonConvert.DeserializeObject<NegotiateInfo>(negotiateJson);
                HubConnection connection = new HubConnectionBuilder()
                    .WithUrl(negotiate.Url, options =>
                    {
                        options.AccessTokenProvider = async () => negotiate.AccessToken;
                    })
                    .Build();
                connection.Closed += Connection_Closed;
                connection.On<JObject>(methodName, AddNewMessage);
                await connection.StartAsync();
                IsConnected = true;
                IsBusy = false;

                Connected?.Invoke(this, true, "Connection successful.");
            }
            catch (Exception ex)
            {
                ConnectionFailed?.Invoke(this, false, ex.Message);
                IsConnected = false;
                IsBusy = false;
            }
        }
        public async Task<string> AuthenticateAsync(string username, string password)
        {
            var client = new HttpClient();
            var response = await client.PostAsync($"{hostName}/api/token", AsJson(new { username, password }));
            var token = await response.Content.ReadAsStringAsync();
            return token;
        }

        Task Connection_Closed(Exception arg)
        {
            ConnectionFailed?.Invoke(this, false, arg.Message);
            IsConnected = false;
            IsBusy = false;
            return Task.CompletedTask;
        }

        void AddNewMessage(JObject message)
        {
            var messageModel = new
            {
                sender = message.GetValue("sender").ToString(),
                text = message.GetValue("text").ToString(),
                timereceived = DateTime.Now
            };

            NewMessageReceived?.Invoke(this, messageModel);
        }

        public static StringContent AsJson(object o) => new StringContent(JsonConvert.SerializeObject(o), Encoding.UTF8, "application/json");
    }
}
