# LeaseLogic デプロイメントガイド

このドキュメントでは、LeaseLogic API システムの完全なデプロイメント手順を説明します。

## 概要

LeaseLogic は以下のコンポーネントで構成されています：

1. **Infrastructure (Bicep)**: Azure リソースの定義
2. **Function App (.NET 8)**: API とビジネスロジック
3. **CI/CD (GitHub Actions)**: 自動デプロイメント

## 🚀 クイックスタート

### 1. 前提条件の確認

- Azure サブスクリプション
- Azure CLI または Azure PowerShell
- .NET 8 SDK
- GitHub アカウント (CI/CD用)

### 2. Azure インフラストラクチャのデプロイ

```bash
# 1. Azure にログイン
az login

# 2. リソースグループ作成
az group create --name "leaselogic-rg" --location "japaneast"

# 3. インフラストラクチャデプロイ
cd infrastructure
./deploy.sh -g "leaselogic-rg"
```

### 3. Function App のデプロイ

```bash
# 1. プロジェクトのビルド
cd src/LeaseLogic.Functions
dotnet build --configuration Release

# 2. Azure Functions Core Tools でデプロイ
func azure functionapp publish <your-function-app-name>
```

## 📋 詳細デプロイメント手順

### Phase 1: インフラストラクチャ

#### 1.1 パラメータの設定

`infrastructure/parameters.json` を編集：

```json
{
  "parameters": {
    "environment": {
      "value": "prod"
    },
    "location": {
      "value": "japaneast"
    },
    "openAILocation": {
      "value": "eastus"
    }
  }
}
```

#### 1.2 Bicep デプロイメント

```bash
# PowerShell の場合
.\infrastructure\deploy.ps1 -ResourceGroupName "leaselogic-prod-rg" -Environment "prod"

# Bash の場合
./infrastructure/deploy.sh -g "leaselogic-prod-rg" -e "prod"
```

#### 1.3 デプロイメント結果の確認

デプロイ完了後、`deployment-outputs.json` ファイルに以下の情報が保存されます：

- Function App 名
- Storage Account 名
- OpenAI Service エンドポイント
- Document Intelligence エンドポイント
- Key Vault URI

### Phase 2: 会計基準文書の準備

#### 2.1 参照文書のアップロード

```bash
# Storage Account に会計基準文書をアップロード
az storage blob upload \
  --account-name <storage-account-name> \
  --container-name standards \
  --file ./documents/IFRS-16.pdf \
  --name ifrs16.pdf

az storage blob upload \
  --account-name <storage-account-name> \
  --container-name standards \
  --file ./documents/ASC-842.pdf \
  --name asc842.pdf
```

#### 2.2 OpenAI Assistant の設定

Function App デプロイ後に実行するセットアップスクリプト（今後実装予定）：

```bash
# Assistant の作成と参照文書の関連付け
dotnet run --project ./setup -- create-assistant
```

### Phase 3: Function App デプロイメント

#### 3.1 ローカル設定の確認

`src/LeaseLogic.Functions/local.settings.json` で設定を確認：

```json
{
  "Values": {
    "AZURE_OPENAI_ENDPOINT": "https://your-openai.openai.azure.com/",
    "DOCUMENT_INTELLIGENCE_ENDPOINT": "https://your-docint.cognitiveservices.azure.com/"
  }
}
```

#### 3.2 手動デプロイ

```bash
cd src/LeaseLogic.Functions

# ビルドと発行
dotnet publish --configuration Release --output ./bin/publish

# Azure へデプロイ
func azure functionapp publish <function-app-name> --publish-local-settings
```

### Phase 4: CI/CD セットアップ

#### 4.1 GitHub Secrets の設定

GitHub リポジトリの Settings > Secrets and variables > Actions で設定：

| Secret Name | Description | 取得方法 |
|-------------|-------------|----------|
| `AZURE_FUNCTIONAPP_PUBLISH_PROFILE` | Function App の発行プロファイル | Azure ポータルから取得 |

発行プロファイルの取得方法：

```bash
az functionapp deployment list-publishing-profiles \
  --name <function-app-name> \
  --resource-group <resource-group> \
  --xml
```

#### 4.2 ワークフローの有効化

`.github/workflows/deploy-function-app.yml` の `AZURE_FUNCTIONAPP_NAME` を更新：

```yaml
env:
  AZURE_FUNCTIONAPP_NAME: 'your-actual-function-app-name'
```

## 🔧 設定とカスタマイズ

### Function App 設定

重要なアプリケーション設定：

| 設定名 | 説明 | 例 |
|--------|------|-----|
| `AZURE_OPENAI_ENDPOINT` | OpenAI Service エンドポイント | `https://xxx.openai.azure.com/` |
| `DOCUMENT_INTELLIGENCE_ENDPOINT` | Document Intelligence エンドポイント | `https://xxx.cognitiveservices.azure.com/` |
| `KEY_VAULT_URI` | Key Vault URI | `https://xxx.vault.azure.net/` |

### Key Vault シークレット

