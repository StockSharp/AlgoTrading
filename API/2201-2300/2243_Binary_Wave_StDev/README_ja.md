# Binary Wave StdDev 戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

MA、MACD、CCI、Momentum、RSI、ADXからのシグナルを設定可能なウェイトで合計する戦略。
標準偏差で測定されたボラティリティが閾値を超えると、累積スコアの方向でトレードします。
ポイント単位のオプションのストップロスとテイクプロフィット。

## 詳細

- **エントリー条件**:
  - ロング: スコア > 0 かつ StdDev >= EntryVolatility
  - ショート: スコア < 0 かつ StdDev >= EntryVolatility
- **エグジット条件**:
  - ボラティリティが ExitVolatility を下回る
- **ストップ**: `UseStopLoss` と `UseTakeProfit` でオプション
- **デフォルト値**:
  - `WeightMa` = 1
  - `WeightMacd` = 1
  - `WeightCci` = 1
  - `WeightMomentum` = 1
  - `WeightRsi` = 1
  - `WeightAdx` = 1
  - `MaPeriod` = 13
  - `FastMacd` = 12
  - `SlowMacd` = 26
  - `SignalMacd` = 9
  - `CciPeriod` = 14
  - `MomentumPeriod` = 14
  - `RsiPeriod` = 14
  - `AdxPeriod` = 14
  - `StdDevPeriod` = 9
  - `EntryVolatility` = 1.5
  - `ExitVolatility` = 1
  - `StopLossPoints` = 1000
  - `TakeProfitPoints` = 2000
  - `UseStopLoss` = false
  - `UseTakeProfit` = false
  - `CandleType` = TimeSpan.FromHours(4).TimeFrame()
- **フィルター**:
  - カテゴリ: トレンドフォロー
  - 方向: 両方
  - インジケーター: MA, MACD, CCI, Momentum, RSI, ADX, StandardDeviation
  - ストップ: オプション
  - 複雑さ: 中程度
  - 時間軸: 中期
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
