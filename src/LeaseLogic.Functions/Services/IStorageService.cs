using Azure.Storage.Blobs.Models;
using LeaseLogic.Functions.Models;

namespace LeaseLogic.Functions.Services;

public interface IStorageService
{
    /// <summary>
    /// ファイルアップロード用のSAS URLを生成
    /// </summary>
    Task<UploadUrlResponse> GenerateUploadUrlAsync(UploadUrlRequest request);

    /// <summary>
    /// ファイルのメタデータを保存
    /// </summary>
    Task SaveFileMetadataAsync(string fileId, string fileName, long fileSize, string contentType);

    /// <summary>
    /// ファイルの存在確認
    /// </summary>
    Task<bool> FileExistsAsync(string fileId);

    /// <summary>
    /// ファイルのストリームを取得
    /// </summary>
    Task<Stream> GetFileStreamAsync(string fileId);

    /// <summary>
    /// ファイルを削除
    /// </summary>
    Task<bool> DeleteFileAsync(string fileId);

    /// <summary>
    /// 解析結果を保存
    /// </summary>
    Task SaveAnalysisResultAsync(string analysisId, AnalysisResult result);

    /// <summary>
    /// 解析結果を取得
    /// </summary>
    Task<AnalysisResult?> GetAnalysisResultAsync(string analysisId);

    /// <summary>
    /// 解析メタデータを保存
    /// </summary>
    Task SaveAnalysisMetadataAsync(string analysisId, AnalysisStatus status);

    /// <summary>
    /// 解析メタデータを取得
    /// </summary>
    Task<AnalysisStatus?> GetAnalysisMetadataAsync(string analysisId);

    /// <summary>
    /// 解析メタデータを更新
    /// </summary>
    Task UpdateAnalysisMetadataAsync(string analysisId, AnalysisStatus status);
}