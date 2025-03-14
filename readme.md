# High Level Description 
This sample demonstrates how to make a call and play audio in 2 different way
- Azure Communication Services (ACS) using Call Automation SDK
- Microsoft Teams using the Rest API
- Microsoft Teams making a call to a phone number 

This is purely sample code used in a proof of concept 

**Note that most of below was generated using Github Copilot.**

## Pre-requisites
Note when running locally, devtunnels was used to expose the endpoints to the internet


### Using ACS
- If you are using the ACS API, you will need to have the following:
  - Azure Communication Services account
  - Azure Communication Services connection string
  - Cognitive Services account
  - Cognitive Services endpoint
  - Cognitive Services subscription key
  - MP3 file to be played during calls
  - The users need to have the correct Teams license 
  - IMPORTANT - enterprise voice license is required for the user to be able to receive calls



### Using Teams API
	
- If you are using the Teams API, you will need to have the following:
	- Entra (aka Azure AD) application registation with the following permissions:
	  - `Team.ManageCalls`
	  - `Teams.ManageChats` (need to review if this is required)
	- The above application registration should have a client secret
	- The above application registration should have a redirect URI
	- Admin consent should be granted for the above permissions
	- IMPORTANT - The above application registration should be registered as a bot at https://dev.botframework.com/bots/new
		- Make sure calling permissions are enabled
		- Setup Teams as a channel
		
## Configuration

The `local.settings.json` file contains the following configuration values:

- **AzureWebJobsStorage**: Connection string for Azure WebJobs storage.
- **FUNCTIONS_WORKER_RUNTIME**: Specifies the runtime for Azure Functions.
- **AcsConnectionString**: Connection string for Azure Communication Services.
- **mp3Url**: URL of the MP3 file to be played during calls.
- **callbackUriHost**: Callback URI host for handling call events.
- **TeamscallbackUriHost**: Callback URI host specific to Teams API interactions.
- **ClientId**: Azure AD client ID for authenticating with Microsoft Graph.
- **TenantId**: Azure AD tenant ID for authenticating with Microsoft Graph.
- **ClientSecret**: Azure AD client secret for authenticating with Microsoft Graph.
- **UserId**: User ID for the recipient of the call.
- **cognitiveServicesEndpoint**: Endpoint for Cognitive Services.
- **UseTTS**: Boolean flag to indicate whether to use text-to-speech for call messages. If using, then Cognitive Services values must be set.
- **applicationId**: Application ID for the bot instance.
- **applicationDisplayName**: Display name for the bot instance.
- **cognitiveKey**: Subscription key for Cognitive Services.
- **cognitiveRegion**: Region for Cognitive Services.
- **blobConnectionString**: Connection string for Azure Blob Storage.
- **blobContainerName**: Name of the blob container for storing WAV files.
- **ThanksmediaUri**: URL of the media file to be played as a thank you message.

## Making Calls
The solution should preferably be deployed to Azure Functions. The following endpoints are available for making calls:
https://YOURURL/api/CallUserACS
https://YOURURL/api/CallViaTeamsAPI

- Example payload for making a call via ACS. 
```http
POST https://YOURURL/api/CallUserACS
Content-Type: application/json
Accept-Language: en-US,en;q=0.5

{
  "Description": "this is a new test of a ticket. Please action this asap. please press one to confirm",
  "Date": "2023-10-01",
  "UserID": "a10911ae-e159-455a-971c-efc966948bc4",
  "Recipient": "John test"
}
```

- Example payload for making a call via Teams. 
```http
POST https://YOURURL/api/CallUserTeamsAPI
Content-Type: application/json
Accept-Language: en-US,en;q=0.5

{
  "Description": "this is a new test of a ticket. Please action this asap. please press one to confirm",
  "Date": "2023-10-01",
  "UserID": "a10911ae-e159-455a-971c-efc966948bc4",
  "Recipient": "John test"
}
```




## Project Structure

### Files

- `local.settings.json`: Contains configuration settings and environment variables for the Azure Functions.
- `teamcallback.cs`: Defines an Azure Function that processes and logs Teams notifications.
- `callback.cs`: Defines an Azure Function that handles and responds to ACS call events.
- `CallUserACS.cs`: Implements an Azure Function to initiate calls to users via Azure Communication Services.
- `CallViaTeamsAPI.cs`: Implements an Azure Function to initiate calls via the Microsoft Teams API.
- `Ticket.cs`: Defines models for ticket details and communication payloads.



### Functions

#### teamcallback

This function handles HTTP POST requests and logs Teams notifications. It is defined in `teamcallback.cs`.

- **Endpoint**: `/api/teamcallback`
- **HTTP Methods**: POST
- **Request Body**: `CommsNotificationsPayload`
- **Response**: Logs the notification details and returns a welcome message.

#### callback

This function processes call events from ACS, logs the events, and plays a message to the call participants. It is defined in `callback.cs`.

