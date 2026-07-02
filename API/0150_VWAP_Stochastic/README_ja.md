# Vwap Stochastic 戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)
 
VWAPとストキャスティクスインジケーターを組み合わせた戦略。価格がVWAPを下回り、Stochasticが売られすぎのときに買い。価格がVWAPを上回り、Stochasticが買われすぎのときに売り。

テストでは年平均リターン約187%を示しています。株式市場で最もパフォーマンスが高いです。

VWAPは平均取引レベルを示し、Stochasticは買われすぎまたは売られすぎの状態を示します。ロングはVWAP下方で上昇するオシレーターとともに発動し、ショートはVWAP上方で下降するオシレーターとともに発動します。

イントラデイの価値水準を観察するデイトレーダーはこのスタイルから恩恵を受けることができます。ストップはATRの倍数を使って設定されます。

## 詳細

- **エントリー条件**:
  - ロング: `Close < VWAP && StochK < OversoldLevel`
  - ショート: `Close > VWAP && StochK > OverboughtLevel`
- **ロング/ショート**: 両方
- **エグジット条件**:
  - ロング: `Close > VWAP`
  - ショート: `Close < VWAP`
- **ストップ**: `StopLossPercent` を使用したパーセントベース
- **デフォルト値**:
  - `StochPeriod` = 14
  - `StochKPeriod` = 3
  - `StochDPeriod` = 3
  - `OverboughtLevel` = 80m
  - `OversoldLevel` = 20m
  - `StopLossPercent` = 2m
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **フィルター**:
  - カテゴリ: 平均回帰
  - 方向: 両方
  - インジケーター: VWAP, Stochastic Oscillator
  - ストップ: はい
  - 複雑さ: 中級
  - 時間軸: 中期
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中

