# RSI 買われすぎ/売られすぎ (RSI Overbought/Oversold)
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

このシステムは相対力指数（RSI）を使ってリバーサルを取引します。RSIが売られすぎレベルを下回ると、ショートを決済した後に買いエントリーします。RSIが買われすぎレベルを上回ると、ロングを決済した後に売りエントリーします。

テストでは年平均リターンが約61%となっています。暗号通貨市場での運用に最も適しています。

RSIがニュートラルゾーンに戻るか、ストップロスに達したときにポジションを決済します。

## 詳細

- **エントリー条件**: RSIが `OversoldLevel` を下回るか `OverboughtLevel` を上回る。
- **ロング/ショート**: 両方。
- **エグジット条件**: RSIが `NeutralLevel` をクロスするかストップ。
- **ストップ**: はい。
- **デフォルト値**:
  - `RsiPeriod` = 14
  - `OverboughtLevel` = 70
  - `OversoldLevel` = 30
  - `NeutralLevel` = 50
  - `CandleType` = TimeSpan.FromMinutes(5)
  - `StopLossPercent` = 2.0m
- **フィルター**:
  - カテゴリ: オシレーター
  - 方向: 両方
  - インジケーター: RSI
  - ストップ: はい
  - 複雑さ: 基本
  - 時間軸: イントラデイ
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: はい
  - リスクレベル: 中
