using Azure.Communication.CallAutomation;
using Azure.Communication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace TeamsACSFunctions
{
    /// <summary>
    /// Azure Function to handle calling a user via Azure Communication Services.
    /// </summary>
    public class CallUserACS
    {
        private readonly ILogger<CallUserACS> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="CallUserACS"/> class.
        /// </summary>
        /// <param name="logger">The logger instance.</param>
        public CallUserACS(ILogger<CallUserACS> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Azure Function entry point to initiate a call to a user.
        /// </summary>
        /// <param name="req">The HTTP request containing the ticket data.</param>
        /// <returns>An <see cref="IActionResult"/> indicating the result of the operation.</returns>
        [Function("CallUserACS")]
        public async Task<IActionResult> RunAsync([HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequest req)
        {
            var acsConnectionString = Environment.GetEnvironmentVariable("AcsConnectionString");
            ArgumentNullException.ThrowIfNullOrEmpty(acsConnectionString);

            var callbackUriHost = Environment.GetEnvironmentVariable("callbackUriHost");
            ArgumentNullException.ThrowIfNullOrEmpty(callbackUriHost);

            var cognitiveServicesEndpoint = Environment.GetEnvironmentVariable("cognitiveServicesEndpoint");
            ArgumentNullException.ThrowIfNullOrEmpty(cognitiveServicesEndpoint);

            _logger.LogInformation("C# HTTP trigger function processed a request.");
            var ticket = await JsonSerializer.DeserializeAsync<Ticket>(req.Body);

            // get a string json representation of the ticket
            var ticketJson = JsonSerializer.Serialize(ticket);
            if (ticket == null || string.IsNullOrEmpty(ticket.UserID))
            {
                return new BadRequestObjectResult("Invalid ticket data.");
            }

            var callAutomationClient = new CallAutomationClient(acsConnectionString, new CallAutomationClientOptions());

            var teamsUserId = ticket.UserID;
            var callInvite = new CallInvite(new MicrosoftTeamsUserIdentifier(teamsUserId))
            {
                SourceDisplayName = "Contoso Support"
            };

            var createCallOptions = new CreateCallOptions(callInvite, new Uri(callbackUriHost))
            {
                CallIntelligenceOptions = new CallIntelligenceOptions()
                {
                    CognitiveServicesEndpoint = new Uri(cognitiveServicesEndpoint)
                },
                OperationContext = ticketJson
            };

            var callConnection = await callAutomationClient.CreateCallAsync(createCallOptions);
            return new OkObjectResult(callConnection.GetRawResponse().ToString());
        }
    }
}
