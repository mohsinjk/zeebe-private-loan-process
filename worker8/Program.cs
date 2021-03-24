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
        private static readonly string JobType = "create-promissory-note";
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

                Console.WriteLine("Worker 8 with job type '{0}' is running in {1} mode.", JobType, test ? "test" : "normal");

                // blocks main thread, so that worker can run
                signal.WaitOne();
            }
        }
        private static void HandleJob(IJobClient jobClient, IJob job)
        {
            // business logic
            var jobKey = job.Key;
            Console.WriteLine("Worker 8 handling job: " + job);

            Thread.Sleep(3000);

            if (!test)
            {
                Console.WriteLine("Worker 8 completes job successfully.");
                jobClient.NewCompleteJobCommand(jobKey)
                    .Variables("{\"promissory-note\":\"A6221ds.xml\"}")
                    .Send()
                    .GetAwaiter()
                    .GetResult();
            }
            else
            {
                Console.WriteLine("Worker 8 failing with message: {0}", "Activation Fault Message");
                jobClient.NewThrowErrorCommand(jobKey)
                    .ErrorCode("Activation Fault")
                    .ErrorMessage("Activation Fault Message")
                    .Send()
                    .GetAwaiter()
                    .GetResult();
            }
        }
    }
}
