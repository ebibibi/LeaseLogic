# LeaseLogic API 設計書

## システム概要

契約書を解析し、新会計基準（IFRS 16/ASC 842）に基づいてリース契約かどうかを判定するAPIシステム。
Azure Durable Functionsを活用して、長時間実行される契約書解析処理を効率的に処理する。

## アーキテクチャ設計

### システム構成（Durable Functions + Blob URL方式）

```
Client Application
    ↓ 1. POST /api/upload-url
HTTP Trigger Function (Upload URL Generator)
    ↓ Generate SAS URL
Azure Blob Storage
    ↑ 2. PUT {SAS URL} (直接アップロード)
Client Application
    ↓ 3. POST /api/analyze (fileId)
HTTP Trigger Function (Analysis Starter)
    ↓ Start Orchestration
Durable Functions Orchestrator
    ↓ Call Activities in sequence
┌─────────────────────────────────┐
│ Activity Functions              │
│ ├─ DocumentParser               │
│ ├─ TextExtractor               │
│ ├─ LeaseClassifier (AI)        │
│ └─ ReportGenerator             │
└─────────────────────────────────┘
    ↓ Store Results
Azure Storage (Blob + Table)
    ↑ 4. Poll Status
Client Application
    ↓ GET /api/status/{analysisId}
HTTP Trigger Function (Status Check)
    ↓ GET /api/result/{analysisId}
HTTP Trigger Function (Result Retrieval)
```

## 技術スタック

- **Runtime**: Azure Functions (.NET 8)
- **Orchestration**: Azure Durable Functions
- **Storage**: Azure Blob Storage (documents/results), Azure Table Storage (metadata)
- **AI/ML**: Azure AI Document Intelligence (文書解析) + Azure OpenAI Service (意味理解・判定)
- **Monitoring**: Application Insights
- **Authentication**: Azure AD

## AI処理アプローチ（ハイブリッド方式）

**選択理由**: Document Intelligenceの高精度な文書構造解析と、OpenAI Serviceの高度な意味理解を組み合わせることで、最適な契約書解析を実現。

### 処理フロー
```
PDF契約書 → Document Intelligence → 構造化データ → OpenAI Service → リース判定
```

### 各段階の役割
1. **Document Intelligence**: 
   - 高精度なOCR・レイアウト解析
   - テーブル、リスト、セクションの構造化
   - メタデータ（ページ、座標）の抽出

2. **OpenAI Service**:
   - 契約内容の意味理解
   - IFRS 16/ASC 842基準に基づく判定
   - 複雑な契約条項の解釈

## APIエンドポイント設計

### 1. アップロードURL取得

```http
POST /api/upload-url
Content-Type: application/json
Authorization: Bearer {token}

Request Body:
{
  "fileName": "contract001.pdf",
  "fileSize": 1024000,
  "contentType": "application/pdf"
}

Response:
{
  "fileId": "uuid-generated-id",
  "uploadUrl": "https://storage.blob.core.windows.net/documents/uuid?sp=w&sig=...",
  "expiresAt": "2024-01-01T01:00:00Z",
  "maxFileSize": 52428800
}
```

### 2. 契約書解析開始

```http
POST /api/analyze
Content-Type: application/json
Authorization: Bearer {token}

Request Body:
{
  "fileId": "uuid-from-upload-url",
  "options": {
    "language": "ja",
    "detailLevel": "standard|detailed",
    "notificationUrl": "https://client.com/webhook"
  }
}

Response:
{
  "analysisId": "durable-function-instance-id",
  "status": "Running",
  "statusUrl": "/api/status/{analysisId}",
  "resultUrl": "/api/result/{analysisId}",
  "estimatedDuration": "5-10分",
  "createdTime": "2024-01-01T00:00:00Z"
}
```

### 3. 解析状況確認

