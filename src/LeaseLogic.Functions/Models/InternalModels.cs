using Azure.AI.DocumentIntelligence;

namespace LeaseLogic.Functions.Models;

// Document Intelligence 結果モデル
public class ParsedDocument
{
    public AnalyzeResult? AnalyzeResult { get; set; }
    public string FileId { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public DateTime ParsedAt { get; set; } = DateTime.UtcNow;
}

// 構造化されたコンテンツモデル
public class StructuredContent
{
    public string FileId { get; set; } = string.Empty;
    public ContractParties ContractParties { get; set; } = new();
    public AssetDetails AssetDetails { get; set; } = new();
    public PaymentTerms PaymentTerms { get; set; } = new();
    public ContractPeriod ContractPeriod { get; set; } = new();
    public List<SpecialClause> SpecialClauses { get; set; } = new();
    public string RawContent { get; set; } = string.Empty;
    public DateTime StructuredAt { get; set; } = DateTime.UtcNow;

    public string ToAnalysisText()
    {
        return $@"
契約当事者: {ContractParties}
資産詳細: {AssetDetails}
支払条件: {PaymentTerms}
契約期間: {ContractPeriod}
特別条項: {string.Join(", ", SpecialClauses.Select(c => c.Description))}

全文:
{RawContent}
";
    }
}

public class ContractParties
{
    public string Lessor { get; set; } = string.Empty;
    public string Lessee { get; set; } = string.Empty;
    public List<string> OtherParties { get; set; } = new();

    public override string ToString()
    {
        return $"貸手: {Lessor}, 借手: {Lessee}";
    }
}

public class AssetDetails
{
    public string AssetType { get; set; } = string.Empty;
    public string AssetDescription { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public string Specifications { get; set; } = string.Empty;

    public override string ToString()
    {
        return $"{AssetType} - {AssetDescription} ({Location})";
    }
}

public class PaymentTerms
{
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "JPY";
    public string Frequency { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public List<string> AdditionalFees { get; set; } = new();

    public override string ToString()
    {
        return $"{Amount:N0} {Currency} ({Frequency})";
    }
}

public class ContractPeriod
{
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public int DurationMonths { get; set; }
    public bool HasRenewalOption { get; set; }
    public bool HasTerminationOption { get; set; }
    public string TerminationConditions { get; set; } = string.Empty;

    public override string ToString()
    {
        return $"{StartDate:yyyy-MM-dd} to {EndDate:yyyy-MM-dd} ({DurationMonths}ヶ月)";
    }
}

public class SpecialClause
{
    public string Type { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Impact { get; set; } = string.Empty;
}

// OpenAI によるリース分類結果
public class LeaseClassification
{
    public string FileId { get; set; } = string.Empty;
    public bool IsLease { get; set; }
    public double Confidence { get; set; }
    public LeaseType LeaseType { get; set; }
    public IdentifiedAssetAnalysis IdentifiedAssetAnalysis { get; set; } = new();
    public RightToControlAnalysis RightToControlAnalysis { get; set; } = new();
    public SubstitutionRightsAnalysis SubstitutionRightsAnalysis { get; set; } = new();
    public List<string> Reasoning { get; set; } = new();
    public List<string> Citations { get; set; } = new();
    public DateTime ClassifiedAt { get; set; } = DateTime.UtcNow;
}

// 処理状況モデル
public class AnalysisStatus
{
    public string AnalysisId { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public Models.FileInfo FileInfo { get; set; } = new();
    public ProgressInfo Progress { get; set; } = new();
    public AnalysisResult? Result { get; set; }
    public DateTime CreatedTime { get; set; }
    public DateTime LastUpdatedTime { get; set; }
}

public class ProgressInfo
{
    public string CurrentStep { get; set; } = string.Empty;
    public int Percentage { get; set; }
    public string Message { get; set; } = string.Empty;
    public TimeSpan EstimatedRemaining { get; set; }
}