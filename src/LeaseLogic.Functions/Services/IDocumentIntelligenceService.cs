using LeaseLogic.Functions.Models;

namespace LeaseLogic.Functions.Services;

public interface IDocumentIntelligenceService
{
    /// <summary>
    /// ドキュメントを解析してParsedDocumentを返す
    /// </summary>
    Task<ParsedDocument> ParseDocumentAsync(string fileId, Stream documentStream);

    /// <summary>
    /// ParsedDocumentから構造化コンテンツを抽出
    /// </summary>
    Task<StructuredContent> ExtractStructuredContentAsync(ParsedDocument parsedDocument);
}