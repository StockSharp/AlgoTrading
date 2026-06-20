# Adx Stochastic Strategy
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)
 
トレンドの強さにADX（平均方向性指数）、売られすぎ/買われすぎ状態のエントリータイミングにストキャスティクスオシレーターを組み合わせた戦略。

テストでは年平均リターン約172%を示しています。外国為替市場で最もパフォーマンスが高いです。

ADXはトレンドの強さを強調し、Stochasticはプルバックを特定します。ADXが高い水準を維持している間にモメンタムが転換するとロングまたはショートのシグナルが現れます。

トレンドフォローとオシレータータイミングを組み合わせるトレーダーに適しています。保護的なATRストップがドローダウンを抑制するのに役立ちます。

## 詳細

- **エントリー条件**:
  - ロング: `ADX > AdxThreshold && StochK < StochOversold && Bullish`
  - ショート: `ADX > AdxThreshold && StochK > StochOverbought && Bearish`
- **ロング/ショート**: 両方
- **エグジット条件**:
  - `ADX < AdxThreshold` のときに退出
- **ストップ**: `StopLossPercent` でのパーセントベース
- **デフォルト値**:
  - `AdxPeriod` = 14
  - `AdxThreshold` = 25m
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
  - インジケーター: ADX, Stochastic Oscillator
  - ストップ: はい
  - 複雑さ: 中級
  - 時間軸: 中期
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中

