using Azure.Data.Tables;
using Azure.Storage.Blobs;
using Azure.Storage.Sas;
using LeaseLogic.Functions.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace LeaseLogic.Functions.Services;

public class StorageService : IStorageService
{
    private readonly BlobServiceClient _blobServiceClient;
    private readonly TableServiceClient _tableServiceClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<StorageService> _logger;

    private const string DocumentsContainer = "documents";
    private const string ResultsContainer = "results";
    private const string AnalysisMetadataTable = "AnalysisMetadata";

    public StorageService(IConfiguration configuration, ILogger<StorageService> logger)
    {
        _configuration = configuration;
        _logger = logger;

        var connectionString = _configuration.GetConnectionString("AzureWebJobsStorage") 
                              ?? _configuration["STORAGE_CONNECTION_STRING"];

        if (string.IsNullOrEmpty(connectionString))
        {
            throw new InvalidOperationException("Storage connection string is not configured");
        }

        _blobServiceClient = new BlobServiceClient(connectionString);
        _tableServiceClient = new TableServiceClient(connectionString);
    }

    public async Task<UploadUrlResponse> GenerateUploadUrlAsync(UploadUrlRequest request)
    {
        try
        {
            var fileId = Guid.NewGuid().ToString();
            var containerClient = _blobServiceClient.GetBlobContainerClient(DocumentsContainer);
            
            // Ensure container exists
            await containerClient.CreateIfNotExistsAsync();

            var blobClient = containerClient.GetBlobClient(fileId);

            // Generate SAS URL with write permissions
            var sasBuilder = new BlobSasBuilder
            {
                BlobContainerName = DocumentsContainer,
                BlobName = fileId,
                Resource = "b",
                ExpiresOn = DateTimeOffset.UtcNow.AddHours(1),
            };

            sasBuilder.SetPermissions(BlobSasPermissions.Write | BlobSasPermissions.Create);

            var sasUrl = blobClient.GenerateSasUri(sasBuilder).ToString();

            // Save file metadata
            await SaveFileMetadataAsync(fileId, request.FileName, request.FileSize, request.ContentType);

            var response = new UploadUrlResponse
            {
                FileId = fileId,
                UploadUrl = sasUrl,
                ExpiresAt = sasBuilder.ExpiresOn.DateTime,
                MaxFileSize = 52428800 // 50MB
            };

            _logger.LogInformation("Generated upload URL for file: {FileName} (ID: {FileId})", 
                request.FileName, fileId);

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate upload URL for file: {FileName}", request.FileName);
            throw;
        }
    }

