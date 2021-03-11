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
        private static readonly string ProcessId = "create-isk-process";

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

            string variables = "{\"customer_id\": \"" + args[0] + "\",\"monthly_saving\": 500,\"source_account_id\": \"87fd78b4-de0e-4899-ba02-a1aee2f5a47b\",\"funds\": [{\"fund_id\": \"SEB Emerging Marketsfond\",\"allocation\": 50},{\"fund_id\": \"SEB Asienfond ex-Japan\",\"allocation\": 25},{\"fund_id\": \"SEB Sverige Småbolag\",\"allocation\": 25}]}";

            Console.WriteLine("Starting workflow with id: " + args[0]);

            await client
                .NewCreateWorkflowInstanceCommand()
                .BpmnProcessId(ProcessId)
                .LatestVersion()
                .Variables(variables)
                .Send();
        }
    }
}
