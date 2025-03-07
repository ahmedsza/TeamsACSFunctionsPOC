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

namespace TeamsACSFunctions
{
    public class CallViaTeamsAPI
    {
        private readonly ILogger<CallViaTeamsAPI> _logger;

        public CallViaTeamsAPI(ILogger<CallViaTeamsAPI> logger)
        {
            _logger = logger;
        }

        [Function("CallViaTeamsAPI")]
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


            var TeamscallbackUriHost = Environment.GetEnvironmentVariable("TeamscallbackUriHost");
            ArgumentNullException.ThrowIfNullOrEmpty(TeamscallbackUriHost);

            var ticket = await JsonSerializer.DeserializeAsync<Ticket>(req.Body);
           
            var userId = ticket.UserID;

            var requestBody = new Call
            {
                OdataType = "#microsoft.graph.call",
                CallbackUri = TeamscallbackUriHost,
                Targets = new List<InvitationParticipantInfo>
    {
        new InvitationParticipantInfo
        {
            OdataType = "#microsoft.graph.invitationParticipantInfo",
            Identity = new IdentitySet
            {
                OdataType = "#microsoft.graph.identitySet",
                User = new Identity
                {
                    Id= userId,
                    // Id = "3c41663b-91ae-4b62-be41-2d9be7459772",
                },

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

                TenantId = tenantId // "91b8f903-6afd-49d6-aefa-564f634b2cb3"
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
            var graphClient = new GraphServiceClient(authProvider);

            var result = await graphClient.Communications.Calls.PostAsync(requestBody);
            //var result = await graphClient.Communications.Calls["{call-id}"].PlayPrompt.PostAsync(requestBody);

            // wait until result is established
            while (result.State != Microsoft.Graph.Models.CallState.Established)
            {
                await Task.Delay(1000);
                result = await graphClient.Communications.Calls[result.Id].GetAsync();
            }

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

            return new OkObjectResult("Welcome to Azure Functions!");
        }
    }

    public class ClientCredentialProvider : IAuthenticationProvider
    {
        private IConfidentialClientApplication _clientApplication;

        public ClientCredentialProvider(IConfidentialClientApplication clientApplication)
        {
            _clientApplication = clientApplication;
        }

        public async Task AuthenticateRequestAsync(RequestInformation request, Dictionary<string, object>? additionalAuthenticationContext, CancellationToken cancellationToken)
        {
            var result = await _clientApplication.AcquireTokenForClient(new[] { "https://graph.microsoft.com/.default" }).ExecuteAsync(cancellationToken);
            request.Headers.Add("Authorization", $"Bearer {result.AccessToken}");
        }
    }
}
