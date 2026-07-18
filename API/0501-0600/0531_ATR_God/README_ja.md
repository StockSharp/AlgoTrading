# ATR GOD 戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

SupertrendエントリーとATRベースのストップロスおよびテイクプロフィットを組み合わせた戦略。

## 詳細

- **エントリー条件**: Supertrendの反転。
- **ロング/ショート**: 両方向。
- **エグジット条件**: ATRストップまたは反対シグナル。
- **ストップ**: ATRベース。
- **デフォルト値**:
  - `Period` = 10
  - `Multiplier` = 3m
  - `RiskMultiplier` = 4.5m
  - `RewardRiskRatio` = 1.5m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **フィルター**:
  - カテゴリ: トレンド
  - 方向: 両方
  - インジケーター: ATR, Supertrend
  - ストップ: ATR
  - 複雑さ: 基本
  - 時間軸: イントラデイ (5m)
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中

