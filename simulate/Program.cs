using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using NLog.Extensions.Logging;
using Zeebe.Client;
using Zeebe.Client.Api.Responses;
using Zeebe.Client.Api.Worker;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Simulate
{
    internal class Program
    {
        private static readonly string ZeebeUrl = "127.0.0.1:26500";
        private static readonly string ProcessId = "create-isk-process-pt";

        public static async Task Main(string[] args)
        {
            var client = ZeebeClient.Builder()
                .UseLoggerFactory(new NLogLoggerFactory())
                .UseGatewayAddress(ZeebeUrl)
                .UsePlainText()
                .Build();

            for (int i = 0; i < Int32.Parse(args[0]); i++)
            {
                string variables = GeneratePayload(i);

                Console.WriteLine(variables);

                await client
                    .NewCreateWorkflowInstanceCommand()
                    .BpmnProcessId(ProcessId)
                    .LatestVersion()
                    .Variables(variables)
                    .Send();

                Console.WriteLine("Number of requests is {0} of {1}", i + 1, args[0]);

                Thread.Sleep(Int32.Parse(args[1]));
            }
        }

        private static string GeneratePayload(int id)
        {
            dynamic payload = new JObject();
            Random rnd = new Random();

            // DateTime from = new DateTime(2020, 1, 1);
            // DateTime to = new DateTime(2020, 7, 10);

            // TimeSpan range = new TimeSpan(to.Ticks - from.Ticks);
            // DateTime dt = from + new TimeSpan((long)(range.Ticks * rnd.NextDouble()));

            // payload.request_timestamp = dt.ToString("yyyy-MM-ddTHH:mm:ss");

            payload.customer_id = Convert.ToString(id);

            int monthly_saving = rnd.Next(100, 5000);
            payload.monthly_saving = ((int)Math.Round((double)(monthly_saving / 100)) * 100);

            payload.source_account_id = Guid.NewGuid();
            payload.funds = new JArray();

            string[] funds1 = {"ABC Asienfond ex-Japan",
                "ABC Etisk Global Indexfond C USD - Lux",
                "ABC Emerging Marketsfond",
                "ABC Dynamisk Aktiefond",
                "ABC Europafond Småbolag"};

            string[] funds2 = {"ABC Asienfond ex-Kina",
                "ABC Etisk Global Indexfond A USD - Lux",
                "ABC Global Marketsfond",
                "ABC Dynamisk Småbolagsfond",
                "ABC Sverige Småbolag"};

            string[] funds3 = {"ABC Asienfond ex-Asien",
                "ABC Etisk Global Indexfond C EUR - Lux",
                "ABC Bluff Marketsfond",
                "ABC Statisk Aktiefond",
                "ABC Norden Småbolag"};

            dynamic fund1 = new JObject();
            fund1.fund_id = funds1[rnd.Next(1, 100) % 5];
            fund1.allocation = 50;
            payload.funds.Add(fund1);

            dynamic fund2 = new JObject();
            fund2.fund_id = funds2[rnd.Next(1, 100) % 5];
            fund2.allocation = 25;
            payload.funds.Add(fund2);

            dynamic fund3 = new JObject();
            fund3.fund_id = funds3[rnd.Next(1, 100) % 5];
            fund3.allocation = 25;
            payload.funds.Add(fund3);

            return payload.ToString();
        }
    }
}
