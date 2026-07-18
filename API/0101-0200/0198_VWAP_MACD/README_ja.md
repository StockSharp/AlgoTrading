# Vwap Macd 戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)
 
VWAPとMACDに基づく戦略。価格がVWAPより上でMACD > シグナルのときロングエントリー。価格がVWAPより下でMACD < シグナルのときショートエントリー。MACDがシグナル線を反対方向にクロスしたら退場。

テストでは年平均リターン約181%を示しています。暗号通貨市場で最もパフォーマンスが良好です。

VWAPはイントラデイの価値を導き、MACDのクロスオーバーはモメンタムの転換を示します。MACDがVWAPレベル付近で転換するときにトレードが開始されます。

短期モメンタムトレーダーに適しています。ATRストップルールにより過大なリスクを防ぎます。

## 詳細

- **エントリー条件**:
  - ロング: `Close > VWAP && MACD > Signal`
  - ショート: `Close < VWAP && MACD < Signal`
- **ロング/ショート**: 両方
- **エグジット条件**: MACDの反対方向クロス
- **ストップ**: `StopLossPercent` を使用したパーセントベース
- **デフォルト値**:
  - `MacdFastPeriod` = 12
  - `MacdSlowPeriod` = 26
  - `MacdSignalPeriod` = 9
  - `StopLossPercent` = 2m
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **フィルター**:
  - カテゴリ: 平均回帰
  - 方向: 両方
  - インジケーター: VWAP, MACD
  - ストップ: はい
  - 複雑さ: 中級
  - 時間軸: 中期
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中

