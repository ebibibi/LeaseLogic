using LeaseLogic.Functions.Models;
using LeaseLogic.Functions.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Text.Json;

namespace LeaseLogic.Functions.Functions;

public class UploadFunctions
{
    private readonly IStorageService _storageService;
    private readonly ILogger<UploadFunctions> _logger;

    public UploadFunctions(IStorageService storageService, ILogger<UploadFunctions> logger)
    {
        _storageService = storageService;
        _logger = logger;
    }

    [Function("UploadUrl")]
    public async Task<HttpResponseData> GenerateUploadUrl(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "upload-url")] HttpRequestData req)
    {
        try
        {
            // Read request body
            var requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            var uploadRequest = JsonSerializer.Deserialize<UploadUrlRequest>(requestBody, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            if (uploadRequest == null)
            {
                _logger.LogWarning("Invalid request body for upload URL generation");
                var badRequestResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await badRequestResponse.WriteStringAsync("Invalid request body");
                return badRequestResponse;
            }

            // Validate request
            if (string.IsNullOrEmpty(uploadRequest.FileName) || 
                uploadRequest.FileSize <= 0 || 
                string.IsNullOrEmpty(uploadRequest.ContentType))
            {
                _logger.LogWarning("Invalid upload request parameters: {FileName}, {FileSize}, {ContentType}", 
                    uploadRequest.FileName, uploadRequest.FileSize, uploadRequest.ContentType);
                
                var badRequestResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await badRequestResponse.WriteStringAsync("Invalid parameters");
                return badRequestResponse;
            }

            // Check file size limit (50MB)
            if (uploadRequest.FileSize > 52428800)
            {
                _logger.LogWarning("File size exceeds limit: {FileSize} bytes", uploadRequest.FileSize);
                var badRequestResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await badRequestResponse.WriteStringAsync("File size exceeds 50MB limit");
                return badRequestResponse;
            }

            // Validate content type
            var allowedContentTypes = new[] 
            { 
                "application/pdf", 
                "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                "application/msword",
                "text/plain"
            };

            if (!allowedContentTypes.Contains(uploadRequest.ContentType.ToLowerInvariant()))
            {
                _logger.LogWarning("Unsupported content type: {ContentType}", uploadRequest.ContentType);
                var badRequestResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await badRequestResponse.WriteStringAsync("Unsupported file type. Only PDF, Word, and Text files are allowed.");
                return badRequestResponse;
            }

            // Generate upload URL
            var uploadResponse = await _storageService.GenerateUploadUrlAsync(uploadRequest);

            _logger.LogInformation("Generated upload URL for file: {FileName} (ID: {FileId})", 
                uploadRequest.FileName, uploadResponse.FileId);

            // Create successful response
            var response = req.CreateResponse(HttpStatusCode.OK);
            response.Headers.Add("Content-Type", "application/json");

            var json = JsonSerializer.Serialize(uploadResponse, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            await response.WriteStringAsync(json);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating upload URL");

            var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
            await errorResponse.WriteStringAsync("Internal server error");
            return errorResponse;
        }
    }

    [Function("DeleteFile")]
    public async Task<HttpResponseData> DeleteFile(
        [HttpTrigger(AuthorizationLevel.Function, "delete", Route = "file/{fileId}")] HttpRequestData req,
        string fileId)
    {
        try
        {
            if (string.IsNullOrEmpty(fileId))
            {
                _logger.LogWarning("Missing fileId parameter");
                var badRequestResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await badRequestResponse.WriteStringAsync("Missing fileId parameter");
                return badRequestResponse;
            }

            var deleted = await _storageService.DeleteFileAsync(fileId);

            var response = req.CreateResponse(deleted ? HttpStatusCode.OK : HttpStatusCode.NotFound);
            response.Headers.Add("Content-Type", "application/json");

            var result = new
            {
                fileId = fileId,
                deleted = deleted,
                deletedAt = deleted ? DateTime.UtcNow : (DateTime?)null
            };

            var json = JsonSerializer.Serialize(result, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            await response.WriteStringAsync(json);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting file: {FileId}", fileId);

            var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
            await errorResponse.WriteStringAsync("Internal server error");
            return errorResponse;
        }
    }
}