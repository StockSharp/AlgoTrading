# VAWSI とトレンド持続性リバーサル戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

VAWSI、トレンド持続性、ATRを組み合わせて平均足上に動的閾値を構築するリバーサル戦略。

## 詳細

- **エントリー条件**: 平均足の終値が動的閾値を上抜けまたは下抜け
- **ロング/ショート**: 両方
- **エグジット条件**: 逆クロスまたは保護的ストップ
- **ストップ**: はい、パーセントベース
- **デフォルト値**:
  - `CandleType` = 15 minute
  - `SlTp` = 5
  - `RsiWeight` = 100
  - `TrendWeight` = 79
  - `AtrWeight` = 20
  - `CombinationMult` = 1
  - `Smoothing` = 3
  - `CycleLength` = 20
- **フィルター**:
  - カテゴリ: リバーサル
  - 方向: 両方
  - インジケーター: RSI, ATR
  - ストップ: はい
  - 複雑さ: 上級
  - 時間軸: イントラデイ
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
