# MACD強化MTFストップロス付き戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

MACDベースのスコアリングとATR由来のトレーリングストップラインを使用するマルチ時間軸戦略。

## 詳細

- **エントリー条件**: MACDスコアが正または負に転換。
- **ロング/ショート**: 両方向。
- **エグジット条件**: 反対方向のシグナルまたはトレーリングストップラインの突破。
- **ストップ**: ATRトレーリングストップ。
- **デフォルト値**:
  - `FastLength` = 12
  - `SlowLength` = 26
  - `SignalLength` = 9
  - `CrossScore` = 10
  - `IndicatorScore` = 8
  - `HistogramScore` = 2
  - `StopLossFactor` = 1.2
  - `StopLossPeriod` = 10
  - `CandleType` = TimeSpan.FromMinutes(1)
- **フィルター**:
  - カテゴリ: トレンド
  - 方向: 両方
  - インジケーター: MACD, ATR
  - ストップ: はい
  - 複雑さ: 中級
  - 時間軸: イントラデイ
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
