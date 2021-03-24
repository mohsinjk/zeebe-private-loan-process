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
        private static readonly string JobType = "create-kafka-data";
        private static readonly string WorkerName = Environment.MachineName;

#pragma warning disable 1998
        public static async Task Main(string[] args)
        {

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

                Console.WriteLine("Worker 8 with job type '{0}' is running in {1} mode.", JobType,  "normal");

                // blocks main thread, so that worker can run
                signal.WaitOne();
            }
        }
        private static void HandleJob(IJobClient jobClient, IJob job)
        {
            // business logic
            Console.WriteLine("Worker 8 handling job: " + job);

        }
    }
}
