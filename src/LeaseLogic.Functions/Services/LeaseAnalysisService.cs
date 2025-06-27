using LeaseLogic.Functions.Models;
using Microsoft.Extensions.Logging;

namespace LeaseLogic.Functions.Services;

public class LeaseAnalysisService : ILeaseAnalysisService
{
    private readonly ILogger<LeaseAnalysisService> _logger;

    public LeaseAnalysisService(ILogger<LeaseAnalysisService> logger)
    {
        _logger = logger;
    }

    public Task<AnalysisResult> GenerateAnalysisReportAsync(
        AnalysisRequest request,
        StructuredContent structuredContent,
        LeaseClassification leaseClassification)
    {
        _logger.LogInformation("Generating analysis report: {FileId}", request.FileId);
        
        // Placeholder implementation
        var result = new AnalysisResult
        {
            AnalysisId = request.FileId,
            FileInfo = new Models.FileInfo
            {
                FileName = request.FileName,
                FileSize = request.FileSize,
                UploadedAt = DateTime.UtcNow
            },
            Analysis = new LeaseAnalysis
            {
                IsLease = leaseClassification.IsLease,
                Confidence = leaseClassification.Confidence,
                LeaseType = leaseClassification.LeaseType,
                Summary = new ContractSummary
                {
                    ContractType = leaseClassification.IsLease ? "リース契約" : "サービス契約",
                    PrimaryAsset = "オフィスビル",
                    ContractPeriod = "36ヶ月"
                },
                DetailedAnalysis = new DetailedLeaseAnalysis(),
                KeyFindings = leaseClassification.Reasoning,
                RiskFactors = new List<string> { "Placeholder risk factors" },
                Recommendations = new List<string> { "Placeholder recommendations" },
                ComplianceRequirements = new List<string> { "Placeholder compliance requirements" }
            },
            DocumentSummary = "Placeholder document summary",
            ProcessingTime = TimeSpan.FromMinutes(2),
            CompletedAt = DateTime.UtcNow
        };

        return Task.FromResult(result);
    }

    public string FormatAnalysisResultAsJson(AnalysisResult result)
    {
        _logger.LogInformation("Formatting analysis result as JSON: {AnalysisId}", result.AnalysisId);
        
        // Placeholder implementation
        return System.Text.Json.JsonSerializer.Serialize(result, new System.Text.Json.JsonSerializerOptions
        {
            WriteIndented = true
        });
    }

    public AnalysisResult CreateErrorResult(string analysisId, string fileName, Exception exception)
    {
        _logger.LogError(exception, "Creating error result for analysis: {AnalysisId}", analysisId);
        
        return new AnalysisResult
        {
            AnalysisId = analysisId,
            FileInfo = new Models.FileInfo
            {
                FileName = fileName,
                FileSize = 0,
                UploadedAt = DateTime.UtcNow
            },
            Analysis = new LeaseAnalysis
            {
                IsLease = false,
                Confidence = 0.0,
                LeaseType = LeaseType.NotApplicable,
                KeyFindings = new List<string> { $"エラーが発生しました: {exception.Message}" },
                RiskFactors = new List<string>(),
                Recommendations = new List<string>(),
                ComplianceRequirements = new List<string>()
            },
            DocumentSummary = $"分析エラー: {exception.Message}",
            ProcessingTime = TimeSpan.Zero,
            CompletedAt = DateTime.UtcNow
        };
    }
}