    public async Task SaveFileMetadataAsync(string fileId, string fileName, long fileSize, string contentType)
    {
        try
        {
            var tableClient = _tableServiceClient.GetTableClient("FileMetadata");
            await tableClient.CreateIfNotExistsAsync();

            var entity = new TableEntity("FileMetadata", fileId)
            {
                ["FileName"] = fileName,
                ["FileSize"] = fileSize,
                ["ContentType"] = contentType,
                ["UploadedAt"] = DateTime.UtcNow,
                ["Status"] = "Uploaded"
            };

            await tableClient.UpsertEntityAsync(entity);

            _logger.LogInformation("Saved file metadata: {FileId} - {FileName}", fileId, fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save file metadata: {FileId}", fileId);
            throw;
        }
    }

    public async Task<bool> FileExistsAsync(string fileId)
    {
        try
        {
            var containerClient = _blobServiceClient.GetBlobContainerClient(DocumentsContainer);
            var blobClient = containerClient.GetBlobClient(fileId);

            var response = await blobClient.ExistsAsync();
            return response.Value;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking file existence: {FileId}", fileId);
            return false;
        }
    }

    public async Task<Stream> GetFileStreamAsync(string fileId)
    {
        try
        {
            var containerClient = _blobServiceClient.GetBlobContainerClient(DocumentsContainer);
            var blobClient = containerClient.GetBlobClient(fileId);

            var response = await blobClient.DownloadStreamingAsync();
            return response.Value.Content;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get file stream: {FileId}", fileId);
            throw;
        }
    }

    public async Task<bool> DeleteFileAsync(string fileId)
    {
        try
        {
            var containerClient = _blobServiceClient.GetBlobContainerClient(DocumentsContainer);
            var blobClient = containerClient.GetBlobClient(fileId);

            var response = await blobClient.DeleteIfExistsAsync();

            if (response.Value)
            {
                // Also delete metadata
                var tableClient = _tableServiceClient.GetTableClient("FileMetadata");
                await tableClient.DeleteEntityAsync("FileMetadata", fileId);

                _logger.LogInformation("Deleted file: {FileId}", fileId);
            }

            return response.Value;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete file: {FileId}", fileId);
            return false;
        }
    }

    public async Task SaveAnalysisResultAsync(string analysisId, AnalysisResult result)
    {
        try
        {
            var containerClient = _blobServiceClient.GetBlobContainerClient(ResultsContainer);
            await containerClient.CreateIfNotExistsAsync();

            var blobClient = containerClient.GetBlobClient($"{analysisId}.json");

            var json = JsonSerializer.Serialize(result, new JsonSerializerOptions 
            { 
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(json));
            await blobClient.UploadAsync(stream, overwrite: true);

            _logger.LogInformation("Saved analysis result: {AnalysisId}", analysisId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save analysis result: {AnalysisId}", analysisId);
            throw;
        }
    }

    public async Task<AnalysisResult?> GetAnalysisResultAsync(string analysisId)
    {
        try
        {
            var containerClient = _blobServiceClient.GetBlobContainerClient(ResultsContainer);
            var blobClient = containerClient.GetBlobClient($"{analysisId}.json");

            var exists = await blobClient.ExistsAsync();
            if (!exists.Value)
            {
                return null;
            }

            var response = await blobClient.DownloadContentAsync();
            var json = response.Value.Content.ToString();

            return JsonSerializer.Deserialize<AnalysisResult>(json, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get analysis result: {AnalysisId}", analysisId);
            return null;
        }
    }

    public async Task SaveAnalysisMetadataAsync(string analysisId, AnalysisStatus status)
    {
        try
        {
            var tableClient = _tableServiceClient.GetTableClient(AnalysisMetadataTable);
            await tableClient.CreateIfNotExistsAsync();

            var entity = new TableEntity("AnalysisMetadata", analysisId)
            {
                ["Status"] = status.Status,
                ["FileName"] = status.FileInfo.FileName,
                ["FileSize"] = status.FileInfo.FileSize,
                ["CurrentStep"] = status.Progress.CurrentStep,
                ["Percentage"] = status.Progress.Percentage,
                ["Message"] = status.Progress.Message,
                ["CreatedTime"] = status.CreatedTime,
                ["LastUpdatedTime"] = status.LastUpdatedTime
            };

            await tableClient.UpsertEntityAsync(entity);

            _logger.LogInformation("Saved analysis metadata: {AnalysisId} - {Status}", analysisId, status.Status);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save analysis metadata: {AnalysisId}", analysisId);
            throw;
        }
    }

    public async Task<AnalysisStatus?> GetAnalysisMetadataAsync(string analysisId)
    {
        try
        {
            var tableClient = _tableServiceClient.GetTableClient(AnalysisMetadataTable);
            
            var response = await tableClient.GetEntityIfExistsAsync<TableEntity>("AnalysisMetadata", analysisId);
            if (!response.HasValue)
            {
                return null;
            }

            var entity = response.Value;
            return new AnalysisStatus
            {
                AnalysisId = analysisId,
                Status = entity.GetString("Status") ?? "",
                FileInfo = new Models.FileInfo
                {
                    FileName = entity.GetString("FileName") ?? "",
                    FileSize = entity.GetInt64("FileSize") ?? 0
                },
                Progress = new ProgressInfo
                {
                    CurrentStep = entity.GetString("CurrentStep") ?? "",
                    Percentage = entity.GetInt32("Percentage") ?? 0,
                    Message = entity.GetString("Message") ?? ""
                },
                CreatedTime = entity.GetDateTime("CreatedTime") ?? DateTime.UtcNow,
                LastUpdatedTime = entity.GetDateTime("LastUpdatedTime") ?? DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get analysis metadata: {AnalysisId}", analysisId);
            return null;
        }
    }

    public async Task UpdateAnalysisMetadataAsync(string analysisId, AnalysisStatus status)
    {
        status.LastUpdatedTime = DateTime.UtcNow;
        await SaveAnalysisMetadataAsync(analysisId, status);
    }
}