using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using System.Runtime.CompilerServices;
using System.Security.Principal;
using System.Text.Json;
using Azure.Identity;
using Microsoft.Graph;
using Microsoft.Graph.Communications.Calls.Item.PlayPrompt;
using Microsoft.Graph.Models;
using Microsoft.Identity.Client;
using Prompt = Microsoft.Graph.Models.Prompt;
using Microsoft.Kiota.Abstractions.Authentication;
using System.Net.Http.Headers;
using Microsoft.Kiota.Abstractions;
using Microsoft.Graph.Communications.Calls.Item.SubscribeToTone;


namespace TeamsACSFunctions
{
    public class CallMobileViaTeams
    {
        private readonly ILogger<CallMobileViaTeams> _logger;

        public CallMobileViaTeams(ILogger<CallMobileViaTeams> logger)
        {
            _logger = logger;
        }

        [Function("CallMobileViaTeams")]
        private async Task<Dictionary<string, string>> GetEnvironmentVariablesAsync()
        {
            var variables = new Dictionary<string, string>
            {
                { "clientId", Environment.GetEnvironmentVariable("clientId") },
                { "tenantId", Environment.GetEnvironmentVariable("tenantId") },
                { "clientSecret", Environment.GetEnvironmentVariable("clientSecret") },
                { "mp3Url", Environment.GetEnvironmentVariable("mp3Url") },
                { "applicationId", Environment.GetEnvironmentVariable("applicationId") },
                { "applicationDisplayName", Environment.GetEnvironmentVariable("applicationDisplayName") },
                { "TeamscallbackUriHost", Environment.GetEnvironmentVariable("TeamscallbackUriHost") },
                { "cognitiveKey", Environment.GetEnvironmentVariable("cognitiveKey") },
                { "cognitiveRegion", Environment.GetEnvironmentVariable("cognitiveRegion") },
                { "blobConnectionString", Environment.GetEnvironmentVariable("blobConnectionString") },
                { "blobContainerName", Environment.GetEnvironmentVariable("blobContainerName") }
            };

            foreach (var variable in variables)
            {
                ArgumentNullException.ThrowIfNullOrEmpty(variable.Value, variable.Key);
            }

            return variables;
        }

        private async Task<string> GenerateAudioFileAsync(Ticket ticket, string cognitiveKey, string cognitiveRegion, string blobConnectionString, string blobContainerName)
        {
            var ttsHelper = new TTSHelper(cognitiveKey, cognitiveRegion, blobConnectionString, blobContainerName);
            var audioFileName = $"{Guid.NewGuid()}.wav";
            return await ttsHelper.ConvertToSpeechAsync(ticket, audioFileName);
        }

        private Call CreateCallRequestBody(string applicationId, string applicationDisplayName, string tenantId, string TeamscallbackUriHost, string telephoneNumber, string mp3Url)
        {
            return new Call
            {
                OdataType = "#microsoft.graph.call",
                CallbackUri = TeamscallbackUriHost,
                Source = new ParticipantInfo
                {
                    OdataType = "#microsoft.graph.participantInfo",
                    Identity = new IdentitySet
                    {
                        OdataType = "#microsoft.graph.identitySet",
                        AdditionalData = new Dictionary<string, object>
                        {
                            {
                                "applicationInstance", new Identity
                                {
                                    OdataType = "#microsoft.graph.identity",
                                    DisplayName = applicationDisplayName,
                                    Id = applicationId,
                                    AdditionalData = new Dictionary<string, object>
                                    {
                                        { "tenantId", tenantId }
                                    }
                                }
                            }
                        }
                    }
                },
                Targets = new List<InvitationParticipantInfo>
                {
                    new InvitationParticipantInfo
                    {
                        OdataType = "#microsoft.graph.invitationParticipantInfo",
                        Identity = new IdentitySet
                        {
                            OdataType = "#microsoft.graph.identitySet",
                            AdditionalData = new Dictionary<string, object>
                            {
                                {
                                    "phone", new Identity
                                    {
                                        OdataType = "#microsoft.graph.identity",
                                        Id = telephoneNumber
                                    }
                                }
                            }
                        }
                    }
                },
                RequestedModalities = new List<Modality?> { Modality.Audio },
                CallOptions = new OutgoingCallOptions
                {
                    OdataType = "#microsoft.graph.outgoingCallOptions",
                    IsContentSharingNotificationEnabled = true,
                    IsDeltaRosterEnabled = true
                },
                MediaConfig = new ServiceHostedMediaConfig
                {
                    OdataType = "#microsoft.graph.serviceHostedMediaConfig",
                    PreFetchMedia = new List<MediaInfo>
                    {
                        new MediaInfo
                        {
                            Uri = mp3Url,
                            ResourceId = Guid.NewGuid().ToString()
                        }
                    }
                },
                TenantId = tenantId
            };
        }

