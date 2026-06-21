# Ttpインテリジェント・アキュムレーター
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

RSIが平均から1標準偏差を下回ったときにロングポジションを積み上げ、RSIが同じ閾値を上回ったときに分配する戦略。

## 詳細

- **エントリー条件**: RSI < SMA(RSI, `MaPeriod`) - StdDev(RSI, `StdPeriod`)
- **ロング/ショート**: ロングのみ
- **エグジット条件**: RSI > SMA(RSI, `MaPeriod`) + StdDev(RSI, `StdPeriod`) かつ利益が `MinProfit` を上回る
- **ストップ**: いいえ
- **デフォルト値**:
  - `RsiPeriod` = 7
  - `MaPeriod` = 14
  - `StdPeriod` = 14
  - `AddWhileInLossOnly` = true
  - `MinProfit` = 0m
  - `ExitPercent` = 100m
  - `UseDateFilter` = false
  - `StartDate` = 2022-06-01
  - `EndDate` = 2030-07-01
  - `CandleType` = TimeSpan.FromHours(1)
- **フィルター**:
  - カテゴリ: 平均回帰
  - 方向: ロングのみ
  - インジケーター: RSI, MA, StdDev
  - ストップ: いいえ
  - 複雑さ: 基本
  - 時間軸: イントラデイ (1h)
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