- **Endpoint**: `/api/callback`
- **HTTP Methods**: GET, POST
- **Request Body**: CloudEvent payload
- **Response**: Logs the event details, retrieves the call connection, and plays a message to the call participants.


### Dependencies

- **Azure.Communication**: Azure Communication Services SDK.
- **Azure.Communication.CallAutomation**: Azure Communication Services Call Automation SDK.
- **Azure.Messaging**: Azure Messaging SDK.
- **Microsoft.AspNetCore.Http**: ASP.NET Core HTTP abstractions.
- **Microsoft.AspNetCore.Mvc**: ASP.NET Core MVC framework.
- **Microsoft.Azure.Functions.Worker**: Azure Functions Worker SDK.
- **Microsoft.Extensions.Logging**: Logging abstractions.



## Getting Started

1. Clone the repository.
2. Open the solution in Visual Studio 2022.
3. Update the `local.settings.json` file with your configuration values.
4. Build and run the solution.

# CallUserACS.cs

## Overview

The `CallUserACS.cs` file defines an Azure Function that handles initiating a call to a user via Azure Communication Services (ACS). This function is implemented using .NET 8 and C# 12.0.

## Class: CallUserACS

### Purpose

The `CallUserACS` class is responsible for processing HTTP POST requests to initiate a call to a user. It uses the Azure Communication Services Call Automation SDK to create and manage the call.

### Constructor


- **logger**: An instance of `ILogger<CallUserACS>` used for logging information and errors.

### Method: RunAsync


- **req**: The HTTP request containing the ticket data.
- **Returns**: An `IActionResult` indicating the result of the operation.

#### Functionality

1. **Retrieve Environment Variables**:
   - `AcsConnectionString`: Connection string for Azure Communication Services.
   - `callbackUriHost`: Callback URI host for the function.
   - `cognitiveServicesEndpoint`: Endpoint for Cognitive Services.

2. **Log Request Processing**:
   - Logs that the HTTP trigger function has processed a request.

3. **Deserialize Request Body**:
   - Deserializes the request body into a `Ticket` object.

4. **Validate Ticket Data**:
   - Checks if the ticket data is valid and contains a `UserID`.

5. **Create Call Automation Client**:
   - Initializes a `CallAutomationClient` using the ACS connection string.

6. **Create Call Invite**:
   - Creates a `CallInvite` object for the user specified in the ticket.

7. **Create Call Options**:
   - Configures the call options, including the callback URI and cognitive services endpoint.

8. **Initiate Call**:
   - Initiates the call using the `CreateCallAsync` method of the `CallAutomationClient`.

9. **Return Response**:
   - Returns the raw response of the call initiation as an `OkObjectResult`.

### Dependencies

- **Azure.Communication.CallAutomation**: Azure Communication Services Call Automation SDK.
- **Azure.Communication**: Azure Communication Services SDK.
- **Microsoft.AspNetCore.Http**: ASP.NET Core HTTP abstractions.
- **Microsoft.AspNetCore.Mvc**: ASP.NET Core MVC framework.
- **Microsoft.Azure.Functions.Worker**: Azure Functions Worker SDK.
- **Microsoft.Extensions.Logging**: Logging abstractions.
- **System.Text.Json**: JSON serialization and deserialization.

## Example Usage

To use this function, send an HTTP POST request to the `/api/CallUserACS` endpoint with a JSON payload containing the ticket data. The function will process the request, initiate a call to the specified user, and return the result.


# CallViaTeamsAPI.cs

## Overview

The `CallViaTeamsAPI.cs` file defines an Azure Function that handles initiating a call to a user via the Microsoft Teams API. This function is implemented using .NET 8 and C# 12.0.

## Class: CallViaTeamsAPI

### Purpose

The `CallViaTeamsAPI` class is responsible for processing HTTP GET and POST requests to initiate a call to a user using the Microsoft Graph API. It uses the Microsoft Graph SDK to create and manage the call.

### Constructor


- **logger**: An instance of `ILogger<CallViaTeamsAPI>` used for logging information and errors.

### Method: RunAsync


- **req**: The HTTP request containing the ticket data.
- **Returns**: An `IActionResult` indicating the result of the operation.

#### Functionality

1. **Retrieve Environment Variables**:
   - `clientId`: Client ID for the Azure AD application.
   - `tenantId`: Tenant ID for the Azure AD application.
   - `clientSecret`: Client secret for the Azure AD application.
   - `mp3Url`: URL of the MP3 file to be played.
   - `TeamscallbackUriHost`: Callback URI host for the function.

2. **Log Request Processing**:
   - Logs that the HTTP trigger function has processed a request.

3. **Deserialize Request Body**:
   - Deserializes the request body into a `Ticket` object.

4. **Create Call Request Body**:
   - Constructs the request body for the call, including the target user, modalities, call options, and media configuration.

5. **Authenticate with Microsoft Graph**:
   - Uses the `ConfidentialClientApplicationBuilder` to create a client application and authenticate with Microsoft Graph.

