# FTMOルールモニター
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

FTMOチャレンジのルールを追跡し、ATRリスクに基づいてトレードを管理する戦略。

ATRを使用してポジションサイズを決定し、チャレンジの目標が達成されると停止します。最大日次損失、総損失、利益目標、最低取引日数を監視します。

## 詳細

- **エントリー条件**: 強気ローソク足でロング、弱気ローソク足でショートを建てる。
- **ロング/ショート**: 両方向。
- **エグジット条件**: チャレンジ完了または逆シグナル。
- **ストップ**: ATRベース。
- **デフォルト値**:
  - `AccountSize` = 10000m
  - `RiskPercent` = 1m
  - `AtrPeriod` = 14
  - `AtrMultiplier` = 2m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **フィルター**:
  - カテゴリ: リスク管理
  - 方向: 両方
  - インジケーター: ATR
  - ストップ: ATR
  - 複雑さ: 基本
  - 時間軸: イントラデイ (5m)
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