```http
GET /api/status/{analysisId}
Authorization: Bearer {token}

Response:
{
  "analysisId": "uuid",
  "status": "Running|Completed|Failed|Terminated",
  "fileInfo": {
    "fileName": "contract001.pdf",
    "fileSize": 1024000,
    "uploadedAt": "2024-01-01T00:00:00Z"
  },
  "progress": {
    "currentStep": "DocumentParsing|TextExtraction|AIAnalysis|ReportGeneration",
    "percentage": 75,
    "message": "AI分析実行中...",
    "estimatedRemaining": "00:02:30"
  },
  "result": null | AnalysisResult,
  "createdTime": "2024-01-01T00:00:00Z",
  "lastUpdatedTime": "2024-01-01T00:05:00Z"
}
```

### 4. 結果取得

```http
GET /api/result/{analysisId}
Authorization: Bearer {token}

Response:
{
  "analysisId": "uuid",
  "fileInfo": {
    "fileName": "contract001.pdf",
    "fileSize": 1024000
  },
  "analysisResult": {
    "isLease": true,
    "confidence": 0.89,
    "leaseType": "OperatingLease|FinanceLease|ServiceContract|NotApplicable",
    "summary": {
      "contractType": "不動産賃貸借契約",
      "primaryAsset": "オフィス建物",
      "contractPeriod": "36ヶ月",
      "monthlyPayment": "1,000,000円"
    },
    "leaseAnalysis": {
      "identifiedAsset": {
        "hasIdentifiedAsset": true,
        "assetDescription": "東京都渋谷区のオフィスビル3階部分",
        "assetSpecificity": "特定の物理的資産"
      },
      "rightToControl": {
        "hasRightToControl": true,
        "controlIndicators": [
          "資産の使用方法を指示する権利",
          "資産からの経済的便益を享受する権利"
        ]
      },
      "substantiveSubstitutionRights": {
        "hasSubstitutionRights": false,
        "analysis": "貸手に実質的な代替権はない"
      }
    },
    "keyFindings": [
      "特定された資産の使用権が存在",
      "借手が資産の使用を指示し制御する権利を有する",
      "契約期間が12ヶ月を超える"
    ],
    "riskFactors": [
      "リース期間の延長オプションが存在",
      "中途解約条項の解釈が必要"
    ],
    "recommendations": [
      "IFRS 16 / ASC 842の適用対象として認識",
      "使用権資産とリース負債の計上が必要",
      "契約開始日における初期測定の実施"
    ],
    "complianceRequirements": [
      "使用権資産の認識と測定",
      "リース負債の計算と計上",
      "注記事項の開示準備"
    ]
  },
  "documentSummary": "東京都渋谷区所在のオフィスビル賃貸借契約。契約期間36ヶ月、月額賃料100万円。新会計基準の適用対象となるリース契約。",
  "processingTime": "00:07:30",
  "completedAt": "2024-01-01T00:07:30Z"
}
```

### 5. ファイル削除（オプション）

```http
DELETE /api/file/{fileId}
Authorization: Bearer {token}

Response:
{
  "fileId": "uuid",
  "deleted": true,
  "deletedAt": "2024-01-01T00:10:00Z"
}
```

## Durable Functions詳細設計

### Orchestrator Function: `LeaseAnalysisOrchestrator`

```csharp
[FunctionName("LeaseAnalysisOrchestrator")]
public static async Task<AnalysisResult> RunOrchestrator(
    [OrchestrationTrigger] IDurableOrchestrationContext context)
{
    var input = context.GetInput<AnalysisRequest>();
    
    // Phase 1: Document Intelligence による構造化解析
    context.SetCustomStatus(new { currentStep = "DocumentParsing", progress = 15, message = "Document Intelligence解析開始" });
    var parsedDocument = await context.CallActivityAsync<ParsedDocument>(
        "DocumentParser", input.FileId);
    
    // Phase 2: 契約書固有の構造認識と前処理
    context.SetCustomStatus(new { currentStep = "ContentStructuring", progress = 35, message = "契約書構造分析中" });
    var structuredContent = await context.CallActivityAsync<StructuredContent>(
        "ContentStructurer", parsedDocument);
    
    // Phase 3: OpenAI による意味理解とリース判定
    context.SetCustomStatus(new { currentStep = "AIAnalysis", progress = 70, message = "OpenAI Service リース判定実行中" });
    var leaseClassification = await context.CallActivityAsync<LeaseClassification>(
        "LeaseClassifier", structuredContent);
    
    // Phase 4: 最終レポート生成
    context.SetCustomStatus(new { currentStep = "ReportGeneration", progress = 90, message = "総合レポート生成中" });
    var result = await context.CallActivityAsync<AnalysisResult>(
        "ReportGenerator", new { input, structuredContent, leaseClassification });
    
    context.SetCustomStatus(new { currentStep = "Completed", progress = 100, message = "ハイブリッド解析完了" });
    return result;
}
```

