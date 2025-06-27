using LeaseLogic.Functions.Models;

namespace LeaseLogic.Functions.Services;

public interface IOpenAIService
{
    /// <summary>
    /// OpenAI Assistant を初期化
    /// </summary>
    Task InitializeAsync();

    /// <summary>
    /// リース分類分析を実行
    /// </summary>
    Task<LeaseClassification> ClassifyLeaseAsync(StructuredContent content);

    /// <summary>
    /// 会計基準参照文書をアップロード
    /// </summary>
    Task<string> UploadAccountingStandardAsync(Stream documentStream, string fileName);

    /// <summary>
    /// Assistant とベクターストアを作成
    /// </summary>
    Task<string> CreateLeaseAnalysisAssistantAsync();

    /// <summary>
    /// ベクターストアIDを取得
    /// </summary>
    Task<string> GetAccountingStandardsVectorStoreIdAsync();
}