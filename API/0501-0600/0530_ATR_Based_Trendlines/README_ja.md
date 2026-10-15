# ATRベースのトレンドライン戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

ピボットポイントからATRベースのトレンドラインを構築し、そのブレイクアウトを取引する戦略。

## 詳細

- **エントリー条件**: ATRベースのトレンドラインのブレイクアウト。
- **ロング/ショート**: 両方向。
- **エグジット条件**: 反対方向のブレイクアウト。
- **ストップ**: なし。
- **デフォルト値**:
  - `LookbackLength` = 30
  - `AtrPercent` = 1.0
  - `CandleType` = TimeSpan.FromMinutes(1)
- **フィルター**:
  - カテゴリ: トレンド
  - 方向: 両方
  - インジケーター: ATR, Price Action
  - ストップ: いいえ
  - 複雑さ: 基本
  - 時間軸: イントラデイ (1m)
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
