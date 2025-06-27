# LeaseLogic Deployment Scripts

このディレクトリには、LeaseLogic全体システムを自動デプロイするためのスクリプトが含まれています。

## 📁 含まれるファイル

- `deploy-all.sh` - Bash版自動デプロイスクリプト
- `deploy-all.ps1` - PowerShell版自動デプロイスクリプト
- `README.md` - このファイル

## 🚀 クイックスタート

### Bash版 (Linux/macOS/WSL)

```bash
# 基本的な使用方法
./scripts/deploy-all.sh -g "leaselogic-dev-rg"

# 本番環境デプロイ
./scripts/deploy-all.sh -g "leaselogic-prod-rg" -e "prod" -l "eastus"

# インフラのみスキップ（Function Appのみ再デプロイ）
./scripts/deploy-all.sh -g "leaselogic-dev-rg" --skip-infrastructure
```

### PowerShell版 (Windows/Cross-platform)

```powershell
# 基本的な使用方法
.\scripts\deploy-all.ps1 -ResourceGroupName "leaselogic-dev-rg"

# 本番環境デプロイ
.\scripts\deploy-all.ps1 -ResourceGroupName "leaselogic-prod-rg" -Environment "prod" -Location "eastus"

# インフラのみスキップ（Function Appのみ再デプロイ）
.\scripts\deploy-all.ps1 -ResourceGroupName "leaselogic-dev-rg" -SkipInfrastructure
```

## 📋 前提条件

### 共通
- Azure サブスクリプションへのアクセス権限
- .NET 8 SDK
- Azure Functions Core Tools (推奨)

### Bash版
- Azure CLI
- `az login` でログイン済み
- `jq` コマンド (JSON処理用)

### PowerShell版
- Azure PowerShell モジュール
- `Connect-AzAccount` でログイン済み

### Azure PowerShell モジュールのインストール

```powershell
# 必要モジュールのインストール
Install-Module Az.Accounts, Az.Resources, Az.Storage, Az.Websites, Az.Functions -Scope CurrentUser
```

## 🛠️ パラメータオプション

### Bash版

| パラメータ | 説明 | デフォルト値 |
|-----------|------|-------------|
| `-g, --resource-group` | リソースグループ名 (必須) | - |
| `-l, --location` | Azureリージョン | japaneast |
| `-e, --environment` | 環境名 | dev |
| `--skip-infrastructure` | インフラ構築をスキップ | false |
| `--skip-function-app` | Function App デプロイをスキップ | false |
| `--cleanup-on-error` | エラー時にリソースグループを削除 | false |
| `-h, --help` | ヘルプ表示 | - |

### PowerShell版

| パラメータ | 説明 | デフォルト値 |
|-----------|------|-------------|
| `-ResourceGroupName` | リソースグループ名 (必須) | - |
| `-Location` | Azureリージョン | japaneast |
| `-Environment` | 環境名 | dev |
| `-SkipInfrastructure` | インフラ構築をスキップ | false |
| `-SkipFunctionApp` | Function App デプロイをスキップ | false |
| `-CleanupOnError` | エラー時にリソースグループを削除 | false |
| `-Help` | ヘルプ表示 | - |

## 🔄 デプロイメントステップ

スクリプトは以下の順序で処理を実行します：

### Step 1: Prerequisites Check
- Azure CLI/PowerShell の確認
- 認証状態の確認
- .NET SDK の確認
- Azure Functions Core Tools の確認

### Step 2: Infrastructure Deployment
- リソースグループの作成（存在しない場合）
- Bicep テンプレートによるAzureリソースの作成
- デプロイメント結果の保存

### Step 3: Function App Deployment
- .NET プロジェクトのビルド
- NuGet パッケージの復元
- プロジェクトの発行
- Azure へのデプロイ

### Step 4: Post-deployment Setup
- ヘルスチェック
- API エンドポイントの確認
- アクセス情報の表示

### Step 5: Next Steps
- 後続作業の案内
- モニタリング情報の提供

## 📊 出力ファイル

デプロイ完了後、以下のファイルが生成されます：