以下のシークレットが自動的に設定されます：

- `OpenAI-ApiKey`: Azure OpenAI Service の API キー
- `DocumentIntelligence-ApiKey`: Document Intelligence の API キー

### Durable Functions 設定

`host.json` でパフォーマンス調整：

```json
{
  "extensions": {
    "durableTask": {
      "maxConcurrentActivityFunctions": 10,
      "maxConcurrentOrchestratorFunctions": 10,
      "extendedSessionsEnabled": false
    }
  }
}
```

## 🧪 テストとヘルスチェック

### API テスト

```bash
# ヘルスチェック
curl "https://<function-app-name>.azurewebsites.net"

# アップロード URL 生成テスト
curl -X POST "https://<function-app-name>.azurewebsites.net/api/upload-url" \
  -H "Content-Type: application/json" \
  -d '{"fileName":"test.pdf","fileSize":1000,"contentType":"application/pdf"}'
```

### ログ確認

```bash
# Function App ログのストリーミング
az functionapp log tail --name <function-app-name> --resource-group <resource-group>

# Application Insights でのクエリ
az monitor app-insights query \
  --app <app-insights-name> \
  --analytics-query "traces | where message contains 'LeaseAnalysis' | take 10"
```

## 🔒 セキュリティ設定

### 認証の有効化

```bash
# Azure AD 認証を有効化
az functionapp auth update \
  --name <function-app-name> \
  --resource-group <resource-group> \
  --enabled true \
  --action LoginWithAzureActiveDirectory \
  --aad-client-id <your-app-registration-id>
```

### CORS 設定

```bash
# 許可するオリジンの設定
az functionapp cors add \
  --name <function-app-name> \
  --resource-group <resource-group> \
  --allowed-origins "https://your-frontend-domain.com"
```

### ネットワークセキュリティ

```bash
# IP 制限の設定（必要に応じて）
az functionapp config access-restriction add \
  --name <function-app-name> \
  --resource-group <resource-group> \
  --rule-name "AllowOfficeIP" \
  --action Allow \
  --ip-address "203.0.113.0/24" \
  --priority 100
```

## 📊 監視とアラート

### Application Insights アラート

```bash
# エラー率のアラート作成
az monitor metrics alert create \
  --name "LeaseLogic-HighErrorRate" \
  --resource-group <resource-group> \
  --scopes "/subscriptions/<subscription-id>/resourceGroups/<resource-group>/providers/Microsoft.Web/sites/<function-app-name>" \
  --condition "count exceptions/requests > 0.1" \
  --description "High error rate in LeaseLogic Function App"
```

### ダッシュボード作成

Azure ポータルで以下のメトリクスを監視：

- Function 実行回数
- 実行時間
- エラー率
- Document Intelligence API 使用量
- OpenAI API 使用量

## 🚨 トラブルシューティング

### よくある問題と解決方法

1. **Function App が起動しない**
   ```bash
   # ログを確認
   az functionapp log tail --name <function-app-name> --resource-group <resource-group>
   
   # 設定を確認
   az functionapp config appsettings list --name <function-app-name> --resource-group <resource-group>
   ```

2. **OpenAI API 接続エラー**
   ```bash
   # Key Vault のアクセス権限を確認
   az keyvault show --name <key-vault-name>
   
   # Function App の Managed Identity を確認
   az functionapp identity show --name <function-app-name> --resource-group <resource-group>
   ```

3. **Document Intelligence エラー**
   ```bash
   # API キーとエンドポイントを確認
   az cognitiveservices account show --name <doc-intelligence-name> --resource-group <resource-group>
   ```

### デバッグ用コマンド

```bash
# Function App の詳細情報
az functionapp show --name <function-app-name> --resource-group <resource-group>

# 最近のデプロイメント履歴
az functionapp deployment source show --name <function-app-name> --resource-group <resource-group>

# リソースの正常性チェック
az resource list --resource-group <resource-group> --output table
```

## 📈 本番環境の考慮事項

### スケーリング

```bash
# Auto-scaling の設定
az functionapp plan update \
  --name <app-service-plan-name> \
  --resource-group <resource-group> \
  --max-burst 20
```

### バックアップ

```bash
# Storage Account のバックアップ設定
az storage account update \
  --name <storage-account-name> \
  --resource-group <resource-group> \
  --enable-versioning true
```

### コスト最適化

- 使用量に応じた Consumption Plan への移行検討
- 不要なログの保持期間調整
- リソースのタグ付けによるコスト追跡

## 🔄 アップデート手順

### Function App の更新

1. コードの修正・テスト
2. GitHub への Push (自動デプロイメント)
3. 本番環境での動作確認

### インフラストラクチャの更新

1. Bicep テンプレートの修正
2. テスト環境での検証
3. 本番環境への段階的適用

## 📞 サポート

問題が発生した場合：

1. このドキュメントのトラブルシューティングセクションを確認
2. Azure ポータルでリソースの状態を確認
3. Application Insights でエラーログを確認
4. 必要に応じて開発チームに連絡