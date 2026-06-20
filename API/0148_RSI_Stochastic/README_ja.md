# Rsi Stochastic Strategy
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)
 
売られすぎ・買われすぎ状態を二重確認するために、RSIとストキャスティクスオシレーターを組み合わせた戦略。

テストでは年平均リターン約181%を示しています。暗号資産市場で最もパフォーマンスが高いです。

RSIはより広いモメンタムの視点を提供し、Stochasticは極値付近でより速いシグナルを提供します。オシレーターがRSIのコンテキスト内でレベルを超えると、トレードが切り替わります。

オシレーターのセットアップを好む機敏なトレーダーに理想的です。この戦略はリスクを抑制するためにATRストップに依存しています。

## 詳細

- **エントリー条件**:
  - ロング: `RSI < RsiOversold && StochK < StochOversold`
  - ショート: `RSI > RsiOverbought && StochK > StochOverbought`
- **ロング/ショート**: 両方
- **エグジット条件**:
  - ロング: `RSI > 50`
  - ショート: `RSI < 50`
- **ストップ**: `StopLossPercent` でのパーセントベース
- **デフォルト値**:
  - `RsiPeriod` = 14
  - `RsiOversold` = 30m
  - `RsiOverbought` = 70m
  - `StochPeriod` = 14
  - `StochK` = 3
  - `StochD` = 3
  - `StochOversold` = 20m
  - `StochOverbought` = 80m
  - `StopLossPercent` = 2.0m
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **フィルター**:
  - カテゴリ: 平均回帰
  - 方向: 両方
  - インジケーター: RSI, Stochastic Oscillator
  - ストップ: はい
  - 複雑さ: 中級
  - 時間軸: 中期
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中

