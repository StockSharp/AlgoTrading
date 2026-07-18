# 高度な適応型グリッド戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

高度な適応型グリッド戦略は、複数のテクニカルインジケーターを使用してトレンド方向を評価し、エントリーレベルの動的なグリッドを構築します。グリッドサイズはATRを通じてボラティリティに適応し、価格がトレンド方向のグリッドレベルに触れたときに注文が発注されます。リスク管理には固定ストップロス、テイクプロフィット、トレーリングストップ、時間ベースの決済、日次損失制限が含まれます。

## 詳細

- **エントリー条件**:
  - トレンド相場：RSI確認とともに計算されたグリッドレベルに価格が到達する。
  - 横ばい相場：RSIの過買い/過売りがグリッドエントリーをトリガーする。
- **ロング/ショート**: 両方。
- **エグジット条件**:
  - ストップロス、テイクプロフィット、トレーリングストップ、トレンド反転、または時間ベースの決済。
- **ストップ**: 固定とトレーリング。
- **デフォルト値**:
  - `BaseGridSize` = 1
  - `MaxPositions` = 5
  - `UseVolatilityGrid` = True
  - `AtrLength` = 14
  - `AtrMultiplier` = 1.5
  - `RsiLength` = 14
  - `RsiOverbought` = 70
  - `RsiOversold` = 30
  - `ShortMaLength` = 20
  - `LongMaLength` = 50
  - `SuperLongMaLength` = 200
  - `MacdFastLength` = 12
  - `MacdSlowLength` = 26
  - `MacdSignalLength` = 9
  - `StopLossPercent` = 2
  - `TakeProfitPercent` = 3
  - `UseTrailingStop` = True
  - `TrailingStopPercent` = 1
  - `MaxLossPerDay` = 5
  - `TimeBasedExit` = True
  - `MaxHoldingPeriod` = 48
- **フィルター**:
  - カテゴリ: グリッド / トレンド
  - 方向: 両方
  - インジケーター: ATR, SMA, MACD, RSI, Momentum
  - ストップ: はい
  - 複雑さ: 高
  - 時間軸: 任意
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 高
