using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Zeebe.Client;
using Zeebe.Client.Api.Responses;
using Zeebe.Client.Api.Worker;

namespace api1.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class ZeebeController : ControllerBase
    {
        private readonly ILogger<ZeebeController> _logger;
        
        private static readonly string ZeebeUrl = "127.0.0.1:26500";
        private static readonly string MessageName = "receive-signing-event-message";

        public ZeebeController(ILogger<ZeebeController> logger)
        {
            _logger = logger;
        }

        [HttpGet]
        public string Ping()
        {
            return "Pong";
        }

        [HttpPost]
        public async Task<IActionResult> Post([FromBody] ZeebeRequest request)
        {
             var client = ZeebeClient.Builder()
                .UseGatewayAddress(ZeebeUrl)
                .UsePlainText()
                .Build();

            await client
                .NewPublishMessageCommand()
                .MessageName(MessageName)
                .CorrelationKey(request.CorrelationKey)
                .Variables("{\"signingCompleted\":true}")
                .Send();
            
            return Ok();
        }
    }

    public class ZeebeRequest
    {
        public string CorrelationKey { get; set; }
    }
}
