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
            var sequenceId = -1;
            var resourceUrl = "";
            var callGuid = "";
            string toneId = "";
            // convert requestbody to Rootobject
            CallData callData = JsonSerializer.Deserialize<CallData>(requestBody);
            //  callData.value[0].resourceData.toneInfo
            // check if toneinfo is present 
            if (callData.value[0].resourceData.toneInfo != null)
            {
                _logger.LogInformation($"ToneInfo: {callData.value[0].resourceData.toneInfo}");
                // extract the toneinfo
                var toneInfo = callData.value[0].resourceData.toneInfo;
                _logger.LogInformation($"ToneInfo: {toneInfo}");
                // check if sequenceId is present
                if (toneInfo.sequenceId != 0)
                {
                    sequenceId = toneInfo.sequenceId;
                    toneId = toneInfo.tone;   
                    resourceUrl = callData.value[0].resource;
                   // string pattern = @"[0-9a-fA-F\-]{36}";
                   // Match match = Regex.Match(resourceUrl, pattern);
                    _logger.LogWarning($"Resource URL: {resourceUrl}");
                    // Inside the RunAsync method, after extracting the resourceUrl
                    if (!string.IsNullOrEmpty(resourceUrl))
                    {
                        string path = resourceUrl;
                         callGuid = path.Split('/').Last();


                        //var match = Regex.Match(resourceUrl, @"\/app\/calls\/([a-f0-9\-]+)\/operations\/");
                        //   string pattern = @"[0-9a-fA-F\-]{36}";
                        // Match match = Regex.Match(resourceUrl, pattern);
                        //  if (match.Success)
                        //{
                        //    // extract the value

                        //    callGuid = match.Groups[1].Value;
                          _logger.LogInformation($"Extracted Call GUID: {callGuid}");
                        //}
                        //else
                        //{
                        //    _logger.LogInformation("Call GUID not found in resourceUrl");
                        //}
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

            var authResult = await clientApplication.AcquireTokenForClient(scopes).ExecuteAsync();
            // if you want to display the Auth Token 
            //var authToken = authResult.AccessToken;
            // _logger.LogInformation($"Auth Token: {authToken}");

            var mp3Url = Environment.GetEnvironmentVariable("mp3Url");
            ArgumentNullException.ThrowIfNullOrEmpty(mp3Url);
            var graphClient = new GraphServiceClient(authProvider);
            var result = await graphClient.Communications.Calls[payload.CallId].GetAsync();
            _logger.LogInformation($"Call ID: {callGuid}");
            if (toneId.Equals("tone1", StringComparison.Ordinal))
            {
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
                                Uri = "https://github.com/ahmedsza/SimpleWebApp/raw/refs/heads/main/thanks.wav",
                                ResourceId = Guid.NewGuid().ToString(),
                            },
                        },
                    }
                });
            }
            //_logger.LogWarning(result.ToString());


            return new OkObjectResult("Logger called");

        }
    }
}

public class Rootobject
{
    public string odatatype { get; set; }
    public Value[] value { get; set; }
}

public class Value
{
    public ToneInfo ToneInfo { get; set; }
}

public class ToneInfo
{
    // Define properties of ToneInfo here
}
