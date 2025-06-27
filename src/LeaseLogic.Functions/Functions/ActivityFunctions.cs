using Azure.AI.DocumentIntelligence;
using LeaseLogic.Functions.Models;
using LeaseLogic.Functions.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace LeaseLogic.Functions.Functions;

public class ActivityFunctions
{
    private readonly IStorageService _storageService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<ActivityFunctions> _logger;

    public ActivityFunctions(
        IStorageService storageService,
        IConfiguration configuration,
        ILogger<ActivityFunctions> logger)
    {
        _storageService = storageService;
        _configuration = configuration;
        _logger = logger;
    }

    [Function("DocumentParser")]
    public async Task<ParsedDocument> ParseDocument([ActivityTrigger] string fileId)
    {
        try
        {
            _logger.LogInformation("Starting document parsing for file: {FileId}", fileId);

            // Get file stream from storage
            var fileStream = await _storageService.GetFileStreamAsync(fileId);
            
            // Initialize Document Intelligence client
            var endpoint = _configuration["DOCUMENT_INTELLIGENCE_ENDPOINT"];
            var apiKey = _configuration["DOCUMENT_INTELLIGENCE_API_KEY"];
            
            if (string.IsNullOrEmpty(endpoint) || string.IsNullOrEmpty(apiKey))
            {
                throw new InvalidOperationException("Document Intelligence configuration is missing");
            }

            var client = new DocumentIntelligenceClient(new Uri(endpoint), new Azure.AzureKeyCredential(apiKey));

            // Analyze document using prebuilt-contract model
            var operation = await client.AnalyzeDocumentAsync(
                WaitUntil.Completed,
                "prebuilt-contract",
                fileStream);

            var result = operation.Value;

            _logger.LogInformation("Document parsing completed for file: {FileId}, Pages: {PageCount}", 
                fileId, result.Pages?.Count ?? 0);

            return new ParsedDocument
            {
                AnalyzeResult = result,
                FileId = fileId,
                FileName = fileId, // We'll get the actual filename from metadata later
                ParsedAt = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing document: {FileId}", fileId);
            throw;
        }
    }

    [Function("ContentStructurer")]
    public async Task<StructuredContent> StructureContent([ActivityTrigger] ParsedDocument parsedDocument)
    {
        try
        {
            _logger.LogInformation("Starting content structuring for file: {FileId}", parsedDocument.FileId);

            var result = parsedDocument.AnalyzeResult;
            var structuredContent = new StructuredContent
            {
                FileId = parsedDocument.FileId
            };

            // Extract raw text content
            if (result.Content != null)
            {
                structuredContent.RawContent = result.Content;
            }

            // Extract contract parties
            structuredContent.ContractParties = ExtractContractParties(result);

            // Extract asset details
            structuredContent.AssetDetails = ExtractAssetDetails(result);

            // Extract payment terms
            structuredContent.PaymentTerms = ExtractPaymentTerms(result);

            // Extract contract period
            structuredContent.ContractPeriod = ExtractContractPeriod(result);

            // Extract special clauses
            structuredContent.SpecialClauses = ExtractSpecialClauses(result);

            _logger.LogInformation("Content structuring completed for file: {FileId}", parsedDocument.FileId);

            return structuredContent;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error structuring content: {FileId}", parsedDocument.FileId);
            throw;
        }
    }

    [Function("LeaseClassifier")]
    public async Task<LeaseClassification> ClassifyLease([ActivityTrigger] StructuredContent content)
    {
        try
        {
            _logger.LogInformation("Starting lease classification for file: {FileId}", content.FileId);

            // This is a simplified implementation
            // In a real implementation, you would use OpenAI Assistants API
            var classification = new LeaseClassification
            {
                FileId = content.FileId,
                ClassifiedAt = DateTime.UtcNow
            };

            // Simple rule-based classification (placeholder)
            var analysisText = content.ToAnalysisText().ToLowerInvariant();
            
            // Check for lease indicators
            var leaseIndicators = new[]
            {
                "賃貸", "リース", "lease", "rental", "借用", "使用権"
            };

            var hasLeaseIndicators = leaseIndicators.Any(indicator => analysisText.Contains(indicator));

            if (hasLeaseIndicators)
            {
                classification.IsLease = true;
                classification.Confidence = 0.85;
                classification.LeaseType = DetermineLeaseType(analysisText);
                
                // Analyze identified asset
                classification.IdentifiedAssetAnalysis = new IdentifiedAssetAnalysis
                {
                    HasIdentifiedAsset = true,
                    AssetDescription = content.AssetDetails.AssetDescription,
                    AssetSpecificity = "特定された物理的資産",
                    Citations = new List<string> { "IFRS 16.B13", "ASC 842-10-15-13" }
                };

                // Analyze right to control
                classification.RightToControlAnalysis = new RightToControlAnalysis
                {
                    HasRightToControl = true,
                    ControlIndicators = new List<string> 
                    { 
                        "資産の使用方法を指示する権利",
                        "資産からの経済的便益を享受する権利"
                    },
                    Citations = new List<string> { "IFRS 16.B9(a)", "ASC 842-10-15-3(a)" }
                };

                // Analyze substitution rights
                classification.SubstitutionRightsAnalysis = new SubstitutionRightsAnalysis
                {
                    HasSubstitutionRights = false,
                    Analysis = "貸手に実質的な代替権は見られない",
                    Citations = new List<string> { "IFRS 16.B14", "ASC 842-10-15-4" }
                };

                classification.Reasoning = new List<string>
                {
                    "契約書にリース関連の用語が含まれている",
                    "特定された資産の使用権が識別される",
                    "借手が資産の使用を制御する権利を有する"
                };
            }
            else
            {
                classification.IsLease = false;
                classification.Confidence = 0.75;
                classification.LeaseType = LeaseType.ServiceContract;
                classification.Reasoning = new List<string>
                {
                    "リース契約を示す明確な要素が見つからない",
                    "サービス契約の特性が強い"
                };
            }

            _logger.LogInformation("Lease classification completed for file: {FileId}, IsLease: {IsLease}, Confidence: {Confidence}", 
                content.FileId, classification.IsLease, classification.Confidence);

            return classification;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error classifying lease: {FileId}", content.FileId);
            throw;
        }
    }

    [Function("ReportGenerator")]
    public async Task<AnalysisResult> GenerateReport([ActivityTrigger] object input)
    {
        try
        {
            var inputData = JsonSerializer.Deserialize<JsonElement>(JsonSerializer.Serialize(input));
            var request = JsonSerializer.Deserialize<AnalysisRequest>(inputData.GetProperty("request").GetRawText());
            var structuredContent = JsonSerializer.Deserialize<StructuredContent>(inputData.GetProperty("structuredContent").GetRawText());
            var leaseClassification = JsonSerializer.Deserialize<LeaseClassification>(inputData.GetProperty("leaseClassification").GetRawText());
            var processingTime = JsonSerializer.Deserialize<TimeSpan>(inputData.GetProperty("processingTime").GetRawText());

            if (request == null || structuredContent == null || leaseClassification == null)
            {
                throw new ArgumentException("Invalid input data for report generation");
            }

            _logger.LogInformation("Starting report generation for file: {FileId}", request.FileId);

            var result = new AnalysisResult
            {
                AnalysisId = request.FileId, // This should be the orchestration instance ID
                FileInfo = new FileInfo
                {
                    FileName = request.FileName,
                    FileSize = request.FileSize,
                    UploadedAt = DateTime.UtcNow
                },
                AnalysisResult = new LeaseAnalysis
                {
                    IsLease = leaseClassification.IsLease,
                    Confidence = leaseClassification.Confidence,
                    LeaseType = leaseClassification.LeaseType,
                    Summary = new ContractSummary
                    {
                        ContractType = leaseClassification.IsLease ? "リース契約" : "サービス契約",
                        PrimaryAsset = structuredContent.AssetDetails.AssetDescription,
                        ContractPeriod = structuredContent.ContractPeriod.ToString(),
                        MonthlyPayment = structuredContent.PaymentTerms.ToString()
                    },
                    LeaseAnalysis = new DetailedLeaseAnalysis
                    {
                        IdentifiedAsset = leaseClassification.IdentifiedAssetAnalysis,
                        RightToControl = leaseClassification.RightToControlAnalysis,
                        SubstantiveSubstitutionRights = leaseClassification.SubstitutionRightsAnalysis
                    },
                    KeyFindings = leaseClassification.Reasoning,
                    RiskFactors = GenerateRiskFactors(structuredContent),
                    Recommendations = GenerateRecommendations(leaseClassification),
                    ComplianceRequirements = GenerateComplianceRequirements(leaseClassification)
                },
                DocumentSummary = GenerateDocumentSummary(structuredContent, leaseClassification),
                ProcessingTime = processingTime,
                CompletedAt = DateTime.UtcNow
            };

            // Save the result to storage
            await _storageService.SaveAnalysisResultAsync(result.AnalysisId, result);

            _logger.LogInformation("Report generation completed for file: {FileId}", request.FileId);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating report");
            throw;
        }
    }

    [Function("ErrorResultGenerator")]
    public async Task<AnalysisResult> GenerateErrorResult([ActivityTrigger] object input)
    {
        try
        {
            var inputData = JsonSerializer.Deserialize<JsonElement>(JsonSerializer.Serialize(input));
            var request = JsonSerializer.Deserialize<AnalysisRequest>(inputData.GetProperty("input").GetRawText());
            var errorMessage = inputData.GetProperty("exception").GetString();
            var processingTime = JsonSerializer.Deserialize<TimeSpan>(inputData.GetProperty("processingTime").GetRawText());

            if (request == null)
            {
                throw new ArgumentException("Invalid input data for error result generation");
            }

            var result = new AnalysisResult
            {
                AnalysisId = request.FileId,
                FileInfo = new FileInfo
                {
                    FileName = request.FileName,
                    FileSize = request.FileSize,
                    UploadedAt = DateTime.UtcNow
                },
                AnalysisResult = new LeaseAnalysis
                {
                    IsLease = false,
                    Confidence = 0.0,
                    LeaseType = LeaseType.NotApplicable,
                    Summary = new ContractSummary
                    {
                        ContractType = "解析エラー",
                        PrimaryAsset = "不明",
                        ContractPeriod = "不明",
                        MonthlyPayment = "不明"
                    },
                    KeyFindings = new List<string> { "解析処理中にエラーが発生しました" },
                    RiskFactors = new List<string> { $"エラー詳細: {errorMessage}" },
                    Recommendations = new List<string> { "ファイル形式や内容を確認して再試行してください" },
                    ComplianceRequirements = new List<string>()
                },
                DocumentSummary = $"解析エラーのため、ドキュメントの内容を処理できませんでした。エラー: {errorMessage}",
                ProcessingTime = processingTime,
                CompletedAt = DateTime.UtcNow
            };

            await _storageService.SaveAnalysisResultAsync(result.AnalysisId, result);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating error result");
            throw;
        }
    }

    // Helper methods
    private ContractParties ExtractContractParties(AnalyzeResult result)
    {
        // Simplified extraction logic
        var content = result.Content ?? "";
        
        return new ContractParties
        {
            Lessor = ExtractEntityByPattern(content, @"貸主|貸し主|賃貸人|lessor", @"借主|借り主|賃借人|lessee"),
            Lessee = ExtractEntityByPattern(content, @"借主|借り主|賃借人|lessee", @"貸主|貸し主|賃貸人|lessor")
        };
    }

    private AssetDetails ExtractAssetDetails(AnalyzeResult result)
    {
        var content = result.Content ?? "";
        
        return new AssetDetails
        {
            AssetType = ExtractAssetType(content),
            AssetDescription = ExtractAssetDescription(content),
            Location = ExtractLocation(content)
        };
    }

    private PaymentTerms ExtractPaymentTerms(AnalyzeResult result)
    {
        var content = result.Content ?? "";
        
        return new PaymentTerms
        {
            Amount = ExtractAmount(content),
            Currency = "JPY",
            Frequency = ExtractPaymentFrequency(content)
        };
    }

    private ContractPeriod ExtractContractPeriod(AnalyzeResult result)
    {
        var content = result.Content ?? "";
        
        return new ContractPeriod
        {
            StartDate = ExtractStartDate(content),
            EndDate = ExtractEndDate(content),
            DurationMonths = CalculateDurationMonths(content)
        };
    }

    private List<SpecialClause> ExtractSpecialClauses(AnalyzeResult result)
    {
        var content = result.Content ?? "";
        var clauses = new List<SpecialClause>();

        if (content.Contains("更新") || content.Contains("延長"))
        {
            clauses.Add(new SpecialClause
            {
                Type = "更新オプション",
                Description = "契約更新または延長に関する条項",
                Impact = "リース期間の判定に影響"
            });
        }

        if (content.Contains("解約") || content.Contains("中途"))
        {
            clauses.Add(new SpecialClause
            {
                Type = "中途解約",
                Description = "中途解約に関する条項",
                Impact = "リース期間の判定に影響"
            });
        }

        return clauses;
    }

    private LeaseType DetermineLeaseType(string content)
    {
        if (content.Contains("ファイナンス") || content.Contains("finance"))
            return LeaseType.FinanceLease;
        
        if (content.Contains("オペレーティング") || content.Contains("operating"))
            return LeaseType.OperatingLease;
            
        return LeaseType.OperatingLease; // Default
    }

    private List<string> GenerateRiskFactors(StructuredContent content)
    {
        var riskFactors = new List<string>();

        if (content.ContractPeriod.HasRenewalOption)
        {
            riskFactors.Add("契約更新オプションの存在");
        }

        if (content.ContractPeriod.HasTerminationOption)
        {
            riskFactors.Add("中途解約オプションの存在");
        }

        if (content.SpecialClauses.Any())
        {
            riskFactors.Add("特別条項による判定の複雑化");
        }

        return riskFactors;
    }

    private List<string> GenerateRecommendations(LeaseClassification classification)
    {
        var recommendations = new List<string>();

        if (classification.IsLease)
        {
            recommendations.Add("IFRS 16 / ASC 842の適用対象として認識");
            recommendations.Add("使用権資産とリース負債の計上が必要");
            recommendations.Add("契約開始日における初期測定の実施");
        }
        else
        {
            recommendations.Add("リース会計基準の適用対象外として処理");
            recommendations.Add("サービス契約として費用処理");
        }

        return recommendations;
    }

    private List<string> GenerateComplianceRequirements(LeaseClassification classification)
    {
        var requirements = new List<string>();

        if (classification.IsLease)
        {
            requirements.Add("使用権資産の認識と測定");
            requirements.Add("リース負債の計算と計上");
            requirements.Add("注記事項の開示準備");
            requirements.Add("リース期間の定期的な見直し");
        }

        return requirements;
    }

    private string GenerateDocumentSummary(StructuredContent content, LeaseClassification classification)
    {
        return $"{content.AssetDetails.AssetType}に関する{(classification.IsLease ? "リース契約" : "サービス契約")}。" +
               $"契約期間{content.ContractPeriod.DurationMonths}ヶ月、" +
               $"月額{content.PaymentTerms.Amount:N0}円。" +
               $"{(classification.IsLease ? "新会計基準の適用対象" : "リース会計基準の適用対象外")}。";
    }

    // Simple pattern extraction helpers (placeholder implementations)
    private string ExtractEntityByPattern(string content, string targetPattern, string excludePattern)
    {
        // Simplified implementation
        var lines = content.Split('\n');
        foreach (var line in lines)
        {
            if (Regex.IsMatch(line, targetPattern, RegexOptions.IgnoreCase) && 
                !Regex.IsMatch(line, excludePattern, RegexOptions.IgnoreCase))
            {
                return line.Trim();
            }
        }
        return "不明";
    }

    private string ExtractAssetType(string content)
    {
        var assetTypes = new[] { "建物", "車両", "機械", "設備", "オフィス", "倉庫" };
        foreach (var type in assetTypes)
        {
            if (content.Contains(type))
                return type;
        }
        return "不明";
    }

    private string ExtractAssetDescription(string content)
    {
        // Simplified implementation
        return "契約書に記載された資産";
    }

    private string ExtractLocation(string content)
    {
        // Simple pattern matching for Japanese addresses
        var locationMatch = Regex.Match(content, @"[都道府県市区町村]\w+");
        return locationMatch.Success ? locationMatch.Value : "不明";
    }

    private decimal ExtractAmount(string content)
    {
        var amountMatch = Regex.Match(content, @"(\d{1,3}(?:,\d{3})*)\s*円");
        if (amountMatch.Success && decimal.TryParse(amountMatch.Groups[1].Value.Replace(",", ""), out var amount))
        {
            return amount;
        }
        return 0;
    }

    private string ExtractPaymentFrequency(string content)
    {
        if (content.Contains("月額") || content.Contains("毎月"))
            return "毎月";
        if (content.Contains("年額") || content.Contains("毎年"))
            return "毎年";
        return "不明";
    }

    private DateTime ExtractStartDate(string content)
    {
        var dateMatch = Regex.Match(content, @"(\d{4})年(\d{1,2})月(\d{1,2})日");
        if (dateMatch.Success)
        {
            var year = int.Parse(dateMatch.Groups[1].Value);
            var month = int.Parse(dateMatch.Groups[2].Value);
            var day = int.Parse(dateMatch.Groups[3].Value);
            return new DateTime(year, month, day);
        }
        return DateTime.Today;
    }

    private DateTime ExtractEndDate(string content)
    {
        return ExtractStartDate(content).AddYears(1); // Simplified
    }

    private int CalculateDurationMonths(string content)
    {
        var monthMatch = Regex.Match(content, @"(\d+)\s*[ヶ|ヵ]?月");
        if (monthMatch.Success && int.TryParse(monthMatch.Groups[1].Value, out var months))
        {
            return months;
        }
        return 12; // Default to 1 year
    }
}