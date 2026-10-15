# 戦略的マルチステップ Supertrend 戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

この戦略は2つの Supertrend 計算を使用してエントリーとエグジットを検出し、設定可能なマルチステップの利益確定を行います。

## 詳細

- **エントリー条件**: 2つの Supertrend の方向に基づくシグナル。
- **ロング/ショート**: 設定可能。
- **エグジット条件**: 逆方向の Supertrend または利益確定レベル。
- **ストップ**: 利益確定ステップ。
- **デフォルト値**:
  - `UseTakeProfit` = true
  - `TakeProfitPercent1` = 6.0
  - `TakeProfitPercent2` = 12.0
  - `TakeProfitPercent3` = 18.0
  - `TakeProfitPercent4` = 50.0
  - `TakeProfitAmount1` = 12
  - `TakeProfitAmount2` = 8
  - `TakeProfitAmount3` = 4
  - `TakeProfitAmount4` = 0
  - `NumberOfSteps` = 3
  - `AtrPeriod1` = 10
  - `Factor1` = 3.0
  - `AtrPeriod2` = 5
  - `Factor2` = 4.0
  - `CandleType` = TimeSpan.FromMinutes(1)
- **フィルター**:
  - カテゴリ: トレンド
  - 方向: 設定可能
  - インジケーター: ATR, Supertrend
  - ストップ: 利益確定
  - 複雑さ: 中級
  - 時間軸: イントラデイ
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
