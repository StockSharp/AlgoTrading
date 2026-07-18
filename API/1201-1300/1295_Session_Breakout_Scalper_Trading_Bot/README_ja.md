# セッション・ブレイクアウト・スキャルパー取引ボット戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

Session Breakout Scalper は、事前に定義されたセッション中に形成された価格レンジのブレイクアウトを取引します。

## 詳細

- **エントリー条件**: 価格がセッション高値を上抜け、またはセッション安値を下抜け
- **ロング/ショート**: 両方
- **エグジット条件**: テイクプロフィットまたはストップロス
- **ストップ**: ATR または固定
- **デフォルト値**:
  - `SessionStart` = 01:00
  - `SessionEnd` = 02:00
  - `TakeProfit` = 100
  - `StopLoss` = 50
  - `UseAtrStop` = true
  - `AtrLength` = 14
  - `AtrMultiplier` = 2
  - `CandleType` = time frame 1 minute
- **フィルター**:
  - カテゴリ: ブレイクアウト
  - 方向: 両方
  - インジケーター: ATR
  - ストップ: ATR
  - 複雑さ: 基本
  - 時間軸: イントラデイ
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
