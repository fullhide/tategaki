# tategaki - プロジェクト仕様書 & AI開発コンテキスト

## 1. プロジェクト概要
- **名称**: tategaki
- **技術スタック**:
  - .NET 10
  - Blazor WebAssembly
  - C# / Razor Components
  - MiniExcel (`MiniExcelLibs`) による Excel 読み込み
- **現状の役割**: 寄付金一覧向けのデータ入力・編集・縦書き印刷と、寄付品一覧の縦書き印刷を同一アプリで提供。

## 2. 現在の実装範囲

### 2.1 寄付金一覧（Donation）
- `TategakiPrint/Pages/Home.razor`: メイン画面。印刷プレビューとデータ編集の切り替えをタブ形式で提供。
- `TategakiPrint/Components/DonationDataEdit.razor`: Excel 取り込みと表形式でのデータ確認・編集。
- `TategakiPrint/Components/DonationTategakiPrint.razor`: 縦書き印刷プレビューと印刷最適化。
- `TategakiPrint/Services/DonationState.cs`: Excel 読み込み・シート選択・データ整形・ソート・画面状態管理。
- `TategakiPrint/Models/DonationItem.cs`: 寄付金データモデル。名前、かな、合計金額、ソートキーを保持。
- `TategakiPrint/Models/PrintSettings.cs`: 印刷設定を保持。フォント、サイズ、列幅、行間、位置調整、空行幅など。

### 2.2 寄付品一覧（Goods）
- `TategakiPrint/Pages/GoodsHome.razor`: 寄付品ページのルート。印刷と編集のタブ切り替えを持つ。
- `TategakiPrint/Components/DonationGoodsPrint.razor`: 寄付品データの縦書き印刷プレビュー。
- `TategakiPrint/Services/DonationGoodsState.cs`: Excel 取り込み・シート選択・データ整形・ソート・画面状態管理。
- `TategakiPrint/Models/GoodsItem.cs`: 寄付品データモデル。氏名・品目・数量・単位・ソートキー。
- `TategakiPrint/Models/DonationGoodsPrintSettings.cs`: 寄付品向け印刷設定。
- `DonationGoodsEdit.razor`: 現状はまだ実装準備中で、コメントアウトされた状態。

## 3. 主要なファイル構成
- `TategakiPrint/Program.cs`
  - Blazor WebAssembly の起動設定。
  - `DonationState` と `DonationGoodsState` を DI に追加。
- `TategakiPrint/App.razor`
  - ルーティング定義。
- `TategakiPrint/Layout/MainLayout.razor`, `NavMenu.razor`
  - アプリの共通レイアウトとナビゲーション。
- `TategakiPrint/Pages/Home.razor`
  - 寄付金一覧のメインページ。
- `TategakiPrint/Pages/GoodsHome.razor`
  - 寄付品一覧のメインページ。
- `TategakiPrint/Components/DonationDataEdit.razor`
  - Excel 取り込みパネル、表編集、ソート、削除機能。
- `TategakiPrint/Components/DonationTategakiPrint.razor`
  - 縦書きプレビュー、ページ分割、空行挿入、印刷ボタン、詳細設定パネル。
- `TategakiPrint/Components/DonationGoodsPrint.razor`
  - 寄付品一覧の縦書きプレビューと印刷用レイアウト。
- `TategakiPrint/Services/DonationState.cs`
  - 寄付金データの Excel 解析と状態管理。
- `TategakiPrint/Services/DonationGoodsState.cs`
  - 寄付品データの Excel 解析と状態管理。
- `TategakiPrint/Models/DonationItem.cs`
  - Excel 列名/列インデックスに対応した属性付きクラスポスト。
- `TategakiPrint/Models/GoodsItem.cs`
  - 寄付品データモデル。
- `TategakiPrint/Models/PrintSettings.cs`
  - 寄付金一覧の印刷設定。
- `TategakiPrint/Models/DonationGoodsPrintSettings.cs`
  - 寄付品一覧の印刷設定。
- `TategakiPrint/TategakiPrint.csproj`
  - `Microsoft.AspNetCore.Components.WebAssembly` 10.0.9、`MiniExcel` 1.31.3 を利用。

