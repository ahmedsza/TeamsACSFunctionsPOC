using Azure.Communication;
using Azure.Communication.CallAutomation;
using Azure.Messaging;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using System.Runtime.CompilerServices;
using System.Text.Json;

namespace TeamsACSFunctions
{
    public class callback
    {
        private readonly ILogger<callback> _logger;

        public callback(ILogger<callback> logger)
        {
            _logger = logger;
        }

        [Function("callback")]
        public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequest req)
        {
            _logger.LogInformation("C# HTTP trigger function processed a request.");
            // log the request body

            using (var memoryStream = new MemoryStream())
            {

                await req.Body.CopyToAsync(memoryStream);
                var bytes = memoryStream.ToArray();
                // Parse the JSON payload into a list of events
                CloudEvent[] cloudEvents = CloudEvent.ParseMany(new BinaryData(bytes));

                foreach (var cloudEvent in cloudEvents)
                {
                    _logger.LogInformation("Received call event: {type}, callConnectionID: {connId}, serverCallId: {serverId}",
                       cloudEvent.Type,
                       cloudEvent.Subject,
                       cloudEvent.Id);
                    CallAutomationEventBase parsedEvent = CallAutomationEventParser.Parse(cloudEvent);
                    if (parsedEvent is CallConnected callConnected)
                    {
                        string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                        _logger.LogInformation($"Request body: {requestBody}");

                        var acsConnectionString = Environment.GetEnvironmentVariable("AcsConnectionString");
                        ArgumentNullException.ThrowIfNullOrEmpty(acsConnectionString);
                        var mp3Url = Environment.GetEnvironmentVariable("mp3Url");
                        ArgumentNullException.ThrowIfNullOrEmpty(mp3Url);
                        var useTTS = bool.Parse(Environment.GetEnvironmentVariable("UseTTS") ?? "true");
                        CallAutomationClient callAutomationClient = new CallAutomationClient(acsConnectionString);
                        var callConnection = callAutomationClient.GetCallConnection(parsedEvent.CallConnectionId);

                        _logger.LogInformation("CallConnectionId: {callConnectionId}", callConnection.CallConnectionId);
                        var callMedia = callConnection.GetCallMedia();
                        var operationContext = callConnected.OperationContext;
                        _logger.LogInformation("OperationContext: {operationContext}", operationContext);

                        // Check if operationContext is not null or empty before deserializing
                        if (!string.IsNullOrEmpty(operationContext))
                        {
                            var ticket = JsonSerializer.Deserialize<Ticket>(operationContext);


                            //concatenate the text to play
                            String textToPlay = "Dear " + ticket.Recipient + ". You have a ticket with details " + ticket.Description
                              + ". logged on" + ticket.Date;


                            PlaySource playSource = useTTS
                                ? new TextSource(textToPlay, "en-US", VoiceKind.Female)
                                : new FileSource(new Uri(mp3Url));


                            // technically this is just one user.. 
                            var playResponse = await callMedia.PlayToAllAsync(playSource);
                            return new OkObjectResult(playResponse.GetRawResponse().ToString());
                        }
                        else
                        {
                            _logger.LogWarning("OperationContext is null or empty.");
                        }
                    }
                }
                return new OkObjectResult("CloudEvent processed successfully!");
            }
        }
    }
}
