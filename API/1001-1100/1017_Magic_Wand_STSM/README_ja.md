# Magic Wand STSM 戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

200 期間 SMA フィルターを備えた Supertrend インジケーターを使用するトレンドフォロー・システム。Supertrend の方向にトレードし、ラインをストップとして使用しながら、設定可能なリスク・リワードのテイクプロフィットを目指します。

## 詳細

- **エントリー条件**:
  - **ロング**: Supertrend が価格の下にあり、終値が SMA200 の上。
  - **ショート**: Supertrend が価格の上にあり、終値が SMA200 の下。
- **ロング/ショート**: 両方向。
- **エグジット条件**:
  - テイクプロフィット: `entry ± (entry - Supertrend) * RiskReward`。
  - ストップロス: Supertrend 位置。
- **ストップ**: はい。
- **デフォルト値**:
  - `Supertrend Period` = 10
  - `Supertrend Multiplier` = 3
  - `MA Length` = 200
  - `Risk Reward` = 2
- **フィルター**:
  - カテゴリ: トレンドフォロー
  - 方向: 両方
  - インジケーター: 複数
  - ストップ: はい
  - 複雑さ: 中級
  - 時間軸: イントラデイ
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
