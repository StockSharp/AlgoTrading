# VWAP RSI スキャルパー戦略 FINAL v1
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

VWAPとRSIを組み合わせ、ATRベースのエグジットと1日の取引回数制限を持つスキャルピング戦略。

## 詳細

- **エントリー条件**: セッション内でVWAPおよびEMAに対する価格とRSIの閾値。
- **ロング/ショート**: 両方。
- **エグジット条件**: ATRベースのストップとターゲット。
- **ストップ**: はい。
- **デフォルト値**:
  - `RsiLength` = 3
  - `RsiOversold` = 35m
  - `RsiOverbought` = 70m
  - `EmaLength` = 50
  - `SessionStart` = 09:00
  - `SessionEnd` = 16:00
  - `MaxTradesPerDay` = 3
  - `AtrLength` = 14
  - `StopAtrMult` = 1m
  - `TargetAtrMult` = 2m
  - `CandleType` = TimeSpan.FromMinutes(1)
- **フィルター**:
  - カテゴリ: スキャルピング
  - 方向: 両方
  - インジケーター: VWAP, RSI, EMA, ATR
  - ストップ: はい
  - 複雑さ: 中級
  - 時間軸: イントラデイ (1m)
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