6. **Initiate Call**:
   - Uses the `GraphServiceClient` to initiate the call by posting the request body to the Microsoft Graph API.

7. **Wait for Call Establishment**:
   - Polls the call state until it is established.

8. **Play Prompt**:
   - Uses the `GraphServiceClient` to play a prompt (audio file) to the call participants.

9. **Return Response**:
   - Returns a success message as an `OkObjectResult`.

### Dependencies

- **Microsoft.Graph**: Microsoft Graph SDK.
- **Azure.Identity**: Azure Identity SDK for authentication.
- **Microsoft.AspNetCore.Http**: ASP.NET Core HTTP abstractions.
- **Microsoft.AspNetCore.Mvc**: ASP.NET Core MVC framework.
- **Microsoft.Azure.Functions.Worker**: Azure Functions Worker SDK.
- **Microsoft.Extensions.Logging**: Logging abstractions.
- **System.Text.Json**: JSON serialization and deserialization.

## Example Usage

To use this function, send an HTTP GET or POST request to the `/api/CallViaTeamsAPI` endpoint with a JSON payload containing the ticket data. The function will process the request, initiate a call to the specified user, and play an audio prompt.


# callback.cs

## Overview

The `callback.cs` file defines an Azure Function that processes call events from Azure Communication Services (ACS), logs the events, and plays a message to the call participants. This function is implemented using .NET 8 and C# 12.0.

## Class: callback

### Purpose

The `callback` class is responsible for processing call events from ACS, logging the events, and playing a message to the call participants.

### Constructor


- **logger**: An instance of `ILogger<callback>` used for logging information and errors.

### Method: Run


- **req**: The HTTP request object.
- **Returns**: An `IActionResult` indicating the result of the function execution.

### Functionality

1. **Log Request Processing**:
   - Logs that the HTTP trigger function has processed a request.

2. **Deserialize Request Body**:
   - Reads the request body into a memory stream and deserializes it into a list of `CloudEvent` objects.

3. **Log Event Details**:
   - Logs the type, call connection ID, and server call ID of each event.

4. **Process CallConnected Event**:
   - If the event is a `CallConnected` event, logs the request body and retrieves the call connection using the ACS connection string.

5. **Log Call Connection Details**:
   - Logs the call connection ID and operation context.

6. **Play Message to Call Participants**:
   - If the operation context is not null or empty, deserializes it into a `Ticket` object and constructs a message to play to the call participants. Uses the `CallMedia` object to play the message.

7. **Return Response**:
   - Returns the raw response of the play operation or a message indicating the CloudEvent was processed successfully.

### Dependencies

- **Azure.Communication**: Azure Communication Services SDK.
- **Azure.Communication.CallAutomation**: Azure Communication Services Call Automation SDK.
- **Azure.Messaging**: Azure Messaging SDK.
- **Microsoft.AspNetCore.Http**: ASP.NET Core HTTP abstractions.
- **Microsoft.AspNetCore.Mvc**: ASP.NET Core MVC framework.
- **Microsoft.Azure.Functions.Worker**: Azure Functions Worker SDK.
- **Microsoft.Extensions.Logging**: Logging abstractions.
- **System.Text.Json**: JSON serialization and deserialization.

## Example Usage

To use this function, send an HTTP GET or POST request to the `/api/callback` endpoint with a CloudEvent payload. The function will process the request, log the event details, retrieve the call connection, and play a message to the call participants.

# teamcallback.cs

## Overview

The `teamcallback.cs` file defines an Azure Function that processes HTTP GET and POST requests to log Teams notifications. This function is implemented using .NET 8 and C# 12.0.

## Class: teamcallback

### Purpose

The `teamcallback` class is responsible for processing HTTP GET and POST requests to log Teams notifications.

### Constructor


- **logger**: An instance of `ILogger<teamcallback>` used for logging information and errors.

### Method: RunAsync


- **req**: The HTTP request object.
- **Returns**: An `IActionResult` indicating the result of the function execution.

### Functionality

1. **Log Request Processing**:
   - Logs that the HTTP trigger function has processed a request.

2. **Deserialize Request Body**:
   - Reads the request body and deserializes it into a `CommsNotificationsPayload` object.

3. **Log Notification Details**:
   - Logs the request body, the time the notification was received, the payload's `CallId`, and `CallState`.

4. **Return Response**:
   - Returns a message indicating the logger was called.

### Dependencies

- **Microsoft.AspNetCore.Http**: ASP.NET Core HTTP abstractions.
- **Microsoft.AspNetCore.Mvc**: ASP.NET Core MVC framework.
- **Microsoft.Azure.Functions.Worker**: Azure Functions Worker SDK.
- **Microsoft.Extensions.Logging**: Logging abstractions.
- **System.Text.Json**: JSON serialization and deserialization.

## Example Usage

To use this function, send an HTTP GET or POST request to the `/api/teamcallback` endpoint with a `CommsNotificationsPayload` payload. The function will process the request, log the notification details, and return a message indicating the logger was called.





