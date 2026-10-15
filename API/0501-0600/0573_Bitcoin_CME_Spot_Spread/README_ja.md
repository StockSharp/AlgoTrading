# Bitcoin CME-スポットスプレッド
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

CME Bitcoin先物とBitfinex BTCUSDスポット間のスプレッドをボリンジャーバンドで取引します。
スプレッドが下限バンドを下回るとロング、上限バンドを上回るとショート。
ポジションは4段階のテイクプロフィットレベルで段階的に縮小され、固定バー数後に決済されます。

## 詳細

- **データ**: CME Bitcoin先物とBitfinex BTCUSDスポット。
- **エントリー**: スプレッド売られすぎでロング、買われすぎでショート。
- **エグジット**: 段階的なテイクプロフィットまたは保有バー数後の決済。
- **銘柄**: Bitcoin先物。
- **リスク**: 部分決済と時間制限による決済。