### infrastructure/deployment-outputs.json
```json
{
    "functionAppName": "leaselogic-dev-abc123-func",
    "storageAccountName": "leaselogicdevabc123storage",
    "openAIEndpoint": "https://leaselogic-dev-abc123-openai.openai.azure.com/",
    "documentIntelligenceEndpoint": "https://leaselogic-dev-abc123-docint.cognitiveservices.azure.com/",
    "keyVaultUri": "https://leaselogic-dev-abc123-kv.vault.azure.net/",
    "resourceGroupName": "leaselogic-dev-rg"
}
```

### deployment-summary.json
```json
{
    "deploymentDate": "2024-01-01T12:00:00Z",
    "resourceGroupName": "leaselogic-dev-rg",
    "location": "japaneast",
    "environment": "dev",
    "functionAppName": "leaselogic-dev-abc123-func",
    "functionAppUrl": "https://leaselogic-dev-abc123-func.azurewebsites.net",
    "status": "completed"
}
```

## 🧪 使用例

### 開発環境のセットアップ
```bash
# 開発環境の完全デプロイ
./scripts/deploy-all.sh -g "leaselogic-dev-rg"
```

### 本番環境のセットアップ
```bash
# 本番環境の完全デプロイ（東日本リージョン）
./scripts/deploy-all.sh -g "leaselogic-prod-rg" -e "prod" -l "eastus"
```

### Function Appのみ再デプロイ
```bash
# コード修正後のFunction App更新
./scripts/deploy-all.sh -g "leaselogic-dev-rg" --skip-infrastructure
```

### エラー時の自動クリーンアップ
```bash
# テスト環境での失敗時自動削除
./scripts/deploy-all.sh -g "leaselogic-test-rg" --cleanup-on-error
```

## 🚨 トラブルシューティング

### よくある問題

1. **Azure CLI/PowerShell 認証エラー**
   ```bash
   # 再ログイン
   az login
   # または
   Connect-AzAccount
   ```

2. **.NET SDK が見つからない**
   ```bash
   # .NET 8 SDK をインストール
   # https://dotnet.microsoft.com/download/dotnet/8.0
   ```

3. **Azure Functions Core Tools がない**
   ```bash
   # npm でインストール
   npm install -g azure-functions-core-tools@4 --unsafe-perm true
   ```

4. **権限エラー**
   - Azureサブスクリプションの共同作成者権限が必要
   - リソースグループの作成権限が必要

### ログ確認

スクリプト実行中にエラーが発生した場合：

1. **スクリプトの詳細出力を確認**
2. **Azure ポータルでリソースの状態を確認**
3. **Application Insights でエラーログを確認**

### デバッグ用コマンド

```bash
# リソースグループの確認
az group show --name "leaselogic-dev-rg"

# Function App の状態確認
az functionapp show --name <function-app-name> --resource-group <resource-group>

# デプロイメント履歴の確認
az deployment group list --resource-group <resource-group> --output table
```

## 🔧 カスタマイズ

### 環境固有の設定

`infrastructure/parameters.json` を環境ごとに作成：

```json
{
  "parameters": {
    "environment": {
      "value": "prod"
    },
    "location": {
      "value": "eastus"
    },
    "openAILocation": {
      "value": "eastus"
    }
  }
}
```

### スクリプトの拡張

独自の処理を追加する場合：

1. `deploy-all.sh` または `deploy-all.ps1` をコピー
2. カスタム処理を追加
3. 適切なエラーハンドリングを実装

## 🔒 セキュリティ考慮事項

- **機密情報**: スクリプトには機密情報を直接記述しない
- **権限**: 最小権限の原則に従ってAzure権限を設定
- **ログ**: 機密情報がログに出力されないよう注意
- **クリーンアップ**: `--cleanup-on-error` は本番環境では使用しない

## 📞 サポート

問題が発生した場合：

1. このREADMEのトラブルシューティングセクションを確認
2. Azure ポータルでリソースの状態を確認
3. 開発チームに連絡（具体的なエラーメッセージを含む）