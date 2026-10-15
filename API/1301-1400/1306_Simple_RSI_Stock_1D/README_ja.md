# シンプルRSI株式戦略 1D
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

このシステムは、RSIが売られすぎレベルを下回り、価格が200日SMAを上回っているときにロングエントリーします。ポジションはATRベースのストップと3つの利益確定目標を使用します。

## 詳細

- **エントリー条件**: RSIが`OversoldLevel`を下回り、終値がSMAフィルターを上回ること。
- **ロング/ショート**: ロングのみ。
- **エグジット条件**: ATRストップまたはいずれかの利益確定レベルへの到達。
- **ストップ**: はい。
- **デフォルト値**:
  - `RsiPeriod` = 5
  - `OversoldLevel` = 30
  - `SmaLength` = 200
  - `AtrLength` = 20
  - `AtrMultiplier` = 1.5
  - `TakeProfit1` = 5
  - `TakeProfit2` = 10
  - `TakeProfit3` = 15
  - `StopLossPercent` = 25
  - `CandleType` = TimeSpan.FromDays(1)
- **フィルター**:
  - カテゴリ: オシレーター
  - 方向: ロング
  - インジケーター: RSI, SMA, ATR
  - ストップ: はい
  - 複雑さ: 基本
  - 時間軸: 日足
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
