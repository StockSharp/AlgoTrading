# Crunchsterのタートル・アンド・トレンド・システム戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

高速/低速EMAトレンドフィルターとDonchianチャネルブレイクアウトエントリー、ATRベースのストップ管理を組み合わせた戦略。モメンタムが反転した際にはトレーリングDonchianチャネルでポジションを決済する。

## 詳細

- **エントリー条件**: EMA差分クロスまたはDonchianチャネルブレイクアウト。
- **ロング/ショート**: 両方。
- **エグジット条件**: トレーリングチャネルまたはATRストップ。
- **ストップ**: あり、ATRベース。
- **デフォルト値**:
  - `CandleType` = 1時間
  - `FastEmaPeriod` = 10
  - `BreakoutPeriod` = 20
  - `TrailPeriod` = 1000
  - `StopAtrMultiple` = 20
  - `OrderPercent` = 10
  - `TrendEnabled` = true
  - `BreakoutEnabled` = false
- **フィルター**:
  - カテゴリ: トレンド
  - 方向: ロング/ショート
  - インジケーター: EMA、Donchian、ATR
  - ストップ: あり
  - 複雑さ: 中程度
  - 時間軸: イントラデイ
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
