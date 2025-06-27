# LeaseLogic Function App

LeaseLogic契約書解析APIのFunction Appプロジェクトです。

## プロジェクト構造

```
src/LeaseLogic.Functions/
├── Functions/
│   ├── UploadFunctions.cs          # ファイルアップロード・削除API
│   ├── AnalysisFunctions.cs        # 解析開始・状況確認・結果取得API
│   ├── LeaseAnalysisOrchestrator.cs # Durable Functions オーケストレーター
│   └── ActivityFunctions.cs        # Activity Functions (解析処理)
├── Models/                         # データモデル
├── Services/                       # サービス層
├── Program.cs                      # エントリーポイント
├── host.json                       # Function App設定
└── local.settings.json             # ローカル開発設定
```

## 前提条件

- .NET 8 SDK
- Azure Functions Core Tools v4
- Visual Studio 2022 または Visual Studio Code
- Azure CLI または Azure PowerShell

## ローカル開発環境のセットアップ

### 1. プロジェクトのビルド

```bash
cd src/LeaseLogic.Functions
dotnet restore
dotnet build
```

### 2. local.settings.json の設定

`local.settings.json` ファイルに必要な設定値を入力してください：

```json
{
  "Values": {
    "AzureWebJobsStorage": "UseDevelopmentStorage=true",
    "AZURE_OPENAI_ENDPOINT": "https://your-openai-service.openai.azure.com/",
    "AZURE_OPENAI_API_KEY": "your-openai-api-key",
    "DOCUMENT_INTELLIGENCE_ENDPOINT": "https://your-doc-intelligence.cognitiveservices.azure.com/",
    "DOCUMENT_INTELLIGENCE_API_KEY": "your-doc-intelligence-api-key",
    "STORAGE_CONNECTION_STRING": "your-storage-connection-string"
  }
}
```

### 3. Azurite の起動 (ローカルストレージエミュレーター)

```bash
# npm でインストール
npm install -g azurite

# 起動
azurite --silent --location c:\\azurite --debug c:\\azurite\\debug.log
```

### 4. Function App の実行

```bash
func start
```

## Azure へのデプロイ

### 方法1: Azure Functions Core Tools を使用

```bash
# Azure にログイン
az login

# Function App にデプロイ
func azure functionapp publish <your-function-app-name>
```

### 方法2: Visual Studio を使用

1. プロジェクトを右クリック
2. "発行..." を選択
3. Azure Function App を選択
4. 対象のFunction Appを選択して発行

### 方法3: GitHub Actions (推奨)

`.github/workflows/deploy-function-app.yml` を作成：

```yaml
name: Deploy Function App

on:
  push:
    branches: [ main ]
    paths: [ 'src/**' ]

env:
  AZURE_FUNCTIONAPP_NAME: 'your-function-app-name'
  AZURE_FUNCTIONAPP_PACKAGE_PATH: 'src/LeaseLogic.Functions'

jobs:
  deploy:
    runs-on: ubuntu-latest
    steps:
    - uses: actions/checkout@v3

    - name: Setup .NET
      uses: actions/setup-dotnet@v3
      with:
        dotnet-version: '8.0.x'

    - name: Restore dependencies
      run: dotnet restore ${{ env.AZURE_FUNCTIONAPP_PACKAGE_PATH }}

    - name: Build
      run: dotnet build ${{ env.AZURE_FUNCTIONAPP_PACKAGE_PATH }} --configuration Release --no-restore

    - name: Publish
      run: dotnet publish ${{ env.AZURE_FUNCTIONAPP_PACKAGE_PATH }} --configuration Release --no-build --output ./output

    - name: Deploy to Azure Functions
      uses: Azure/functions-action@v1
      with:
        app-name: ${{ env.AZURE_FUNCTIONAPP_NAME }}
        package: './output'
        publish-profile: ${{ secrets.AZURE_FUNCTIONAPP_PUBLISH_PROFILE }}
```

## 設定管理

### Azure での環境変数設定

```bash
# Function Appの設定を更新
az functionapp config appsettings set --name <function-app-name> --resource-group <resource-group> --settings \
    "AZURE_OPENAI_ENDPOINT=https://your-openai-service.openai.azure.com/" \
    "DOCUMENT_INTELLIGENCE_ENDPOINT=https://your-doc-intelligence.cognitiveservices.azure.com/"
```

### Key Vault参照の設定

