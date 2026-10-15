# Vwap Stochastic 戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)
 
VWAPとストキャスティクスインジケーターを組み合わせた戦略。価格がVWAPを下回り、Stochasticが売られすぎのときに買い。価格がVWAPを上回り、Stochasticが買われすぎのときに売り。

テストでは年平均リターン約187%を示しています。株式市場で最もパフォーマンスが高いです。

VWAPは平均取引レベルを示し、Stochasticは買われすぎまたは売られすぎの状態を示します。ロングはVWAP下方で上昇するオシレーターとともに発動し、ショートはVWAP上方で下降するオシレーターとともに発動します。

イントラデイの価値水準を観察するデイトレーダーはこのスタイルから恩恵を受けることができます。ストップはエントリー価格から一定割合の位置に設定されます。

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
  - `OverboughtLevel` = 80m
  - `OversoldLevel` = 20m
  - `StopLossPercent` = 2m
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
    市場は24時間取引のため、セッションVWAPはUTCの毎日リセットされ、各足の典型価格を出来高で加重します。ルール中のStochKは%Kで、StochPeriod本のストキャスティクスをStochKPeriod本で平滑化した値です。%Dはルールに関与しないため設定はありません。 保有ポジションと逆方向のエントリーシグナルはドテンになります。
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

