using Microsoft.Azure.CognitiveServices.Vision.ComputerVision;
using Microsoft.Azure.CognitiveServices.Vision.ComputerVision.Models;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Serilog;
using System.Text;
using MyComicsManagerApi.Models;

namespace MyComicsManagerApi.ComputerVision
{
    public class ComputerVisionService
    {
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

        public async Task<string> ReadFileUrl(ComputerVisionClient client, string urlFile)
        {
            
            Log.Debug("READ FILE FROM URL");

            // Read text from URL
            var textHeaders = await client.ReadAsync(urlFile);
            // After the request, get the operation location (operation ID)
            string operationLocation = textHeaders.OperationLocation;
            Thread.Sleep(2000);
 
            // Retrieve the URI where the extracted text will be stored from the Operation-Location header.
            // We only need the ID and not the full URL
            const int numberOfCharsInOperationId = 36;
            string operationId = operationLocation.Substring(operationLocation.Length - numberOfCharsInOperationId);

            // Extract the text
            ReadOperationResult results;
            Log.Debug($"Extracting text from URL file {Path.GetFileName(urlFile)}...");

            do
            {
                results = await client.GetReadResultAsync(Guid.Parse(operationId));
            }
            while ((results.Status == OperationStatusCodes.Running ||
                results.Status == OperationStatusCodes.NotStarted));
            
            // Display the found text.            
            var textUrlFileResults = results.AnalyzeResult.ReadResults;

            StringBuilder sb = new StringBuilder();
            foreach (ReadResult page in textUrlFileResults)
            {
                foreach (Line line in page.Lines)
                {
                    sb.Append(line.Text);
                }
            }
            return sb.ToString();
        }

        public async Task<string> ReadFileLocal(ComputerVisionClient client, string localFile)
        {
            Log.Debug("READ FILE FROM LOCAL");

            // Read text from URL
            var textHeaders = await client.ReadInStreamAsync(File.OpenRead(localFile));
            // After the request, get the operation location (operation ID)
            string operationLocation = textHeaders.OperationLocation;
            Thread.Sleep(2000);

            // Retrieve the URI where the recognized text will be stored from the Operation-Location header.
            // We only need the ID and not the full URL
            const int numberOfCharsInOperationId = 36;
            string operationId = operationLocation.Substring(operationLocation.Length - numberOfCharsInOperationId);

            // Extract the text
            ReadOperationResult results;
            Log.Debug($"Reading text from local file {Path.GetFileName(localFile)}...");

            do
            {
                results = await client.GetReadResultAsync(Guid.Parse(operationId));
            }
            while ((results.Status == OperationStatusCodes.Running ||
                results.Status == OperationStatusCodes.NotStarted));

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