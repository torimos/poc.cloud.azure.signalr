using Microsoft.AspNetCore.SignalR.Client;
using Newtonsoft.Json;
using System;
using System.Net.Http;

namespace EventsListener
{
    class Program
    {
        static void Main(string[] args)
        {
            string HostName = "http://localhost:7071";
            SignalRService s = new SignalRService(HostName, "iotevents");

            string username = args.Length > 0 ? args[0] : "cli";
            string recipient = args.Length > 1 ? args[1] : null;
            string token = s.AuthenticateAsync(username, username).GetAwaiter().GetResult();

            s.NewMessageReceived += OnNewMessageReceived;
            s.Connected += OnConnectionChange;
            s.ConnectionFailed += OnConnectionChange;
            s.ConnectAsync(token).Wait();

            string input;
            while((input = Console.ReadLine()) != "exit")
            {
                s.SendMessageAsync(token, recipient, input).Wait();
            }
            Console.Read();
        }

        private static void OnConnectionChange(object sender, bool successful, string message)
        {
            if (successful)
                Console.WriteLine($"OnConnectionSuccessfull: {message}");
            else
                Console.WriteLine($"OnConnectionFailed: {message}");
        }

        private static void OnNewMessageReceived(object sender, dynamic message)
        {
            Console.WriteLine($"OnNewMessageReceived: {JsonConvert.SerializeObject(message)}");
        }
    }
}
