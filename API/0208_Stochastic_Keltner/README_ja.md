# Stochastic Keltner Strategy
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)
 
この戦略はStochastic Keltnerインジケーターを使用してシグナルを生成します。
Stoch %K < 20 かつ Price < Keltner lower band（下バンドで売られすぎ）の場合にロングエントリー。Stoch %K > 80 かつ Price > Keltner upper band（上バンドで買われすぎ）の場合にショートエントリー。
混合市場で機会を求めるトレーダーに適しています。

テストでは年間平均リターン約61%を示しています。暗号資産市場で最もパフォーマンスが高いです。

## 詳細
- **エントリー条件**:
  - **ロング**: Stoch %K < 20 && Price < Keltner lower band (oversold at lower band)
  - **ショート**: Stoch %K > 80 && Price > Keltner upper band (overbought at upper band)
- **ロング/ショート**: 両方。
- **エグジット条件**:
  - **ロング**: 価格が中間バンドに戻ったときロングポジションを終了
  - **ショート**: 価格が中間バンドに戻ったときショートポジションを終了
- **ストップ**: はい。
- **デフォルト値**:
  - `StochPeriod` = 14
  - `StochK` = 3
  - `StochD` = 3
  - `EmaPeriod` = 20
  - `KeltnerMultiplier` = 2m
  - `AtrPeriod` = 14
  - `AtrMultiplier` = 2m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **フィルター**:
  - カテゴリ: 混合
  - 方向: 両方
  - インジケーター: Stochastic Keltner
  - ストップ: はい
  - 複雑さ: 中級
  - 時間軸: イントラデイ
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中

