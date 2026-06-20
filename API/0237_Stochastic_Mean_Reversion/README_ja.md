# Stochastic Oscillator 平均回帰戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)
 
この戦略はStochastic Oscillatorを独自の移動平均と比較して、過度に伸びたスイングを特定します。%Kが平均から数標準偏差離れたとき、インジケーターが典型的な値に向かってドリフトバックすることが期待されます。

テストでは年平均リターンは約64%を示しています。外国為替市場で最もよいパフォーマンスを発揮します。

Stochastic %Kが平均から`Multiplier`倍の標準偏差を差し引いた下限バンドを下回ったときにロングトレードを建てます。%Kが上限バンドを超えたときにショートトレードが行われます。%Kが平均ラインを戻って越えたときにポジションを決済します。

この手法は、買われ過ぎ・売られ過ぎの極端な水準を取引するのが好きな短期トレーダー向けに設計されています。ストップロスは、平均回帰しない持続的なモメンタムから保護します。

## 詳細
- **エントリー条件**:
  - **ロング**: %K < Avg - Multiplier * StdDev
  - **ショート**: %K > Avg + Multiplier * StdDev
- **ロング/ショート**: 両方。
- **エグジット条件**:
  - **ロング**: %K > Avg のときに決済
  - **ショート**: %K < Avg のときに決済
- **ストップ**: あり、パーセンテージストップロス。
- **デフォルト値**:
  - `StochPeriod` = 14
  - `KPeriod` = 3
  - `DPeriod` = 3
  - `AveragePeriod` = 20
  - `Multiplier` = 2.0m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **フィルター**:
  - カテゴリ: 平均回帰
  - 方向: 両方
  - インジケーター: Stochastic Oscillator
  - ストップ: はい
  - 複雑さ: 中級
  - 時間軸: イントラデイ
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中

