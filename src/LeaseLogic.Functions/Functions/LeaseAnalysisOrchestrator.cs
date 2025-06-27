using LeaseLogic.Functions.Models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.DurableTask;
using Microsoft.Extensions.Logging;

namespace LeaseLogic.Functions.Functions;

public class LeaseAnalysisOrchestrator
{
    private readonly ILogger<LeaseAnalysisOrchestrator> _logger;

    public LeaseAnalysisOrchestrator(ILogger<LeaseAnalysisOrchestrator> logger)
    {
        _logger = logger;
    }

    [Function("LeaseAnalysisOrchestrator")]
    public async Task<AnalysisResult> RunOrchestrator(
        [OrchestrationTrigger] TaskOrchestrationContext context)
    {
        var logger = context.CreateReplaySafeLogger(_logger);
        var input = context.GetInput<AnalysisRequest>();

        if (input == null)
        {
            throw new ArgumentException("Invalid input for orchestration");
        }

        logger.LogInformation("Starting lease analysis orchestration for file: {FileId}", input.FileId);

        var startTime = context.CurrentUtcDateTime;
        AnalysisResult result;

        try
        {
            // Phase 1: Document Intelligence による構造化解析
            logger.LogInformation("Phase 1: Document Intelligence parsing - FileId: {FileId}", input.FileId);
            context.SetCustomStatus(new 
            { 
                currentStep = "DocumentParsing", 
                progress = 15, 
                message = "Document Intelligence解析開始",
                fileId = input.FileId
            });

            var parsedDocument = await context.CallActivityAsync<ParsedDocument>(
                "DocumentParser", input.FileId);

            // Phase 2: 契約書固有の構造認識と前処理
            logger.LogInformation("Phase 2: Content structuring - FileId: {FileId}", input.FileId);
            context.SetCustomStatus(new 
            { 
                currentStep = "ContentStructuring", 
                progress = 35, 
                message = "契約書構造分析中",
                fileId = input.FileId
            });

            var structuredContent = await context.CallActivityAsync<StructuredContent>(
                "ContentStructurer", parsedDocument);

            // Phase 3: OpenAI による意味理解とリース判定
            logger.LogInformation("Phase 3: AI lease classification - FileId: {FileId}", input.FileId);
            context.SetCustomStatus(new 
            { 
                currentStep = "AIAnalysis", 
                progress = 70, 
                message = "OpenAI Service リース判定実行中",
                fileId = input.FileId
            });

            var leaseClassification = await context.CallActivityAsync<LeaseClassification>(
                "LeaseClassifier", structuredContent);

            // Phase 4: 最終レポート生成
            logger.LogInformation("Phase 4: Report generation - FileId: {FileId}", input.FileId);
            context.SetCustomStatus(new 
            { 
                currentStep = "ReportGeneration", 
                progress = 90, 
                message = "総合レポート生成中",
                fileId = input.FileId
            });

            var reportInput = new
            {
                request = input,
                structuredContent = structuredContent,
                leaseClassification = leaseClassification,
                processingTime = context.CurrentUtcDateTime - startTime
            };

            result = await context.CallActivityAsync<AnalysisResult>(
                "ReportGenerator", reportInput);

            // 完了状態を設定
            context.SetCustomStatus(new 
            { 
                currentStep = "Completed", 
                progress = 100, 
                message = "ハイブリッド解析完了",
                fileId = input.FileId
            });

            logger.LogInformation("Completed lease analysis orchestration for file: {FileId}, Result: {IsLease}", 
                input.FileId, result.AnalysisResult.IsLease);

            return result;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error in lease analysis orchestration for file: {FileId}", input.FileId);
            
            // エラー時の結果を生成
            context.SetCustomStatus(new 
            { 
                currentStep = "Failed", 
                progress = 0, 
                message = $"解析エラー: {ex.Message}",
                fileId = input.FileId
            });

            // エラー時の分析結果を作成
            result = await context.CallActivityAsync<AnalysisResult>(
                "ErrorResultGenerator", new { input, exception = ex.Message, processingTime = context.CurrentUtcDateTime - startTime });

            return result;
        }
    }
}