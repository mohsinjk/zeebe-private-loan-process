using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using NLog.Extensions.Logging;
using Zeebe.Client;
using Zeebe.Client.Api.Responses;
using Zeebe.Client.Api.Worker;

namespace Client1
{
    internal class Program
    {
        private static readonly string ZeebeUrl = "127.0.0.1:26500";
        private static readonly string ProcessId = "create-privateloan-process";

        public static async Task Main(string[] args)
        {
            if (args.Length == 0)
            {
                Console.WriteLine("Please enter a numeric value as parameter.");
                return;
            }

            var client = ZeebeClient.Builder()
                .UseLoggerFactory(new NLogLoggerFactory())
                .UseGatewayAddress(ZeebeUrl)
                .UsePlainText()
                .Build();

            string variables = "{\"customerId\": \"" + args[0] + "\",\"loanAmount\": " + args[1] + ",\"customerName\": \"ABC XYZ\"}";

            Console.WriteLine($"Starting workflow with id: " + args[0]);

            await client
                .NewCreateWorkflowInstanceCommand()
                .BpmnProcessId(ProcessId)
                .LatestVersion()
                .Variables(variables)
                .Send();
        }
    }
}
