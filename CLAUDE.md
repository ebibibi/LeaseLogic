# Claude Code Instructions for LeaseLogic

## 必須事項

**すべての作業を開始する前に、必ずDesign.mdファイルを読んで理解してから行動すること。**

## プロジェクト概要

LeaseLogicは契約書を解析し、新会計基準（IFRS 16/ASC 842）に基づいてリース契約かどうかを判定するAPIシステムです。Azure Durable Functionsを使用してバックエンドを実装します。

## アーキテクチャ

- Azure Functions (.NET 8) + Durable Functions
- Queueは使用せず、Durable Functions単体で実装
- HTTP Trigger → Orchestrator → Activity Functions の構成

## 重要な設計決定

1. **Queueなし**: シンプルな Durable Functions 単体構成を採用
2. **結果取得**: ポーリング方式でクライアントが結果を取得
3. **AI統合**: Azure OpenAI Service + Azure AI Document Intelligence
4. **ストレージ**: Azure Blob Storage + Table Storage

## 開発時の注意事項

- Design.mdの設計に従って実装すること
- エラーハンドリングと再試行戦略を適切に実装すること
- セキュリティ要件（認証・暗号化・個人情報保護）を考慮すること
- Application Insightsによる監視・ログ設定を忘れずに

## Git管理ルール

**重要: 切りの良いところで必ずgitにコミット・プッシュすること**

### コミットタイミング
- 設計ドキュメントの更新・作成後
- 新機能の実装完了後
- バグ修正完了後
- 設定ファイルの追加・更新後
- テスト実装完了後
- その他、論理的な区切りの良いタイミング

### コミットメッセージ
- 変更内容を簡潔に説明
- 日本語または英語で記述
- 例: "Add API design specification", "設計書を更新"

### 必須手順
1. 作業完了後、必ずgit statusで変更を確認
2. 適切なファイルをgit addでステージング
3. 意味のあるコミットメッセージでコミット
4. git pushでリモートリポジトリに反映

## 次のステップ

実装開始前に必ずDesign.mdを確認し、不明点があれば設計の見直しを行ってください。