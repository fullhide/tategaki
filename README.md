# tategaki

tategaki は、寄付金・寄付品の一覧を Excel から取り込み、編集・印刷用レイアウトに整えて利用できる Blazor WebAssembly アプリケーションです。

## このプロジェクトについて
- 寄付金のデータを確認しながら印刷用の縦書きレイアウトに整えられます
- 寄付品のデータも同様に印刷向けに整理できます
- ページ境界を手動で調整し、その結果を設定として保存・読み込みできます
- 印刷用設定の JSON エクスポート / インポートに対応しており、現在のレイアウト内容を優先して保持します
- 画面上部にバージョン表示を追加しています

## ドキュメント
- [spec.md](spec.md): 機能要件と仕様の概要
- [AI_CONTEXT.md](AI_CONTEXT.md): 実装コンテキストと開発メモ
- [RELEASE_NOTES.md](RELEASE_NOTES.md): ユーザー向けの変更点一覧

## 開発環境
- .NET 10
- Blazor WebAssembly
- C# / Razor Components
- MiniExcel

## ライセンス
このプロジェクトは MIT License のもとで提供されています。
