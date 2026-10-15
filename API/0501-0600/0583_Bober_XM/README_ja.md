# Bober XM戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

Bober XM戦略はカスタムケルトナー計算に基づくデュアルチャネルアプローチを使用します。ブレイクアウトのエントリーは加重移動平均とADXによる全体的なトレンド強度によって確認されます。エグジットはADXが強い状態でOBVがその移動平均をクロスした時に行われます。

モメンタム確認と出来高ベースのエグジットを求めるトレーダー向けに設計されています。

## 詳細

- **エントリー条件**:
  - **ロング**: `Close > UpperBand && Close > WMA && ADX > Threshold`
  - **ショート**: `Close < LowerBand && Close < WMA && ADX > Threshold`
- **ロング/ショート**: 両方
- **エグジット条件**:
  - **ロング**: `OBV < OBV_MA && ADX > Threshold`
  - **ショート**: `OBV > OBV_MA && ADX > Threshold`
- **ストップ**: `StopLossPercent` によるパーセンテージストップロス
- **デフォルト値**:
  - `EmaPeriod` = 20
  - `AtrPeriod` = 10
  - `KeltnerMultiplier` = 1.5m
  - `WmaPeriod` = 15
  - `ObvMaPeriod` = 22
  - `AdxPeriod` = 60
  - `AdxThreshold` = 35m
  - `StopLossPercent` = 2m
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **フィルター**:
  - カテゴリ: トレンドフォロー
  - 方向: 両方
  - インジケーター: Keltner Channel, WMA, OBV, ADX
  - ストップ: はい
  - 複雑さ: 中級
  - 時間軸: 中期
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
