# LeaseLogic Infrastructure

このディレクトリには、LeaseLogic APIのAzureインフラストラクチャのBicepテンプレートとデプロイメントスクリプトが含まれています。

## 含まれるリソース

- **Azure Functions** (.NET 8, Premium Plan)
  - Durable Functions対応
  - システム割り当てマネージドID
  
- **Azure Storage Account**
  - Blob containers: `documents`, `results`, `standards`
  - Table storage: `AnalysisMetadata`
  
- **Azure OpenAI Service**
  - GPT-4 deployment
  - GPT-4 Turbo deployment (Assistants API用)
  - Text Embedding deployment (RAG用)
  
- **Azure AI Document Intelligence**
  - 契約書解析用

- **Azure Key Vault**
  - API キーの安全な保存
  
- **Application Insights & Log Analytics**
  - 監視とログ収集

## デプロイ方法

### 前提条件

- Azure CLI または Azure PowerShell
- 適切なAzureサブスクリプションへのアクセス権限

### PowerShell でのデプロイ

```powershell
# Azure にログイン
Connect-AzAccount

# デプロイ実行
.\deploy.ps1 -ResourceGroupName "leaselogic-rg"
```

### Bash でのデプロイ

```bash
# Azure にログイン
az login

# デプロイ実行
./deploy.sh -g "leaselogic-rg"
```

### パラメータオプション

| パラメータ | 説明 | デフォルト値 |
|-----------|------|-------------|
| `-g, --resource-group` | リソースグループ名 (必須) | - |
| `-l, --location` | Azureリージョン | japaneast |
| `-e, --environment` | 環境名 (dev/staging/prod) | dev |
| `-p, --parameters-file` | パラメータファイル | parameters.json |

### カスタムパラメータ

`parameters.json`ファイルを編集して設定をカスタマイズできます：

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

## デプロイ後の作業

1. **Function Appコードのデプロイ**
   ```bash
   # Function Appプロジェクトをビルド・デプロイ
   ```

2. **会計基準文書のアップロード**
   ```bash
   # IFRS 16, ASC 842等のPDFを'standards'コンテナにアップロード
   az storage blob upload --account-name <storage-account> --container-name standards --file IFRS-16.pdf --name ifrs16.pdf
   ```

3. **OpenAI Assistantの設定**
   - リース分析用のAssistantを作成
   - 参照文書をベクターストアに関連付け

## 出力

デプロイ完了後、以下の情報が `deployment-outputs.json` に保存されます：

- Function App名とURL
- Storage Account名
- OpenAI Service エンドポイント
- Document Intelligence エンドポイント
- Key Vault URI
- Application Insights接続文字列

## セキュリティ

- 全てのAPIキーはKey Vaultに安全に保存
- Function Appはシステム割り当てマネージドIDを使用
- HTTPS通信を強制
- 最小権限の原則に基づくRBAC設定

## コスト最適化

- Storage Account: Standard LRS
- Function App: Premium Plan (必要に応じてConsumptionに変更可能)
- AI Services: 従量課金
- Log Analytics: 30日保持期間

## トラブルシューティング

### よくある問題

1. **OpenAI Serviceの地域制限**
   - GPT-4が利用可能な地域でデプロイしてください
   - デフォルトは `eastus` に設定済み

2. **リソース名の競合**
   - ユニークサフィックスが自動生成されますが、必要に応じて手動調整

3. **権限エラー**
   - デプロイユーザーがサブスクリプションの共同作成者権限を持つことを確認

### ログ確認

```bash
# デプロイメントの詳細ログを確認
az deployment group show --resource-group <rg-name> --name <deployment-name>
```