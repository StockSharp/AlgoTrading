# KnuxマーチンゲールStrategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

負け取引の後に取引量を増加させるMartingale戦略です。この手法はAverage Directional Index (ADX)によってエントリーをフィルタリングし、トレンド相場でのみ取引します。陽線ローソク足でロングポジション、陰線ローソク足でショートポジションを開きます。

## 詳細

- **エントリー条件**:
  - ADX > 25
  - ロング: `Close > Open`
  - ショート: `Close < Open`
- **ロング/ショート**: 両方
- **エグジット条件**: ストップロスまたはテイクプロフィット
- **ストップ**: あり
- **デフォルト値**:
  - `AdxPeriod` = 14
  - `LotsMultiplier` = 1.5m
  - `StopLoss` = 150m
  - `TakeProfit` = 50m
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **フィルター**:
  - カテゴリ: トレンドフォロー、Martingale
  - 方向: 両方
  - インジケーター: AverageDirectionalIndex
  - ストップ: 絶対値
  - 複雑さ: 中級
  - 時間軸: イントラデイ
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 高
