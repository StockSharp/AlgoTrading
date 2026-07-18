# 適応型オシレーター閾値戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

適応型オシレーター閾値はBufiの適応型閾値（BAT）に基づく動的な閾値を持つRSIを使用します。RSIが固定レベルまたは適応型閾値を下回ったときに買います。

## 詳細

- **エントリー条件**: RSIが固定または適応型閾値を下回る
- **ロング/ショート**: ロング
- **エグジット条件**: 固定バー数での退出またはドル建てストップロス
- **ストップ**: ドル建てストップロス
- **デフォルト値**:
  - `UseAdaptiveThreshold` = true
  - `RsiLength` = 2
  - `BuyLevel` = 14
  - `AdaptiveLength` = 8
  - `AdaptiveCoefficient` = 6
  - `ExitBars` = 28
  - `DollarStopLoss` = 1600
- **フィルター**:
  - カテゴリ: オシレーター
  - 方向: ロング
  - インジケーター: RSI, StandardDeviation, LinearRegression
  - ストップ: ドル
  - 複雑さ: 基本
  - 時間軸: 任意
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
