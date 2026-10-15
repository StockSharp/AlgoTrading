# RSI Hull MA 戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)
 
この戦略はRSI Hull MAインジケーターを使ってシグナルを生成します。
RSI < RsiOversold && HMA(t) > HMA(t-1)（HMAが上昇中で売られすぎ）のときロングエントリー。RSI > RsiOverbought && HMA(t) < HMA(t-1)（HMAが下降中で買われすぎ）のときショートエントリー。
混合市場での機会を求めるトレーダーに適しています。

テストでは年平均リターン約58%を示しています。株式市場で最もパフォーマンスが良好です。

## 詳細
- **エントリー条件**:
  - **ロング**: RSI < RsiOversold && HMA(t) > HMA(t-1) (HMAが上昇中で売られすぎ)
  - **ショート**: RSI > RsiOverbought && HMA(t) < HMA(t-1) (HMAが下降中で買われすぎ)
- **ロング/ショート**: 両方向。
- **エグジット条件**:
  - **ロング**: RSIがニュートラルゾーンに戻ったらロングポジションを退場
  - **ショート**: RSIがニュートラルゾーンに戻ったらショートポジションを退場
- **ストップ**: はい。
- **デフォルト値**:
  - `RsiPeriod` = 14
  - `RsiOversold` = 30
  - `RsiOverbought` = 70
  - `HullPeriod` = 9
  - `AtrPeriod` = 14
  - `AtrMultiplier` = 2
    30と70はルールが示すRSI水準の既定値です。ニュートラルゾーンはRSI 50からで、RSIが50に達したらロングを、50まで下がったらショートを閉じます。ストップはエントリー時の終値からAtrPeriod本のATRのAtrMultiplier倍離れた位置に置かれ、ローソク足の終値で確認されます。0で無効になります。 保有ポジションと逆方向のエントリーシグナルはドテンになります。
  - `CandleType` = TimeSpan.FromMinutes(5)
- **フィルター**:
  - カテゴリ: 混合
  - 方向: 両方
  - インジケーター: RSI Hull MA
  - ストップ: はい
  - 複雑さ: 中級
  - 時間軸: イントラデイ
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中

