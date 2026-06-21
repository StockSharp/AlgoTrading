# MACD 流動性トラッカー戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

MACD Liquidity Tracker は MACD のカラー状態を使用してトレードシグナルを生成します。4 つのモード（Fast、Normal、Safe、Crossover）がシグナルの感度を調整します。オプションのストップロスとテイクプロフィットをサポートしています。

## 詳細
- **データ**: 価格ローソク足。
- **エントリー条件**:
  - **ロング**: `SystemType` に依存（デフォルト `Normal` では MACD がシグナル線の上）。
  - **ショート**: `SystemType` に依存（デフォルト `Normal` では MACD がシグナル線の下）。
- **エグジット条件**: 反対シグナル。
- **ストップ**: オプションのストップロスとテイクプロフィット。
- **デフォルト値**:
  - `FastLength` = 25
  - `SlowLength` = 60
  - `SignalLength` = 220
  - `AllowShortTrades` = false
  - `SystemType` = Normal
  - `UseStopLoss` = false
  - `StopLossPercent` = 3
  - `UseTakeProfit` = false
  - `TakeProfitPercent` = 6
  - `StartDate` = 2018-01-01
  - `EndDate` = 2069-12-31
  - `CandleType` = tf(5)
- **フィルター**:
  - カテゴリ: トレンド
  - 方向: ロング/ショート
  - インジケーター: MACD
  - ストップ: はい
  - 複雑さ: 基本
  - 時間軸: イントラデイ (5m)
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