本番環境では、APIキーはKey Vaultから取得されるように設定されています：

```
AZURE_OPENAI_API_KEY=@Microsoft.KeyVault(VaultName=your-keyvault;SecretName=OpenAI-ApiKey)
DOCUMENT_INTELLIGENCE_API_KEY=@Microsoft.KeyVault(VaultName=your-keyvault;SecretName=DocumentIntelligence-ApiKey)
```

## API エンドポイント

デプロイ後、以下のAPIエンドポイントが利用可能になります：

| エンドポイント | メソッド | 説明 |
|---------------|----------|------|
| `/api/upload-url` | POST | ファイルアップロード用URL生成 |
| `/api/analyze` | POST | 契約書解析開始 |
| `/api/status/{analysisId}` | GET | 解析状況確認 |
| `/api/result/{analysisId}` | GET | 解析結果取得 |
| `/api/file/{fileId}` | DELETE | ファイル削除 |

## 使用例

### 1. ファイルアップロードURL取得

```bash
curl -X POST "https://your-function-app.azurewebsites.net/api/upload-url" \
  -H "Content-Type: application/json" \
  -d '{
    "fileName": "contract.pdf",
    "fileSize": 1024000,
    "contentType": "application/pdf"
  }'
```

### 2. ファイルアップロード

```bash
curl -X PUT "https://storage-sas-url-from-step1" \
  -H "x-ms-blob-type: BlockBlob" \
  --data-binary @contract.pdf
```

### 3. 解析開始

```bash
curl -X POST "https://your-function-app.azurewebsites.net/api/analyze" \
  -H "Content-Type: application/json" \
  -d '{
    "fileId": "uuid-from-step1",
    "fileName": "contract.pdf",
    "fileSize": 1024000,
    "contentType": "application/pdf",
    "options": {
      "language": "ja",
      "detailLevel": "standard"
    }
  }'
```

### 4. 結果確認

```bash
curl "https://your-function-app.azurewebsites.net/api/result/{analysisId}"
```

## トラブルシューティング

### よくある問題

1. **Document Intelligence 接続エラー**
   ```
   Error: Document Intelligence configuration is missing
   ```
   **解決方法**: DOCUMENT_INTELLIGENCE_ENDPOINT と DOCUMENT_INTELLIGENCE_API_KEY が正しく設定されているか確認

2. **OpenAI API エラー**
   ```
   Error: OpenAI API key is invalid
   ```
   **解決方法**: Azure OpenAI Serviceのキーとエンドポイントを確認

3. **Storage 接続エラー**
   ```
   Error: Storage connection string is not configured
   ```
   **解決方法**: AzureWebJobsStorage または STORAGE_CONNECTION_STRING を確認

### ログ確認方法

```bash
# ローカル開発時
func start --verbose

# Azure での確認
az functionapp log tail --name <function-app-name> --resource-group <resource-group>
```

### Application Insights でのモニタリング

Azure ポータルでApplication Insightsを確認：
1. Function App → Application Insights
2. ログクエリ例：

```kusto
traces
| where operation_Name contains "LeaseAnalysis"
| order by timestamp desc
| take 100
```

## パフォーマンス最適化

### Function App 設定

```bash
# 同時実行数の調整
az functionapp config appsettings set --name <function-app-name> --resource-group <resource-group> --settings \
    "FUNCTIONS_WORKER_PROCESS_COUNT=4" \
    "AzureFunctionsJobHost__functionTimeout=00:10:00"
```

### Durable Functions 最適化

`host.json` で調整可能：

```json
{
  "extensions": {
    "durableTask": {
      "maxConcurrentActivityFunctions": 10,
      "maxConcurrentOrchestratorFunctions": 10
    }
  }
}
```

## セキュリティ

### 認証設定

Function App レベルでの認証を有効化：

```bash
az functionapp auth update --name <function-app-name> --resource-group <resource-group> \
    --enabled true \
    --action LoginWithAzureActiveDirectory \
    --aad-client-id <your-client-id>
```

### CORS 設定

```bash
az functionapp cors add --name <function-app-name> --resource-group <resource-group> \
    --allowed-origins "https://your-frontend-domain.com"
```

## 次のステップ

1. 実際の会計基準文書のアップロード
2. OpenAI Assistants APIの設定
3. フロントエンドアプリケーションとの統合
4. 監視・アラートの設定