using System.Text.Json.Serialization;

namespace LeaseLogic.Functions.Models;

public class AnalysisResult
{
    public string AnalysisId { get; set; } = string.Empty;
    public FileInfo FileInfo { get; set; } = new();
    public LeaseAnalysis AnalysisResult { get; set; } = new();
    public string DocumentSummary { get; set; } = string.Empty;
    public TimeSpan ProcessingTime { get; set; }
    public DateTime CompletedAt { get; set; }
}

public class FileInfo
{
    public string FileName { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public DateTime UploadedAt { get; set; }
}

public class LeaseAnalysis
{
    public bool IsLease { get; set; }
    public double Confidence { get; set; }
    public LeaseType LeaseType { get; set; }
    public ContractSummary Summary { get; set; } = new();
    public DetailedLeaseAnalysis LeaseAnalysis { get; set; } = new();
    public List<string> KeyFindings { get; set; } = new();
    public List<string> RiskFactors { get; set; } = new();
    public List<string> Recommendations { get; set; } = new();
    public List<string> ComplianceRequirements { get; set; } = new();
}

public class ContractSummary
{
    public string ContractType { get; set; } = string.Empty;
    public string PrimaryAsset { get; set; } = string.Empty;
    public string ContractPeriod { get; set; } = string.Empty;
    public string MonthlyPayment { get; set; } = string.Empty;
}

public class DetailedLeaseAnalysis
{
    public IdentifiedAssetAnalysis IdentifiedAsset { get; set; } = new();
    public RightToControlAnalysis RightToControl { get; set; } = new();
    public SubstitutionRightsAnalysis SubstantiveSubstitutionRights { get; set; } = new();
}

public class IdentifiedAssetAnalysis
{
    public bool HasIdentifiedAsset { get; set; }
    public string AssetDescription { get; set; } = string.Empty;
    public string AssetSpecificity { get; set; } = string.Empty;
    public List<string> Citations { get; set; } = new();
}

public class RightToControlAnalysis
{
    public bool HasRightToControl { get; set; }
    public List<string> ControlIndicators { get; set; } = new();
    public List<string> Citations { get; set; } = new();
}

public class SubstitutionRightsAnalysis
{
    public bool HasSubstitutionRights { get; set; }
    public string Analysis { get; set; } = string.Empty;
    public List<string> Citations { get; set; } = new();
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum LeaseType
{
    OperatingLease,
    FinanceLease,
    ServiceContract,
    NotApplicable
}