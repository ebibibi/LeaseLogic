# LeaseLogic Frontend

LeaseLogic契約書解析APIのフロントエンドWebアプリケーションです。

## 技術スタック

- **React 18** + **TypeScript**
- **Axios** (API通信)
- **Lucide React** (アイコン)
- **CSS3** (レスポンシブデザイン)

## 機能

### 📄 ファイルアップロード
- ドラッグ&ドロップ対応
- PDF、Word、テキストファイル対応
- ファイルサイズ・形式バリデーション
- プログレス表示

### 🔍 リアルタイム解析監視
- 解析進捗のリアルタイム表示
- 4段階の処理ステップ表示
- エラーハンドリング

### 📊 解析結果表示
- リース判定結果（信頼度付き）
- IFRS 16/ASC 842準拠の詳細分析
- 契約概要とレコメンデーション
- リスク要因とコンプライアンス要件

### 📈 解析履歴
- 過去10件の解析履歴表示
- 結果概要の一覧表示

## 開発環境のセットアップ

### 前提条件
- Node.js 18+
- npm または yarn

### インストールと起動

```bash
# 依存関係のインストール
npm install

# 開発サーバー起動
npm start
```

アプリケーションは http://localhost:3000 で起動します。

### 環境変数

開発環境では、`package.json` の `proxy` 設定により、ローカルのFunction App（http://localhost:7071）にAPIリクエストがプロキシされます。

本番環境では、以下の環境変数が自動設定されます：
- `REACT_APP_API_BASE_URL`: Function AppのURL

## ビルドとデプロイ

### 本番ビルド

```bash
npm run build
```

### Azure App Service へのデプロイ

#### 自動デプロイ（推奨）
全体デプロイスクリプトを使用：
```bash
./scripts/deploy-all.sh -g "your-resource-group"
```

#### 手動デプロイ
```bash
# 1. ビルド
npm run build

# 2. Azure CLI でデプロイ
cd build
zip -r ../build.zip .
az webapp deployment source config-zip \
  --resource-group "your-resource-group" \
  --name "your-frontend-app-name" \
  --src ../build.zip
```

## プロジェクト構造

```
frontend/
├── public/
│   └── index.html           # HTMLテンプレート
├── src/
│   ├── components/          # Reactコンポーネント
│   │   ├── FileUpload.tsx   # ファイルアップロード
│   │   ├── AnalysisProgress.tsx # 解析進捗表示
│   │   ├── AnalysisResults.tsx  # 結果表示
│   │   └── AnalysisHistory.tsx  # 履歴表示
│   ├── services/
│   │   └── api.ts          # API通信ロジック
│   ├── types.ts            # TypeScript型定義
│   ├── App.tsx             # メインアプリケーション
│   ├── index.tsx           # エントリーポイント
│   └── index.css           # グローバルスタイル
├── package.json
└── tsconfig.json
```

## API統合

### エンドポイント

フロントエンドは以下のAPIエンドポイントを使用します：

| エンドポイント | 説明 |
|---------------|------|
| `POST /api/upload-url` | アップロードURL生成 |
| `POST /api/analyze` | 解析開始 |
| `GET /api/status/{id}` | 解析状況確認 |
| `GET /api/result/{id}` | 解析結果取得 |

### フロー

1. **ファイル選択**: ユーザーがファイルを選択
2. **アップロードURL取得**: バックエンドからSAS URLを取得
3. **ファイルアップロード**: Azure Blob Storageに直接アップロード
4. **解析開始**: バックエンドに解析リクエスト送信
5. **進捗監視**: 3秒間隔でステータスポーリング
6. **結果表示**: 解析完了時に詳細結果を表示

## カスタマイズ

### テーマカラー

CSS変数を使用してテーマカラーをカスタマイズできます：

```css
:root {
  --primary-color: #667eea;
  --secondary-color: #764ba2;
  --success-color: #28a745;
  --error-color: #dc3545;
  --warning-color: #ffc107;
}
```

### API設定

`src/services/api.ts` でAPIエンドポイントやタイムアウト設定を変更できます：

```typescript
const API_BASE_URL = process.env.REACT_APP_API_BASE_URL || 'http://localhost:7071';

const api = axios.create({
  baseURL: API_BASE_URL,
  timeout: 30000, // 30秒
});
```

## トラブルシューティング

### よくある問題

1. **CORS エラー**
   - Function AppのCORS設定を確認
   - フロントエンドのURLが許可されているか確認

2. **API接続エラー**
   - `REACT_APP_API_BASE_URL` 環境変数を確認
   - Function Appが正常に動作しているか確認

3. **ファイルアップロードエラー**
   - ファイルサイズ制限（50MB）を確認
   - 対応ファイル形式を確認

### ログ確認

ブラウザの開発者ツールでAPIリクエスト/レスポンスを確認：

1. F12キーで開発者ツールを開く
2. Networkタブでリクエストを確認
3. Consoleタブでエラーログを確認

## パフォーマンス

### 最適化されている機能

- **コード分割**: React.lazy()を使用（将来実装予定）
- **API キャッシュ**: axios interceptorsでレスポンスログ
- **レスポンシブデザイン**: モバイルファーストデザイン
- **プログレッシブエンハンスメント**: 段階的な機能強化

### さらなる最適化

本番環境での使用時に検討すべき最適化：

1. **Service Worker**: オフライン対応
2. **PWA化**: モバイルアプリライクな体験
3. **画像最適化**: WebP形式の使用
4. **CDN**: 静的アセットの配信最適化

## セキュリティ

### 現在の実装

- **HTTPS**: 本番環境では強制
- **CORS**: 適切なオリジン制限
- **入力検証**: ファイル形式・サイズバリデーション

### 今後の実装予定

- **認証**: Azure AD統合
- **CSP**: Content Security Policy
- **認可**: ロールベースアクセス制御

## 貢献

このプロジェクトへの貢献を歓迎します：

1. 機能追加・改善の提案
2. バグレポート
3. ドキュメント改善
4. UI/UXの改善提案