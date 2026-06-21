# 固定SLを用いたリスク・リワード・バックテスター戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

終値がユーザー定義の値と一致した際にロングエントリーする。ストップロスはATRまたはピボット安値で設定し、テイクプロフィットはリスク・リワード比または固定パーセントを使用する。目標到達後にオプションでストップをブレイクイーブンに移動できる。

## 詳細

- **エントリー条件**: 終値が `DealStartValue` と等しい
- **ロング/ショート**: ロングのみ
- **エグジット条件**: テイクプロフィットまたはストップロス（オプションのブレイクイーブン）
- **ストップ**: ATRまたはピボット安値（ブレイクイーブン付き）
- **デフォルト値**:
  - `DealStartValue` = 100
  - `UseRiskToReward` = true
  - `RiskToRewardRatio` = 1.5
  - `StopLossType` = Atr
  - `AtrFactor` = 1.4
  - `PivotLookback` = 8
  - `FixedTp` = 0.015
  - `FixedSl` = 0.015
  - `UseBreakEven` = true
  - `BreakEvenRr` = 1.0
  - `BreakEvenPercent` = 0.001
- **フィルター**:
  - カテゴリ: トレンド
  - 方向: ロングのみ
  - インジケーター: ATR, Lowest
  - ストップ: はい
  - 複雑さ: 基本
  - 時間軸: イントラデイ
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
