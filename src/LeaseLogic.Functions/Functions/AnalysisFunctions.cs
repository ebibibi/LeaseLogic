using LeaseLogic.Functions.Models;
using LeaseLogic.Functions.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.DurableTask.Client;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Text.Json;

namespace LeaseLogic.Functions.Functions;

public class AnalysisFunctions
{
    private readonly IStorageService _storageService;
    private readonly ILogger<AnalysisFunctions> _logger;

    public AnalysisFunctions(IStorageService storageService, ILogger<AnalysisFunctions> logger)
    {
        _storageService = storageService;
        _logger = logger;
    }

    [Function("StartAnalysis")]
    public async Task<HttpResponseData> StartAnalysis(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "analyze")] HttpRequestData req,
        [DurableClient] DurableTaskClient client)
    {
        try
        {
            // Read request body
            var requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            var analysisRequest = JsonSerializer.Deserialize<AnalysisRequest>(requestBody, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            if (analysisRequest == null)
            {
                _logger.LogWarning("Invalid request body for analysis start");
                var badRequestResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await badRequestResponse.WriteStringAsync("Invalid request body");
                return badRequestResponse;
            }

            // Validate request
            if (string.IsNullOrEmpty(analysisRequest.FileId))
            {
                _logger.LogWarning("Missing fileId in analysis request");
                var badRequestResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await badRequestResponse.WriteStringAsync("Missing fileId");
                return badRequestResponse;
            }

            // Check if file exists
            var fileExists = await _storageService.FileExistsAsync(analysisRequest.FileId);
            if (!fileExists)
            {
                _logger.LogWarning("File not found: {FileId}", analysisRequest.FileId);
                var notFoundResponse = req.CreateResponse(HttpStatusCode.NotFound);
                await notFoundResponse.WriteStringAsync("File not found");
                return notFoundResponse;
            }

            // Start durable function orchestration
            var instanceId = await client.ScheduleNewOrchestrationInstanceAsync(
                "LeaseAnalysisOrchestrator", 
                analysisRequest);

            _logger.LogInformation("Started lease analysis for file: {FileId} (Instance: {InstanceId})", 
                analysisRequest.FileId, instanceId);

            // Create initial analysis status
            var analysisStatus = new AnalysisStatus
            {
                AnalysisId = instanceId,
                Status = "Running",
                FileInfo = new FileInfo
                {
                    FileName = analysisRequest.FileName,
                    FileSize = analysisRequest.FileSize
                },
                Progress = new ProgressInfo
                {
                    CurrentStep = "Initializing",
                    Percentage = 0,
                    Message = "解析を開始しています..."
                },
                CreatedTime = DateTime.UtcNow,
                LastUpdatedTime = DateTime.UtcNow
            };

            await _storageService.SaveAnalysisMetadataAsync(instanceId, analysisStatus);

            // Create response
            var response = req.CreateResponse(HttpStatusCode.OK);
            response.Headers.Add("Content-Type", "application/json");

            var result = new
            {
                analysisId = instanceId,
                status = "Running",
                statusUrl = $"/api/status/{instanceId}",
                resultUrl = $"/api/result/{instanceId}",
                estimatedDuration = "5-10分",
                createdTime = DateTime.UtcNow
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
            _logger.LogError(ex, "Error starting analysis");

            var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
            await errorResponse.WriteStringAsync("Internal server error");
            return errorResponse;
        }
    }

    [Function("GetAnalysisStatus")]
    public async Task<HttpResponseData> GetAnalysisStatus(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = "status/{analysisId}")] HttpRequestData req,
        [DurableClient] DurableTaskClient client,
        string analysisId)
    {
        try
        {
            if (string.IsNullOrEmpty(analysisId))
            {
                _logger.LogWarning("Missing analysisId parameter");
                var badRequestResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await badRequestResponse.WriteStringAsync("Missing analysisId parameter");
                return badRequestResponse;
            }

            // Get orchestration status
            var orchestrationStatus = await client.GetInstanceAsync(analysisId);
            if (orchestrationStatus == null)
            {
                _logger.LogWarning("Analysis not found: {AnalysisId}", analysisId);
                var notFoundResponse = req.CreateResponse(HttpStatusCode.NotFound);
                await notFoundResponse.WriteStringAsync("Analysis not found");
                return notFoundResponse;
            }

            // Get stored metadata
            var metadata = await _storageService.GetAnalysisMetadataAsync(analysisId);

            // Map orchestration status to our status format
            var status = orchestrationStatus.RuntimeStatus switch
            {
                OrchestrationRuntimeStatus.Running => "Running",
                OrchestrationRuntimeStatus.Completed => "Completed",
                OrchestrationRuntimeStatus.Failed => "Failed",
                OrchestrationRuntimeStatus.Terminated => "Terminated",
                _ => "Unknown"
            };

            var response = req.CreateResponse(HttpStatusCode.OK);
            response.Headers.Add("Content-Type", "application/json");

            var result = new
            {
                analysisId = analysisId,
                status = status,
                fileInfo = new
                {
                    fileName = metadata?.FileInfo.FileName ?? "",
                    fileSize = metadata?.FileInfo.FileSize ?? 0,
                    uploadedAt = metadata?.CreatedTime ?? DateTime.UtcNow
                },
                progress = new
                {
                    currentStep = metadata?.Progress.CurrentStep ?? "Unknown",
                    percentage = metadata?.Progress.Percentage ?? 0,
                    message = metadata?.Progress.Message ?? "",
                    estimatedRemaining = "00:02:30" // Placeholder
                },
                result = status == "Completed" ? orchestrationStatus.SerializedOutput : null,
                createdTime = orchestrationStatus.CreatedAt,
                lastUpdatedTime = orchestrationStatus.LastUpdatedAt
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
            _logger.LogError(ex, "Error getting analysis status: {AnalysisId}", analysisId);

            var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
            await errorResponse.WriteStringAsync("Internal server error");
            return errorResponse;
        }
    }

    [Function("GetAnalysisResult")]
    public async Task<HttpResponseData> GetAnalysisResult(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = "result/{analysisId}")] HttpRequestData req,
        [DurableClient] DurableTaskClient client,
        string analysisId)
    {
        try
        {
            if (string.IsNullOrEmpty(analysisId))
            {
                _logger.LogWarning("Missing analysisId parameter");
                var badRequestResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await badRequestResponse.WriteStringAsync("Missing analysisId parameter");
                return badRequestResponse;
            }

            // Check orchestration status
            var orchestrationStatus = await client.GetInstanceAsync(analysisId);
            if (orchestrationStatus == null)
            {
                _logger.LogWarning("Analysis not found: {AnalysisId}", analysisId);
                var notFoundResponse = req.CreateResponse(HttpStatusCode.NotFound);
                await notFoundResponse.WriteStringAsync("Analysis not found");
                return notFoundResponse;
            }

            if (orchestrationStatus.RuntimeStatus != OrchestrationRuntimeStatus.Completed)
            {
                _logger.LogWarning("Analysis not completed yet: {AnalysisId}, Status: {Status}", 
                    analysisId, orchestrationStatus.RuntimeStatus);
                
                var badRequestResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await badRequestResponse.WriteStringAsync($"Analysis is not completed yet. Current status: {orchestrationStatus.RuntimeStatus}");
                return badRequestResponse;
            }

            // Get result from storage
            var result = await _storageService.GetAnalysisResultAsync(analysisId);
            if (result == null)
            {
                _logger.LogWarning("Analysis result not found in storage: {AnalysisId}", analysisId);
                var notFoundResponse = req.CreateResponse(HttpStatusCode.NotFound);
                await notFoundResponse.WriteStringAsync("Analysis result not found");
                return notFoundResponse;
            }

            var response = req.CreateResponse(HttpStatusCode.OK);
            response.Headers.Add("Content-Type", "application/json");

            var json = JsonSerializer.Serialize(result, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = true
            });

            await response.WriteStringAsync(json);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting analysis result: {AnalysisId}", analysisId);

            var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
            await errorResponse.WriteStringAsync("Internal server error");
            return errorResponse;
        }
    }
}