### Activity Functions

#### 1. DocumentParser
```csharp
[FunctionName("DocumentParser")]
public static async Task<ParsedDocument> ParseDocument(
    [ActivityTrigger] string fileId,
    [Blob("documents/{fileId}", FileAccess.Read)] Stream documentStream,
    ILogger log)
{
    // Azure AI Document Intelligence使用 (Phase 1)
    // Blob StorageからPDF/Word取得 → Document Intelligence API
    // 高精度OCR、レイアウト解析、テーブル・リスト構造化
    // ページごとの座標情報付きでテキスト・構造データを抽出
    
    var documentIntelligenceClient = new DocumentIntelligenceClient();
    var analyzedDocument = await documentIntelligenceClient.AnalyzeDocumentAsync(
        "prebuilt-contract", documentStream);
    
    return new ParsedDocument
    {
        StructuredContent = analyzedDocument.Documents,
        Pages = analyzedDocument.Pages,
        Tables = analyzedDocument.Tables,
        Paragraphs = analyzedDocument.Paragraphs
    };
}
```

#### 2. ContentStructurer
```csharp
[FunctionName("ContentStructurer")]
public static async Task<StructuredContent> StructureContent(
    [ActivityTrigger] ParsedDocument document,
    ILogger log)
{
    // Document Intelligence結果の後処理
    // 契約書固有のセクション識別（当事者情報、契約条件、支払条件等）
    // テーブルデータの意味づけ
    // 重要な契約条項の抽出と分類
    
    return new StructuredContent
    {
        ContractParties = ExtractParties(document),
        AssetDetails = ExtractAssetInfo(document),
        PaymentTerms = ExtractPaymentTerms(document),
        ContractPeriod = ExtractContractPeriod(document),
        SpecialClauses = ExtractSpecialClauses(document),
        RawContent = document.GetFullText()
    };
}
```

#### 3. LeaseClassifier
```csharp
[FunctionName("LeaseClassifier")]
public static async Task<LeaseClassification> ClassifyLease(
    [ActivityTrigger] StructuredContent content,
    ILogger log)
{
    // Azure OpenAI Service使用 (Phase 2)
    // 構造化されたコンテンツを基にリース判定
    // IFRS 16 / ASC 842の判定基準を適用:
    
    // 1. 特定された資産の識別
    var identifiedAssetAnalysis = await AnalyzeIdentifiedAsset(content);
    
    // 2. 使用権の制御権の評価  
    var controlRightsAnalysis = await AnalyzeControlRights(content);
    
    // 3. 実質的な代替権の分析
    var substitutionRightsAnalysis = await AnalyzeSubstitutionRights(content);
    
    // OpenAI GPT-4で総合判定
    var leaseClassification = await openAIClient.GetChatCompletionsAsync(
        BuildLeaseAnalysisPrompt(content, identifiedAssetAnalysis, 
            controlRightsAnalysis, substitutionRightsAnalysis));
    
    return ParseLeaseClassificationResponse(leaseClassification);
}
```

#### 4. ReportGenerator
```csharp
[FunctionName("ReportGenerator")]
public static async Task<AnalysisResult> GenerateReport(
    [ActivityTrigger] object input,
    [Blob("results/{analysisId}.json", FileAccess.Write)] Stream resultStream,
    ILogger log)
{
    // 最終結果の生成と保存
    // 構造化されたJSON形式でのレポート作成
    // Blob Storageへの結果保存
}
```

## データフロー詳細

### 1. ファイルアップロードフロー

```
Client Request → Upload URL API → SAS URL Generation
                                     ↓
Client → Direct Blob Upload (PUT) → Blob Storage
                                     ↓
                                File Metadata Saved
```

### 2. ハイブリッド解析実行フロー