        private async Task<GraphServiceClient> GetGraphClientAsync(string clientId, string clientSecret, string tenantId)
        {
            var clientApplication = ConfidentialClientApplicationBuilder
                .Create(clientId)
                .WithClientSecret(clientSecret)
                .WithAuthority(new Uri($"https://login.microsoftonline.com/{tenantId}/v2.0"))
                .Build();

            var authProvider = new ClientCredentialProvider(clientApplication);
            return new GraphServiceClient(authProvider);
        }

        private async Task WaitForCallEstablishmentAsync(GraphServiceClient graphClient, Call result)
        {
            while (result.State != Microsoft.Graph.Models.CallState.Established)
            {
                await Task.Delay(1000);
                result = await graphClient.Communications.Calls[result.Id].GetAsync();
            }
        }

        [Function("CallMobileViaTeams")]
        public async Task<IActionResult> RunAsync([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequest req)
        {
            _logger.LogInformation("C# HTTP trigger function processed a request.");

            var envVariables = await GetEnvironmentVariablesAsync();
            var ticket = await JsonSerializer.DeserializeAsync<Ticket>(req.Body);
            var telephoneNumber = ticket.UserID;

            var mp3Url = await GenerateAudioFileAsync(ticket, envVariables["cognitiveKey"], envVariables["cognitiveRegion"], envVariables["blobConnectionString"], envVariables["blobContainerName"]);
            var requestBody = CreateCallRequestBody(envVariables["applicationId"], envVariables["applicationDisplayName"], envVariables["tenantId"], envVariables["TeamscallbackUriHost"], telephoneNumber, mp3Url);

            var graphClient = await GetGraphClientAsync(envVariables["clientId"], envVariables["clientSecret"], envVariables["tenantId"]);
            var result = await graphClient.Communications.Calls.PostAsync(requestBody);

            _logger.LogInformation("about to make call to: " + telephoneNumber);
            await WaitForCallEstablishmentAsync(graphClient, result);
            _logger.LogInformation("call established to: " + telephoneNumber);
            await PlayAudioPromptAsync(mp3Url, graphClient, result);

            var resultSubscribe = await SubscribeToToneAsync(graphClient, result);

            return new OkObjectResult("call made and audio played to " + telephoneNumber);



        }

        static async Task<SubscribeToToneOperation?> SubscribeToToneAsync(GraphServiceClient graphClient, Call? result)
        {
            var subscribeToToneRequestBody = new SubscribeToTonePostRequestBody
            {
                ClientContext = Guid.NewGuid().ToString()
            };
            var resultTone = await graphClient.Communications.Calls[result.Id].SubscribeToTone.PostAsync(subscribeToToneRequestBody);
            return resultTone;
        }

        static async Task PlayAudioPromptAsync(string mp3Url, GraphServiceClient graphClient, Call? result)
        {
            var playPromptOperation = await graphClient.Communications.Calls[result.Id].PlayPrompt.PostAsync(new PlayPromptPostRequestBody
            {
                Prompts = new List<Prompt>
                {
                    new MediaPrompt
                    {
                        MediaInfo = new MediaInfo
                        {
                            Uri = mp3Url,
                            ResourceId = Guid.NewGuid().ToString()
                        }
                    }
                }
            });
        }
    }

    }

