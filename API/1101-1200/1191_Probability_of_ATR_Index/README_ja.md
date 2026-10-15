# ATR確率インデックス戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

Probability of ATR Indexインジケーターに基づく戦略。

## 詳細

- **エントリー条件**: 確率がその移動平均を上下にクロスする。
- **ロング/ショート**: 両方向。
- **エグジット条件**: 反対シグナル。
- **ストップ**: なし。
- **デフォルト値**:
  - `AtrDistance` = 1.5m
  - `Bars` = 8
  - `CandleType` = TimeSpan.FromMinutes(5)
- **フィルター**:
  - カテゴリ: ボラティリティ
  - 方向: 両方
  - インジケーター: ATR, SMA, StandardDeviation
  - ストップ: なし
  - 複雑さ: 基本
  - 時間軸: イントラデイ (5m)
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