## 4. 実装のポイント
### 4.1 Excel 読み込み
- `MiniExcel` を使い、シート名を取得して選択可能にしている。
- `DonationState.LoadSelectedSheetAsync()` は `A3` 以降の行を読み込み、列名やインデックスから `名前`・`かな`・`合計金額` を抽出する。
- `DonationGoodsState.LoadSelectedSheetAsync()` は `名前` と `品物` を厳密な列名判定で取得し、数量・単位・ソートキーを解析する。
- 寄付金データは `Amount` を `decimal` に変換し、正しい値だけを採用する。

### 4.2 データ管理とソート
- `DonationState.SortItems()`:
  - `Amount` 降順、`Kana` 昇順、`SortKey1`〜`SortKey3` を順に比較。
- `DonationGoodsState.SortItems()`:
  - `SortKey` 昇順、`Name` 昇順。
- 編集画面から直接 `State.Items` を書き換え可能。

### 4.3 縦書き印刷レイアウト
- `writing-mode: vertical-rl`、`text-orientation: upright` を使用し、名前・金額を縦書き表示。
- `DonationTategakiPrint.razor` は名前の縦書き表示を生成するために `FormatVerticalName()` を実装。
- 名前生成ロジックは:
  - `様` / `殿` の末尾を除去して再付加
  - `／` / `/` で複数行に分割
  - ASCII 英字を全角に変換
  - 半角括弧を全角括弧に変換
- `FormatAmountToKanji()` は金額を漢数字に変換し、万単位の表記をサポートする。

### 4.4 ページ分割と調整
- `ResetPages()` により短冊を横並びでページに詰め、最大幅を超えたら改ページする。
- 1行用と複数行用で短冊幅を変え、`Settings.SingleLineWidth` / `Settings.MultiLineWidth` を切り替える。
- 金額が変わる境目に `Settings.SpacerWidth` の空行を挿入するロジックがある。
- 行選択機能で、選択した短冊を前ページ/次ページへ移動できる。

### 4.5 印刷最適化
- `@media print` で:
  - A4 横向きに強制
  - 非印刷 UI を非表示
  - 印刷対象コンテナのみ可視化
  - 各ページを `page-break-after: always` で区切る
- 画面プレビューと印刷出力を同一レイアウトで調整している。

### 4.6 設定の永続化
- `DonationTategakiPrint.razor` は `localStorage` に設定を保存し、再描画時に復元する。
- 設定を JSON 形式でエクスポート / インポートできる。

## 5. 現状の課題・未実装領域
- `DonationGoods` 側の編集画面は未完成で、`GoodsHome.razor` の編集タブは「準備中」状態。
- `DonationGoodsPrint.razor` は印刷プレビュー重視で、寄付品データ編集 UI がまだ提供されていない。
- `DonationState` の Excel 読み込みは柔軟だが、入力シートのフォーマット変化には追加対応が必要。

## 6. 開発ルール
- 直接 `main` にコミットしない。
- `feature/use-ai-start` のような専用ブランチで作業する。
- 変更後は `dotnet build ./TategakiPrint/TategakiPrint.csproj` を実行し、ビルドエラーや警告がないことを確認する。
- 既存機能に影響する改修では、`DonationMoneyState` / `DonationGoodsState` の Excel 解析ロジックと `ResetPages()` のページ分割ロジックを重点的に確認する。
- 命名は「寄付金」と「寄付品」を明示するため、共通基盤は `Donation` で統一し、差分は `Money` / `Goods` で表す。
  - 例: `DonationMoneyState`, `DonationMoneyItem`, `DonationGoodsState`, `DonationGoodsItem`
  - 画面ファイル名も `DonationMoneyHome`, `DonationGoodsHome` のように明示する。

## 7. 参考情報
- `TategakiPrint/TategakiPrint.csproj`: Blazor WebAssembly アプリ構成。
- `TategakiPrint/Program.cs`: DI とルートコンポーネント登録。
- `TategakiPrint/Services/DonationState.cs`, `DonationGoodsState.cs`: データフローの中心。
- `TategakiPrint/Components/DonationTategakiPrint.razor`: 縦書き印刷レイアウトの主要実装。
