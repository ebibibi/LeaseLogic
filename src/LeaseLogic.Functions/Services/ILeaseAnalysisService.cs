using LeaseLogic.Functions.Models;

namespace LeaseLogic.Functions.Services;

public interface ILeaseAnalysisService
{
    /// <summary>
    /// 最終的な分析レポートを生成
    /// </summary>
    Task<AnalysisResult> GenerateAnalysisReportAsync(
        AnalysisRequest request,
        StructuredContent structuredContent,
        LeaseClassification leaseClassification);

    /// <summary>
    /// 分析結果をJSON形式でフォーマット
    /// </summary>
    string FormatAnalysisResultAsJson(AnalysisResult result);

    /// <summary>
    /// エラー時の分析結果を生成
    /// </summary>
    AnalysisResult CreateErrorResult(string analysisId, string fileName, Exception exception);
}