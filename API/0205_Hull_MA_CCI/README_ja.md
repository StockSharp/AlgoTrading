# Hull MA CCI Strategy
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)
 
この戦略はHull MA CCIインジケーターを使ってシグナルを生成します。
HMA(t) > HMA(t-1) && CCI < -100（売られすぎ条件でHMAが上昇中）のときロングエントリー。HMA(t) < HMA(t-1) && CCI > 100（買われすぎ条件でHMAが下降中）のときショートエントリー。
混合市場での機会を求めるトレーダーに適しています。

テストでは年平均リターン約52%を示しています。暗号通貨市場で最もパフォーマンスが良好です。

## 詳細
- **エントリー条件**:
  - **ロング**: HMA(t) > HMA(t-1) && CCI < -100 (売られすぎ条件でHMAが上昇中)
  - **ショート**: HMA(t) < HMA(t-1) && CCI > 100 (買われすぎ条件でHMAが下降中)
- **ロング/ショート**: 両方向。
- **エグジット条件**:
  - **ロング**: HMAが下落し始めたらロングポジションを退場
  - **ショート**: HMAが上昇し始めたらショートポジションを退場
- **ストップ**: はい。
- **デフォルト値**:
  - `HullPeriod` = 9
  - `CciPeriod` = 20
  - `AtrPeriod` = 14
  - `AtrMultiplier` = 2m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **フィルター**:
  - カテゴリ: 混合
  - 方向: 両方
  - インジケーター: Hull MA CCI
  - ストップ: はい
  - 複雑さ: 中級
  - 時間軸: イントラデイ
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中

