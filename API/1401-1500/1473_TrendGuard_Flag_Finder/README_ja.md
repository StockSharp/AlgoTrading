# TrendGuard Flag Finder 戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

TrendGuard Flag Finder は SuperTrend で確認された強気フラッグと弱気フラッグのパターンを検出します。
強気フラッグを上抜けたときに買い、弱気フラッグを下抜けたときに売ります。

## 詳細

- **エントリー条件**: SuperTrend 確認付きフラッグのブレイクアウト
- **ロング/ショート**: 設定可能
- **エグジット条件**: 逆のフラッグブレイクアウト
- **ストップ**: なし
- **デフォルト値**:
  - `TradingDirection` = Both
  - `SuperTrend Length` = 10
  - `SuperTrend Factor` = 4
  - `MaxFlagDepth` = 5
  - `MinFlagLength` = 3
  - `MaxFlagLength` = 7
  - `MaxFlagRally` = 5
  - `MinBearFlagLength` = 3
  - `MaxBearFlagLength` = 7
  - `PoleMin` = 3
  - `PoleLength` = 7
  - `PoleMinBear` = 3
  - `PoleLengthBear` = 7
- **フィルター**:
  - カテゴリ: パターン
  - 方向: 設定可能
  - インジケーター: SuperTrend, Lowest, Highest
  - ストップ: なし
  - 複雑さ: 上級
  - 時間軸: イントラデイ
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