```
Client Request → Analysis API → Durable Function Orchestrator
                                     ↓
                            Phase 1: DocumentParser
                     (Blob Storage → Document Intelligence API)
                        高精度OCR + 構造化解析
                                     ↓
                            Phase 2: ContentStructurer  
                        (契約書固有の構造認識・前処理)
                         セクション識別 + メタデータ抽出
                                     ↓
                            Phase 3: LeaseClassifier
                      (OpenAI Service → IFRS/ASC判定)
                        構造化データ + 意味理解 + リース判定
                                     ↓
                            Phase 4: ReportGenerator
                        (統合結果 → Blob Storage保存)
```

### ハイブリッド方式の利点

1. **高精度**: Document Intelligenceの正確な文書解析
2. **高度理解**: OpenAIの意味理解とコンテキスト判定
3. **構造化**: 段階的な処理による品質向上
4. **デバッグ容易**: 各フェーズの結果を個別に確認可能
5. **コスト効率**: 必要な部分のみでOpenAI APIを使用

### 3. 結果取得フロー

```
Client Status Poll → Status API → Orchestrator Status
                                     ↓
                            Progress Information
                                     ↓
Client Result Request → Result API → Stored Analysis Result
```

## データモデル

### AnalysisRequest
```csharp
public class AnalysisRequest
{
    public string FileId { get; set; }
    public string FileName { get; set; }
    public long FileSize { get; set; }
    public string ContentType { get; set; }
    public AnalysisOptions Options { get; set; }
}

public class AnalysisOptions
{
    public string Language { get; set; } = "ja";
    public string DetailLevel { get; set; } = "standard";
    public string NotificationUrl { get; set; }
}
```

### AnalysisResult
```csharp
public class AnalysisResult
{
    public bool IsLease { get; set; }
    public double Confidence { get; set; }
    public LeaseType LeaseType { get; set; }
    public List<string> KeyFindings { get; set; }
    public List<string> RiskFactors { get; set; }
    public List<string> Recommendations { get; set; }
    public ExtractedData ExtractedData { get; set; }
    public string DocumentSummary { get; set; }
    public TimeSpan ProcessingTime { get; set; }
}
```

## エラーハンドリング・再試行戦略

### 自動再試行設定
- **DocumentParser**: 最大3回、指数バックオフ
- **TextExtractor**: 最大2回
- **LeaseClassifier**: 最大5回（AI API制限対応）
- **ReportGenerator**: 最大2回

### エラー種別対応
- **AI API制限**: 指数バックオフで再試行
- **ファイル破損**: 即座に失敗通知
- **一時的障害**: 自動再試行後、手動介入オプション

## セキュリティ

### 認証・認可
- Azure AD統合
- API Key代替オプション
- リクエスト元IP制限

### データ保護
- ファイル暗号化保存
- 処理後の自動削除（設定可能期間）
- 個人情報自動マスキング
- 監査ログ記録

## パフォーマンス・スケーラビリティ

### 処理能力
- **同時処理**: 最大50インスタンス
- **処理時間**: 1文書あたり3-15分
- **ファイルサイズ**: 最大10MB
- **自動スケーリング**: 負荷に応じて動的調整

### リソース管理
- Function App: Premium Plan使用
- Storage: Hot tier（処理中）→ Cool tier（保存）
- AI Services: 使用量に応じた従量課金

## 監視・ログ

### Application Insights統合
- 処理時間追跡
- エラー率監視
- カスタムメトリクス（精度、処理ファイル数）
- アラート設定

### ログレベル
- **INFO**: 処理開始・完了
- **WARN**: 再試行発生
- **ERROR**: 処理失敗
- **DEBUG**: 詳細処理ログ

## デプロイメント

### Infrastructure as Code
```bicep
// Function App + Storage + AI Services
// Application Insights
// Key Vault（機密情報管理）
```

### CI/CD Pipeline
1. GitHub Actions trigger
2. テスト実行
3. Bicepデプロイ
4. Function Appデプロイ
5. 統合テスト

## 運用

### 日次運用
- 処理状況監視
- エラー率確認
- AI APIクォータ監視

### メンテナンス
- 古い結果データ削除
- ログローテーション
- AI モデル更新対応