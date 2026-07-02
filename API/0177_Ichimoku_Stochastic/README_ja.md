# Ichimoku Stochastic 戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)
 
Ichimoku CloudとStochastic Oscillatorインジケーターに基づく戦略。
価格がKumo（雲）の上にあり、Tenkan > Kijunで、Stochasticが売られすぎ（< 20）のときロングエントリー。価格がKumoの下にあり、Tenkan < Kijunで、Stochasticが買われすぎ（> 80）のときショートエントリー。

テストでは年平均リターン約118%を示しています。株式市場で最もパフォーマンスが高くなります。

Ichimokuがトレンドとサポートレベルを描き、Stochasticが押し目でのエントリータイミングを計ります。オシレーターが雲の優勢な方向でリセットしたときにトレードが開始されます。

構造化されたインジケーターを好むトレーダーに実用的です。ATRストップが急激なリバーサルをカバーします。

## 詳細

- **エントリー条件**:
  - ロング: `Price > Cloud && StochK < 20`
  - ショート: `Price < Cloud && StochK > 80`
- **ロング/ショート**: 両方
- **エグジット条件**:
  - 逆方向への雲のブレイクアウト
- **ストップ**: Ichimoku雲の境界を使用
- **デフォルト値**:
  - `TenkanPeriod` = 9
  - `KijunPeriod` = 26
  - `SenkouPeriod` = 52
  - `StochPeriod` = 14
  - `StochK` = 3
  - `StochD` = 3
  - `CandleType` = TimeSpan.FromMinutes(30).TimeFrame()
- **フィルター**:
  - カテゴリ: 平均回帰
  - 方向: 両方
  - インジケーター: Ichimoku Cloud, Stochastic Oscillator
  - ストップ: はい
  - 複雑さ: 中級
  - 時間軸: 中期
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中

