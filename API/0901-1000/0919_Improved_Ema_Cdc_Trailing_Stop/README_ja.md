# 改良型EMA & CDCトレーリングストップ戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

EMAトレンドフィルター、MACD確認、ATRベースのCDCトレーリングストップを組み合わせます。

## 詳細

- **エントリー条件**:
  - **ロング**: 価格 > EMA60、EMA60 > EMA90、MACDライン > シグナルライン。
  - **ショート**: 価格 < EMA60、EMA60 < EMA90、MACDライン < シグナルライン。
- **ロング/ショート**: 両方向。
- **エグジット条件**:
  - トレーリングストップまたはATRベースの利益目標。
- **ストップ**: はい。
- **デフォルト値**:
  - `Ema60Period` = 60
  - `Ema90Period` = 90
  - `AtrPeriod` = 24
  - `Multiplier` = 4
  - `ProfitTargetMultiplier` = 2
  - `CandleType` = TimeSpan.FromMinutes(1).TimeFrame()
- **フィルター**:
  - カテゴリ: トレンドフォロー
  - 方向: 両方
  - インジケーター: EMA, MACD, ATR
  - ストップ: はい
  - 複雑さ: 基本
  - 時間軸: イントラデイ
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
