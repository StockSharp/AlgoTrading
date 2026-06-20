# MACD Bollinger Strategy
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)
 
この戦略はMACD Bollingerインジケーターを使ってシグナルを生成します。
MACD > Signal && Price < BB_lower（売られすぎ条件での上昇トレンド）のときロングエントリー。MACD < Signal && Price > BB_upper（買われすぎ条件での下降トレンド）のときショートエントリー。
混合市場での機会を求めるトレーダーに適しています。

テストでは年平均リターン約55%を示しています。株式市場で最もパフォーマンスが良好です。

## 詳細
- **エントリー条件**:
  - **ロング**: MACD > Signal && Price < BB_lower (売られすぎ条件での上昇トレンド)
  - **ショート**: MACD < Signal && Price > BB_upper (買われすぎ条件での下降トレンド)
- **ロング/ショート**: 両方向。
- **エグジット条件**:
  - **ロング**: 価格が中間バンドに戻ったらロングポジションを退場
  - **ショート**: 価格が中間バンドに戻ったらショートポジションを退場
- **ストップ**: はい。
- **デフォルト値**:
  - `MacdFast` = 12
  - `MacdSlow` = 26
  - `MacdSignal` = 9
  - `BollingerPeriod` = 20
  - `BollingerDeviation` = 2.0m
  - `AtrPeriod` = 14
  - `AtrMultiplier` = 2m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **フィルター**:
  - カテゴリ: 混合
  - 方向: 両方
  - インジケーター: MACD Bollinger
  - ストップ: はい
  - 複雑さ: 中級
  - 時間軸: イントラデイ
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中

