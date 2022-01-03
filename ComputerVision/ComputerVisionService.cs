using Microsoft.Azure.CognitiveServices.Vision.ComputerVision;
using Microsoft.Azure.CognitiveServices.Vision.ComputerVision.Models;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Text;
using MyComicsManagerApi.Models;
using MyComicsManagerApi.Utils;
using Serilog;

namespace MyComicsManagerApi.ComputerVision
{
    public class ComputerVisionService
    {
        private static ILogger Log => Serilog.Log.ForContext<ComputerVisionService>();
        private readonly IAzureSettings _azureSettings;

        public ComputerVisionService(IAzureSettings azureSettings)
        {
            _azureSettings = azureSettings;
        }

        public ComputerVisionClient Authenticate(string endpoint, string key)
        {
            ComputerVisionClient client =
              new ComputerVisionClient(new ApiKeyServiceClientCredentials(key))
              { Endpoint = endpoint };
            return client;
        }
        
        public async Task<string> ReadFileLocal(ComputerVisionClient client, string localFile)
        {
            Log.Here().Debug("READ FILE FROM LOCAL");

            // Read text from URL
            var textHeaders = await client.ReadInStreamAsync(File.OpenRead(localFile));
            // After the request, get the operation location (operation ID)
            string operationLocation = textHeaders.OperationLocation;

            // Retrieve the URI where the recognized text will be stored from the Operation-Location header.
            // We only need the ID and not the full URL
            const int numberOfCharsInOperationId = 36;
            string operationId = operationLocation.Substring(operationLocation.Length - numberOfCharsInOperationId);

            // Extract the text
            ReadOperationResult results;
            Log.Here().Debug($"Reading text from local file {Path.GetFileName(localFile)}...");

            do
            {
                results = await client.GetReadResultAsync(Guid.Parse(operationId));
            }
            while ((results.Status == OperationStatusCodes.Running || results.Status == OperationStatusCodes.NotStarted));

            // Display the found text.
            StringBuilder sb = new StringBuilder();
            var textUrlFileResults = results.AnalyzeResult.ReadResults;
            foreach (ReadResult page in textUrlFileResults)
            {
                foreach (Line line in page.Lines)
                {
                    sb.Append(line.Text);
                }
            }
            return sb.ToString();

        }

        public async Task<string> ReadTextFromLocalImage(string imagePath)
        {         
            ComputerVisionClient client = Authenticate(_azureSettings.Endpoint, _azureSettings.Key);
            return await ReadFileLocal(client, imagePath).ConfigureAwait(false);
        }
    }
}