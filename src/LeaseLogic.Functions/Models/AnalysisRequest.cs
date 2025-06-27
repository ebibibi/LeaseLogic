using System.ComponentModel.DataAnnotations;

namespace LeaseLogic.Functions.Models;

public class AnalysisRequest
{
    [Required]
    public string FileId { get; set; } = string.Empty;
    
    [Required]
    public string FileName { get; set; } = string.Empty;
    
    [Required]
    public long FileSize { get; set; }
    
    [Required]
    public string ContentType { get; set; } = string.Empty;
    
    public AnalysisOptions Options { get; set; } = new();
}

public class AnalysisOptions
{
    public string Language { get; set; } = "ja";
    public string DetailLevel { get; set; } = "standard";
    public string? NotificationUrl { get; set; }
}

public class UploadUrlRequest
{
    [Required]
    public string FileName { get; set; } = string.Empty;
    
    [Required]
    public long FileSize { get; set; }
    
    [Required]
    public string ContentType { get; set; } = string.Empty;
}

public class UploadUrlResponse
{
    public string FileId { get; set; } = string.Empty;
    public string UploadUrl { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public long MaxFileSize { get; set; } = 52428800; // 50MB
}