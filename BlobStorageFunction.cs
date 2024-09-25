using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Azure.Storage.Blobs;
using Microsoft.Azure.Functions.Worker.Http;
using System.Net;
using System.Threading.Tasks;

namespace BlobStorageFunction
{
    public class BlobStorageFunction
    {
        // Declare a private BlobServiceClient object to interact with Azure Blob Storage
        private readonly BlobServiceClient _blobServiceClient;

        public BlobStorageFunction()
        {
            // Get the Azure Storage connection string from environment variables
            string connectionString = Environment.GetEnvironmentVariable("AzureWebJobsStorage");

            // Initialize the BlobServiceClient using the connection string
            _blobServiceClient = new BlobServiceClient(connectionString);
        }

        // Function that handles HTTP POST requests to upload data to Blob Storage
        [Function("UploadToBlob")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequestData req,
            FunctionContext executionContext)
        {
            // Get a Logger instance to log information about the function's execution
            var logger = executionContext.GetLogger("UploadToBlob");
            logger.LogInformation("Uploading to Blob Storage...");

            // Get a BlobContainerClient for a specific container (replace "your-container-name" with actual container name)
            var containerClient = _blobServiceClient.GetBlobContainerClient("your-container-name");

            // Create the blob container if it doesn't already exist
            await containerClient.CreateIfNotExistsAsync();

            // Get a BlobClient for the specific blob (replace "your-blob-name" with actual blob name)
            var blobClient = containerClient.GetBlobClient("your-blob-name");

            // Upload the HTTP request body stream to the blob storage, overwriting any existing blob with the same name
            using (var stream = req.Body)
            {
                await blobClient.UploadAsync(stream, overwrite: true);
            }

            // Create a success response
            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteStringAsync("Blob uploaded successfully.");

            return response;
        }
    }
}
