using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.Graph;
using Microsoft.Graph.Communications.Calls.Item.PlayPrompt;
using Microsoft.Graph.Models;
using Microsoft.Identity.Client;
using System.Text.Json;
using System.Text.RegularExpressions;
using static System.Formats.Asn1.AsnWriter;

namespace TeamsACSFunctions
{
    public class teamcallback
    {
        private readonly ILogger<teamcallback> _logger;

        public teamcallback(ILogger<teamcallback> logger)
        {
            _logger = logger;
        }

        [Function("teamcallback")]
        public async Task<IActionResult> RunAsync([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequest req)
        {
            _logger.LogInformation("C# HTTP trigger function processed a request.");
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            var payload = JsonSerializer.Deserialize<CommsNotificationsPayload>(requestBody);

            LogRequestDetails(requestBody, payload);

            var callData = JsonSerializer.Deserialize<CallData>(requestBody);
            var (sequenceId, toneId, resourceUrl, callGuid) = ExtractToneInfo(callData);

            if (sequenceId != -1)
            {
                var graphClient = await GetGraphClientAsync();
                await HandleToneAsync(graphClient, payload.CallId, toneId, callGuid);
            }

            return new OkObjectResult("Logger called");
        }

        private void LogRequestDetails(string requestBody, CommsNotificationsPayload payload)
        {
            _logger.LogInformation($"requestBody: {requestBody}");
            _logger.LogInformation($"Teams notification received at: {DateTime.UtcNow}");
            _logger.LogInformation($"Payload: {payload.CallId}");
            _logger.LogInformation($"CallState: {payload.CallState}");
        }

        private (int sequenceId, string toneId, string resourceUrl, string callGuid) ExtractToneInfo(CallData callData)
        {
            int sequenceId = -1;
            string toneId = "";
            string resourceUrl = "";
            string callGuid = "";

            if (callData.value[0].resourceData.toneInfo != null)
            {
                var toneInfo = callData.value[0].resourceData.toneInfo;
                _logger.LogInformation($"ToneInfo: {toneInfo}");

                if (toneInfo.sequenceId != 0)
                {
                    sequenceId = toneInfo.sequenceId;
                    toneId = toneInfo.tone;
                    resourceUrl = callData.value[0].resource;
                    _logger.LogWarning($"Resource URL: {resourceUrl}");

                    if (!string.IsNullOrEmpty(resourceUrl))
                    {
                        callGuid = resourceUrl.Split('/').Last();
                        _logger.LogInformation($"Extracted Call GUID: {callGuid}");
                    }

                    _logger.LogInformation($"SequenceId: {toneInfo.sequenceId}");
                }
                else
                {
                    _logger.LogInformation("SequenceId is null");
                }
            }
            else
            {
                _logger.LogInformation("ToneInfo is null");
            }

            return (sequenceId, toneId, resourceUrl, callGuid);
        }

        private async Task<GraphServiceClient> GetGraphClientAsync()
        {
            var clientId = Environment.GetEnvironmentVariable("clientId");
            ArgumentNullException.ThrowIfNullOrEmpty(clientId);

            var tenantId = Environment.GetEnvironmentVariable("tenantId");
            ArgumentNullException.ThrowIfNullOrEmpty(tenantId);

            var clientSecret = Environment.GetEnvironmentVariable("clientSecret");
            ArgumentNullException.ThrowIfNullOrEmpty(clientSecret);

            var scopes = new[] { "https://graph.microsoft.com/.default" };
            var clientApplication = ConfidentialClientApplicationBuilder
                .Create(clientId)
                .WithClientSecret(clientSecret)
                .WithAuthority(new Uri($"https://login.microsoftonline.com/{tenantId}/v2.0"))
                .Build();

            var authProvider = new ClientCredentialProvider(clientApplication);
            await clientApplication.AcquireTokenForClient(scopes).ExecuteAsync();

            return new GraphServiceClient(authProvider);
        }

        private async Task HandleToneAsync(GraphServiceClient graphClient, string callId, string toneId, string callGuid)
        {
            var result = await graphClient.Communications.Calls[callId].GetAsync();
            _logger.LogInformation($"Call ID: {callGuid}");

            if (toneId.Equals("tone1", StringComparison.Ordinal))
            {
                var ThanksmediaUri = Environment.GetEnvironmentVariable("ThanksmediaUri");
                ArgumentNullException.ThrowIfNullOrEmpty(ThanksmediaUri);

                var playPromptOperation = await graphClient.Communications.Calls[callGuid].PlayPrompt.PostAsync(new PlayPromptPostRequestBody
                {
                    ClientContext = callGuid,
                    Prompts = new List<Microsoft.Graph.Models.Prompt>
                        {
                            new Microsoft.Graph.Models.MediaPrompt
                            {
                                OdataType = "#microsoft.graph.mediaPrompt",
                                MediaInfo = new Microsoft.Graph.Models.MediaInfo
                                {
                                    OdataType = "#microsoft.graph.mediaInfo",
                                    Uri = ThanksmediaUri,
                                    ResourceId = Guid.NewGuid().ToString(),
                                },
                            },
                        }
                });
            }
        }
    }
}

