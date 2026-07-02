# Keltner Stochastic 戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)
 
Keltnerチャネルとストキャスティクスオシレーターを組み合わせた戦略。
価格がKeltnerチャネルの境界に達し、Stochasticが売られすぎ/買われすぎの状態を確認したときにポジションを取ります。

テストでは年平均リターン約163%を示しています。株式市場で最もパフォーマンスが高いです。

このセットアップは、オシレーターがモメンタムの転換を確認する中、Keltnerバンド付近でのリバーサルをとらえることを目指しています。価格がエンベロープに押し当たるたびに、両方向でシグナルが発生する可能性があります。

素早いリバーサルを求める短期トレーダーに役立つかもしれません。リスクはATRベースのストップ距離によって抑制されます。

## 詳細

- **エントリー条件**:
  - ロング: `Close < LowerBand && StochK < StochOversold`
  - ショート: `Close > UpperBand && StochK > StochOverbought`
- **ロング/ショート**: 両方
- **エグジット条件**:
  - ロング: `Close > EMA`
  - ショート: `Close < EMA`
- **ストップ**: エントリーから `StopLossAtr` ATR
- **デフォルト値**:
  - `EmaPeriod` = 20
  - `AtrPeriod` = 14
  - `KeltnerMultiplier` = 2.0m
  - `StochPeriod` = 14
  - `StochK` = 3
  - `StochD` = 3
  - `StochOversold` = 20m
  - `StochOverbought` = 80m
  - `StopLossAtr` = 2.0m
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **フィルター**:
  - カテゴリ: 平均回帰
  - 方向: 両方
  - インジケーター: Keltner Channel, Stochastic Oscillator
  - ストップ: はい
  - 複雑さ: 中級
  - 時間軸: 中期
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中

