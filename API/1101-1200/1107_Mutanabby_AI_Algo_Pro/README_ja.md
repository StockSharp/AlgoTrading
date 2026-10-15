# Mutanabby AI Algo Pro 戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

Mutanabby AI Algo Pro 戦略は、強気の包み足パターンが RSI のしきい値以下の読みおよび指定された本数のバーにわたる価格下落と一致したときにロングでエントリーします。弱気の包み足パターンが出現するか、ストップロスに達したときに決済します。

## 詳細
- **エントリー条件**: 強気の包み足、安定したローソク足、RSI がしきい値以下、N 本前の価格を下回る。
- **ロング/ショート**: ロングのみ。
- **エグジット条件**: 弱気の包み足またはストップロス。
- **ストップ**: オプション。
- **デフォルト値**:
  - `CandleStabilityIndex` = 0.5
  - `RsiIndex` = 50
  - `CandleDeltaLength` = 5
  - `DisableRepeatingSignals` = false
  - `EnableStopLoss` = true
  - `StopLossMethod` = EntryPriceBased
  - `EntryStopLossPercent` = 2.0
  - `LookbackPeriod` = 10
  - `StopLossBufferPercent` = 0.5
  - `CandleType` = TimeSpan.FromMinutes(1)
- **フィルター**:
  - カテゴリ: トレンドフォロー
  - 方向: ロングのみ
  - インジケーター: RSI
  - ストップ: はい
  - 複雑さ: 基本
  - 時間軸: イントラデイ
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
