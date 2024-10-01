using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Azure.Storage.Blobs;
using Microsoft.Azure.Functions.Worker.Http;
using System.Net;
using System.Threading.Tasks;
using System;
using Azure.Storage.Blobs.Models;
using Azure;

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


            try
            {
                // Get a BlobContainerClient for the specific container (replace "products" with the actual container name)
                var containerClient = _blobServiceClient.GetBlobContainerClient("abc-retail-part-one-container");

                // Create the blob container if it doesn't already exist
                await containerClient.CreateIfNotExistsAsync();

                // Retrieve the original filename from the headers
                if (!req.Headers.TryGetValues("file-name", out var fileNameValues))
                {
                    throw new Exception("File name is missing in the request headers.");
                }

                string originalFileName = fileNameValues.FirstOrDefault();
                if (string.IsNullOrEmpty(originalFileName))
                {
                    throw new Exception("Invalid file name.");
                }

                // Get a BlobClient for the specific blob using the original filename
                var blobClient = containerClient.GetBlobClient(originalFileName);

                // Upload the HTTP request body stream to the blob storage, overwriting any existing blob with the same name
                using (var stream = req.Body)
                {
                    await blobClient.UploadAsync(stream, overwrite: true);
                }

                // Create a success response
                var response = req.CreateResponse(HttpStatusCode.OK);
                await response.WriteStringAsync($"Blob '{originalFileName}' uploaded successfully.");
                return response;
            }
            catch (Exception ex)
            {
                logger.LogError($"Error uploading to Blob Storage: {ex.Message}");

                // Create an error response
                var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
                await errorResponse.WriteStringAsync("Failed to upload blob.");
                return errorResponse;
            }
        }

        // Function that handles HTTP DELETE requests to remove a blob from Blob Storage
        [Function("DeleteBlob")]
        public async Task<HttpResponseData> DeleteBlobAsync(
            [HttpTrigger(AuthorizationLevel.Function, "delete")] HttpRequestData req,
            FunctionContext executionContext)
        {
            // Get a Logger instance to log information about the function's execution
            var logger = executionContext.GetLogger("DeleteBlob");
            logger.LogInformation("Deleting from Blob Storage...");

            try
            {
                // Extract the Blob URI from the query string
                var query = System.Web.HttpUtility.ParseQueryString(req.Url.Query);
                string blobUri = query["blobUri"];

                if (string.IsNullOrEmpty(blobUri))
                {
                    throw new Exception("Blob URI is missing from the query string.");
                }

                // Parse the URI to extract the blob name
                Uri uri = new Uri(blobUri);
                string blobName = uri.Segments[^1]; // Get the blob name from the URI

                // Get a BlobContainerClient for the specific container (replace "products" with your actual container name)
                var containerClient = _blobServiceClient.GetBlobContainerClient("abc-retail-part-one-container");
                var blobClient = containerClient.GetBlobClient(blobName);

                // Delete the blob if it exists (including snapshots)
                await blobClient.DeleteIfExistsAsync(DeleteSnapshotsOption.IncludeSnapshots);

                // Create a success response
                var response = req.CreateResponse(HttpStatusCode.OK);
                await response.WriteStringAsync($"Blob '{blobName}' deleted successfully.");
                return response;
            }
            catch (Exception ex)
            {
                logger.LogError($"Error deleting blob from Blob Storage: {ex.Message}");

                // Create an error response
                var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
                await errorResponse.WriteStringAsync("Failed to delete blob.");
                return errorResponse;
            }
        }
    }
}

