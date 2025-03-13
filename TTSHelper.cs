using System;
using System.IO;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using Azure.Storage.Sas;
using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;
using Microsoft.Graph.Models;

namespace TeamsACSFunctions
{
    internal class TTSHelper
    {
        private readonly string _subscriptionKey;
        private readonly string _serviceRegion;
        private readonly string _blobConnectionString;
        private readonly string _blobContainerName;

        public TTSHelper(string subscriptionKey, string serviceRegion, string blobConnectionString, string blobContainerName)
        {
            _subscriptionKey = subscriptionKey;
            _serviceRegion = serviceRegion;
            _blobConnectionString = blobConnectionString;
            _blobContainerName = blobContainerName;
        }

        public async Task<string> ConvertToSpeechAsync(Ticket ticket, string blobName)
        {
            var config = SpeechConfig.FromSubscription(_subscriptionKey, _serviceRegion);
            config.SpeechSynthesisVoiceName = "en-US-JennyNeural"; // You can choose a different voice

            var textToSynthesize = $"Dear {ticket.Recipient}, you have a ticket with details {ticket.Description} logged on {ticket.Date}.";

            //using var audioConfig = AudioConfig.FromWavFileOutput(blobName);
            //using var speechSynthesizer = new SpeechSynthesizer(config, audioConfig);
            //await speechSynthesizer.SpeakTextAsync("Thank you. Confirmation recieved");

            using var synthesizer = new SpeechSynthesizer(config, null);


            var result = await synthesizer.SpeakTextAsync(textToSynthesize);
            if (result.Reason == ResultReason.SynthesizingAudioCompleted)
            {
                Console.WriteLine("Speech synthesized successfully.");
                
                // await UploadAudioToStorage(result.AudioData);
                // save the audio to local file
                // var filePath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.wav");
                // using (var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write))
                // {
                //     await fileStream.WriteAsync(result.AudioData, 0, result.AudioData.Length);
                // }

                var sasUrl = await UploadToBlobStorageAsync(result.AudioData, blobName);
                return sasUrl;
            }
            else
            {
                Console.WriteLine($"Failed to synthesize speech: {result.Reason}");
            }

                        return "an error occurred";
        }

        
        private async Task<string> UploadToBlobStorageAsync(byte[] audioData, string blobName)
        {
            var blobServiceClient = new BlobServiceClient(_blobConnectionString);
            var blobContainerClient = blobServiceClient.GetBlobContainerClient(_blobContainerName);
            var blobClient = blobContainerClient.GetBlobClient(blobName);
            using var stream = new MemoryStream(audioData);
            await blobClient.UploadAsync(stream, true);
            Console.WriteLine("Audio uploaded to Azure Blob Storage.");
            return GenerateSasUri(blobClient);
        }

        private string GenerateSasUri(BlobClient blobClient)
        {
            if (blobClient.CanGenerateSasUri)
            {
                var sasBuilder = new BlobSasBuilder
                {
                    BlobContainerName = blobClient.BlobContainerName,
                    BlobName = blobClient.Name,
                    Resource = "b",
                    ExpiresOn = DateTimeOffset.UtcNow.AddHours(1) // Set the expiry time as needed
                };

                sasBuilder.SetPermissions(BlobSasPermissions.Read);

                Uri sasUri = blobClient.GenerateSasUri(sasBuilder);
                return sasUri.ToString();
            }
            else
            {
                throw new InvalidOperationException("BlobClient cannot generate SAS URI. Ensure the client is authenticated with Shared Key credentials.");
            }
        }
    }
}

