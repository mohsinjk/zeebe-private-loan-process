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

namespace Worker3
{
    internal class Program
    {
        private static readonly string ZeebeUrl = "127.0.0.1:26500";
        private static readonly string JobType = "create-recurring-transfer";
        private static readonly string WorkerName = Environment.MachineName;
        private static bool test = false;

        #pragma warning disable 1998
        public static async Task Main(string[] args)
        {
            if (Environment.GetEnvironmentVariable("ZEEBE_WORKER_MODE") != null)
                test = Environment.GetEnvironmentVariable("ZEEBE_WORKER_MODE") == "test";
            else
                if (args.Length == 0)
                {
                    Console.WriteLine("Enter a parameter (test or normal) or ...");
                    Console.WriteLine("Set the environment variable ZEEBE_WORKER_MODE to either 'test' or 'normal'.");

                    return;
                }
                else
                    test = (args[0] == "test");

             var client = ZeebeClient.Builder()
                .UseLoggerFactory(new NLogLoggerFactory())
                .UseGatewayAddress(ZeebeUrl)
                .UsePlainText()
                .Build();

            // open job worker
            using (var signal = new EventWaitHandle(false, EventResetMode.AutoReset))
            {
                client.NewWorker()
                      .JobType(JobType)
                      .Handler(HandleJob)
                      .MaxJobsActive(5)
                      .Name(WorkerName)
                      .AutoCompletion()
                      .PollInterval(TimeSpan.FromSeconds(1))
                      .Timeout(TimeSpan.FromMinutes(10))
                      .Open();

                Console.WriteLine("Worker 4 with job type '{0}' is running in {1} mode.", JobType, test?"test":"normal");

                // blocks main thread, so that worker can run
                signal.WaitOne();
            }
        }
        private static void HandleJob(IJobClient jobClient, IJob job)
        {
            // business logic
            var jobKey = job.Key;
            Console.WriteLine("Worker 4 handling job: " + job);

            Thread.Sleep(3000);

            JObject payload = JObject.Parse(job.Variables);
            Console.WriteLine("Account = {0}; Length = {1}", payload["source_account_id"], payload["source_account_id"].ToString().Length);

            if (!test || payload["source_account_id"].ToString().Length != 36) 
            {
                Console.WriteLine("Worker 4 completes job successfully.");
                jobClient.NewCompleteJobCommand(jobKey)
                    .Variables("{\"reccuring_transfer_done\":true}")
                    .Send()
                    .GetAwaiter()
                    .GetResult();
            }
            else
            {
                Console.WriteLine("Worker 4 failing with message: {0}", "Recurring Transfer Fault Message - Source account number is invalid.");
                jobClient.NewFailCommand(jobKey)
                    .Retries(0)
                    .ErrorMessage("Recurring Transfer Fault Message - Source account number is invalid.")
                    .Send()
                    .GetAwaiter()
                    .GetResult();
            }
        }
    }
}
