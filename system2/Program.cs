using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using NLog.Extensions.Logging;
using Zeebe.Client;
using Zeebe.Client.Api.Responses;
using Zeebe.Client.Api.Worker;

namespace Worker2
{
    internal class Program
    {

        private static readonly string ZeebeUrl = "127.0.0.1:26500";
        private static readonly string MessageName = "receive-offer-response-event-message";

        public static async Task Main(string[] args)
        {
            if (args.Length == 0)
            {
                Console.WriteLine("Please enter a offer response.");
                return;
            }

            var client = ZeebeClient.Builder()
                .UseLoggerFactory(new NLogLoggerFactory())
                .UseGatewayAddress(ZeebeUrl)
                .UsePlainText()
                .Build();

            await client
                .NewPublishMessageCommand()
                .MessageName(MessageName)
                .CorrelationKey(args[0])
                .Variables("{\"signingCompleted\":\"" + args[0] + "\"}")
                .Send();
            
            Console.WriteLine("Publish signing event message with correlation id: " + args[0]);
        }
    }
}
