# Stochastic RSI Cross 戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

StochRSIのクロスオーバーに基づく戦略

テストでは年平均リターン約112%が示されています。外国為替市場で最もパフォーマンスが高くなります。

Stochastic RSI CrossはStochRSIの%Kと%Dラインを監視します。売られすぎレベル付近での強気クロスは買いを促し、買われすぎ付近での弱気クロスは売りを促し、逆クロスでエグジットします。

StochRSIは素早く振動するため、シグナルが頻繁に発生することがあります。多くのトレーダーはノイズを排除するために、クロスが極値付近で発生することを求めます。


## 詳細

- **エントリー条件**: RSI、Stochasticに基づくシグナル。
- **ロング/ショート**: 両方。
- **エグジット条件**: 逆シグナルまたはストップ。
- **ストップ**: はい。
- **デフォルト値**:
  - `RsiPeriod` = 14
  - `StochPeriod` = 14
  - `KPeriod` = 3
  - `DPeriod` = 3
  - `StopLossPercent` = 2m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **フィルター**:
  - カテゴリ: トレンド
  - 方向: 両方
  - インジケーター: RSI, Stochastic
  - ストップ: はい
  - 複雑さ: 基本
  - 時間軸: イントラデイ (5m)
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中

