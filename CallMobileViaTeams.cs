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
        public async Task<IActionResult> RunAsync([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequest req)
        {
            //remeer appregistration in azure portal
            //bot registration https://dev.botframework.com/bots/new 
            _logger.LogInformation("C# HTTP trigger function processed a request.");
            var clientId = Environment.GetEnvironmentVariable("clientId");
            ArgumentNullException.ThrowIfNullOrEmpty(clientId);

            var tenantId = Environment.GetEnvironmentVariable("tenantId");
            ArgumentNullException.ThrowIfNullOrEmpty(tenantId);

            var clientSecret = Environment.GetEnvironmentVariable("clientSecret");
            ArgumentNullException.ThrowIfNullOrEmpty(clientSecret);

            var mp3Url = Environment.GetEnvironmentVariable("mp3Url");
            ArgumentNullException.ThrowIfNullOrEmpty(mp3Url);

        

            var applicationId = Environment.GetEnvironmentVariable("applicationId");
            ArgumentNullException.ThrowIfNullOrEmpty(applicationId);

            var applicationDisplayName = Environment.GetEnvironmentVariable("applicationDisplayName");
            ArgumentNullException.ThrowIfNullOrEmpty(applicationDisplayName);

            var TeamscallbackUriHost = Environment.GetEnvironmentVariable("TeamscallbackUriHost");
            ArgumentNullException.ThrowIfNullOrEmpty(TeamscallbackUriHost);

            var cognitiveKey = Environment.GetEnvironmentVariable("cognitiveKey");
            ArgumentNullException.ThrowIfNullOrEmpty(cognitiveKey);

            var cognitiveRegion = Environment.GetEnvironmentVariable("cognitiveRegion");
            ArgumentNullException.ThrowIfNullOrEmpty(cognitiveRegion);
            var blobConnectionString = Environment.GetEnvironmentVariable("blobConnectionString");
            ArgumentNullException.ThrowIfNullOrEmpty(blobConnectionString);
            var blobContainerName = Environment.GetEnvironmentVariable("blobContainerName");
            ArgumentNullException.ThrowIfNullOrEmpty(blobContainerName);

            var ticket = await JsonSerializer.DeserializeAsync<Ticket>(req.Body);

            var userId = ticket.UserID;
            //var telephoneNumber = "+27836258489";
            //var telephoneNumber = "+27838053885";

            var ttsHelper = new TTSHelper(cognitiveKey,cognitiveRegion , blobConnectionString, blobContainerName);

            // generate a unique name for the audio file
            var audioFileName = $"{Guid.NewGuid()}.wav";
            string sasUrl = await ttsHelper.ConvertToSpeechAsync(ticket, audioFileName);
            Console.WriteLine($"SAS URL: {sasUrl}");
            mp3Url = sasUrl;

            var telephoneNumber = userId;

            var requestBody = new Call
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
                    "applicationInstance" , new Identity
                    {
                        OdataType = "#microsoft.graph.identity",
                        DisplayName = applicationDisplayName,
                        
                        Id = applicationId,
                        AdditionalData = new Dictionary<string, object>
                        {
                            { "tenantId", tenantId },
                         
                        }
                    }
                },
            }
            },
                    CountryCode = null,
                    EndpointType = null,
                    Region = null,
                    LanguageId = null,
                   
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
                        "phone" , new Identity
                        {
                            OdataType = "#microsoft.graph.identity",
                            Id = telephoneNumber,
                        }
                    }
                  
                        }
            },

        },
    },
                RequestedModalities = new List<Modality?>
    {
        Modality.Audio,
    },
                CallOptions = new OutgoingCallOptions
                {
                    OdataType = "#microsoft.graph.outgoingCallOptions",
                    IsContentSharingNotificationEnabled = true,
                    IsDeltaRosterEnabled = true,
                },
                MediaConfig = new ServiceHostedMediaConfig
                {
                    OdataType = "#microsoft.graph.serviceHostedMediaConfig",
                    PreFetchMedia = new List<MediaInfo>
                    {
                        new MediaInfo
                        {
                            Uri = mp3Url, // URL to your audio file
                            ResourceId = Guid.NewGuid().ToString()
                        }
                    }
                },

                TenantId = tenantId 
            };

            var scopes = new[]
     {
    "https://graph.microsoft.com/.default"
};



            // using Azure.Identity;  
            //var options = new TokenCredentialOptions
            //{
            //    AuthorityHost = AzureAuthorityHosts.AzurePublicCloud
            //};

            //// https://learn.microsoft.com/dotnet/api/azure.identity.clientsecretcredential  
            //var clientSecretCredential = new ClientSecretCredential(
            //    tenantId, clientId, clientSecret, options);

            //// get accessToken          
            //var accessToken = await clientSecretCredential.GetTokenAsync(new Azure.Core.TokenRequestContext(scopes) { });

            //Console.WriteLine(accessToken.Token);

            //var graphClient = new GraphServiceClient(clientSecretCredential, scopes);

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

      
            var graphClient = new GraphServiceClient(authProvider);


            var result = await graphClient.Communications.Calls.PostAsync(requestBody);
            //var result = await graphClient.Communications.Calls["{call-id}"].PlayPrompt.PostAsync(requestBody);

            _logger.LogInformation("about to make call to: " + telephoneNumber);
            // wait until result is established
            while (result.State != Microsoft.Graph.Models.CallState.Established)
            {
                await Task.Delay(1000);
                result = await graphClient.Communications.Calls[result.Id].GetAsync();
            }
            _logger.LogInformation("call establised to: " + telephoneNumber);
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

            var subscribeToTonerequestBody = new SubscribeToTonePostRequestBody
            {
                ClientContext = Guid.NewGuid().ToString(),
            };
            var resultTone = await graphClient.Communications.Calls[result.Id].SubscribeToTone.PostAsync(subscribeToTonerequestBody);


            return new OkObjectResult("call made and audio played to " + telephoneNumber  );
        }
    }

    }

