# tategaki

Blazor WebAssembly アプリケーションで、寄付金一覧と寄付品一覧のデータ入力・編集・縦書き印刷を提供します。

## 命名ルール

同じドメイン内で意味が異なる概念は、名前だけで区別できるように統一します。

- 寄付金系は `DonationMoney` で統一する
  - 例: `DonationMoneyState`, `DonationMoneyItem`, `DonationMoneyHome`
- 寄付品系は `DonationGoods` で統一する
  - 例: `DonationGoodsState`, `DonationGoodsItem`, `DonationGoodsHome`
- 画面・コンポーネント・モデル・サービスは、役割が分かる名前を使う
- ファイル名も対応する概念が伝わるように命名する

## 開発メモ

- 変更後は `dotnet build tategaki.slnx` でビルド確認を行う
- 既存機能に影響する修正では、Excel 解析ロジックと印刷レイアウトの挙動を重点的に確認する
