# Hull Candles戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

Hull Candlesは、平均価格（OHLC4）のHull Moving Averageを使用したシンプルなトレンドフォロー戦略です。HMAが上昇しクローズがSMAを上回る場合にロングポジションを建て、HMAが下落しクローズがSMAを下回る場合にショートポジションを建てます。

## 詳細
- **データ**: 価格ローソク足。
- **エントリー条件**:
  - **ロング**: HMAが上昇し、クローズ > SMA。
  - **ショート**: HMAが下落し、クローズ < SMA。
- **エグジット条件**: 逆シグナル。
- **ストップ**: なし。
- **デフォルト値**:
  - `BodyLength` = 10
  - `SmaLength` = 1
- **フィルター**:
  - カテゴリ: トレンドフォロー
  - 方向: ロング/ショート
  - インジケーター: HMA, SMA
  - 複雑さ: 低
  - リスクレベル: 高
