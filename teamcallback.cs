using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace TeamsACSFunctions
{
    public class teamcallback
    {
        private readonly ILogger<teamcallback> _logger;

        public teamcallback(ILogger<teamcallback> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Azure Function that processes HTTP GET and POST requests.
        /// </summary>
        /// <param name="req">The HTTP request object.</param>
        /// <returns>An IActionResult indicating the result of the function execution.</returns>
        [Function("teamcallback")]
        public async Task<IActionResult> RunAsync([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequest req)
        {
            _logger.LogInformation("C# HTTP trigger function processed a request.");
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            var payload = JsonSerializer.Deserialize<CommsNotificationsPayload>(requestBody);

            _logger.LogInformation($"requestBody: {requestBody}");
            _logger.LogInformation($"Teams notification received at: {DateTime.UtcNow}");
            _logger.LogInformation($"Payload: {payload.CallId}");
            _logger.LogInformation($"CallState: {payload.CallState}");

            return new OkObjectResult("Logger called");
        }
    }
}
