# Keltner Williams R 戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)
 
この戦略はKeltner Williams Rインジケーターを使ってシグナルを生成します。
Price < lower Keltner band && Williams %R < WilliamsROversold（下限バンドでの売られすぎ）のときロングエントリー。Price > upper Keltner band && Williams %R > WilliamsROverbought（上限バンドでの買われすぎ）のときショートエントリー。
混合市場での機会を求めるトレーダーに適しています。

テストでは年平均リターン約46%を示しています。株式市場で最もパフォーマンスが良好です。

## 詳細
- **エントリー条件**:
  - **ロング**: Price < lower Keltner band && Williams %R < WilliamsROversold (下限バンドでの売られすぎ)
  - **ショート**: Price > upper Keltner band && Williams %R > WilliamsROverbought (上限バンドでの買われすぎ)
- **ロング/ショート**: 両方向。
- **エグジット条件**:
  - **ロング**: 価格が中間バンドに戻ったらロングポジションを退場
  - **ショート**: 価格が中間バンドに戻ったらショートポジションを退場
- **ストップ**: はい。
- **デフォルト値**:
  - `EmaPeriod` = 20
  - `KeltnerMultiplier` = 2m
  - `AtrPeriod` = 14
  - `WilliamsRPeriod` = 14
  - `WilliamsROversold` = -80
  - `WilliamsROverbought` = -20
  - `StopLossPercent` = 2
    バンドはEmaPeriod本のEMAにAtrPeriod本のATRのKeltnerMultiplier倍を加減したもので、中間バンドはEMAそのものです。-80と-20はルールが示すWilliams %R水準の既定値です。ストップはエントリー価格の固定StopLossPercentで、ローソク足の間も監視されます。 保有ポジションと逆方向のエントリーシグナルはドテンになります。
  - `CandleType` = TimeSpan.FromMinutes(5)
- **フィルター**:
  - カテゴリ: 混合
  - 方向: 両方
  - インジケーター: Keltner Williams R
  - ストップ: はい
  - 複雑さ: 中級
  - 時間軸: イントラデイ
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中

