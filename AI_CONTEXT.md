# tategaki - AI開発コンテキスト

このファイルは、AI や開発者がコードを理解するための実装コンテキストです。機能仕様は [spec.md](spec.md) に分離しています。

## 1. プロジェクト概要
- 名称: tategaki
- 技術スタック: .NET 10 / Blazor WebAssembly / C# / Razor / MiniExcel
- 役割: 寄付金と寄付品の Excel 取り込み、編集、縦書き印刷を同一アプリで提供する

## 2. 主要な構成
### 寄付金フロー
- [TategakiPrint/Pages/DonationMoneyHome.razor](TategakiPrint/Pages/DonationMoneyHome.razor): 寄付金画面の入口
- [TategakiPrint/Components/DonationMoneyDataEdit.razor](TategakiPrint/Components/DonationMoneyDataEdit.razor): Excel 取り込みと表編集
- [TategakiPrint/Components/DonationMoneyTategakiPrint.razor](TategakiPrint/Components/DonationMoneyTategakiPrint.razor): 印刷プレビューとレイアウト制御
- [TategakiPrint/Services/DonationMoneyState.cs](TategakiPrint/Services/DonationMoneyState.cs): データ読み込み・ソート・画面状態管理
- [TategakiPrint/Models/DonationMoneyItem.cs](TategakiPrint/Models/DonationMoneyItem.cs): 寄付金データモデル
- [TategakiPrint/Models/PrintSettings.cs](TategakiPrint/Models/PrintSettings.cs): 印刷設定

### 寄付品フロー
- [TategakiPrint/Pages/DonationGoodsHome.razor](TategakiPrint/Pages/DonationGoodsHome.razor): 寄付品画面の入口
- [TategakiPrint/Components/DonationGoodsPrint.razor](TategakiPrint/Components/DonationGoodsPrint.razor): 印刷プレビュー
- [TategakiPrint/Services/DonationGoodsState.cs](TategakiPrint/Services/DonationGoodsState.cs): データ読み込み・ソート・画面状態管理
- [TategakiPrint/Models/DonationGoodsItem.cs](TategakiPrint/Models/DonationGoodsItem.cs): 寄付品データモデル
- [TategakiPrint/Models/DonationGoodsPrintSettings.cs](TategakiPrint/Models/DonationGoodsPrintSettings.cs): 印刷設定

## 3. 実装メモ
- Excel 取り込みは MiniExcel を利用し、シート名選択と列名ベースの解析を行う
- 寄付金は名前・かな・金額・ソートキーを扱い、ソート順は金額降順・かな順・ソートキー順
- 寄付品は氏名・品物・数量・単位・ソートキーを扱い、ソート順はソートキー順・氏名順
- 印刷レイアウトでは縦書き CSS とページ分割ロジックを利用する
- 設定は localStorage に保存し、JSON でエクスポート / インポートできる

## 4. 開発ルール
- 直接 main にコミットしない
- 専用ブランチで作業する
- 変更後は `dotnet build tategaki.slnx` を実行して確認する
- 既存機能に影響する修正では、Excel 解析と印刷レイアウトを重点的に確認する
- 命名は寄付金を `DonationMoney`、寄付品を `DonationGoods` で統一する

## 5. 現状の注意点
- 寄付品の編集画面はまだ未実装
- Excel の入力フォーマット変更に対しては追加対応が必要な場合がある
