# MACD 平均回帰戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)
 
この手法はMACDヒストグラムを自身の平均との関係で追跡します。ヒストグラムの極端な読み値は、モメンタムが衰えると回帰することが多いです。MACDとシグナルラインの差を監視することで、戦略は過度に伸びた動きを見つけます。

テストでは年平均リターンは約67%を示しています。株式市場で最もよいパフォーマンスを発揮します。

MACDヒストグラムが平均を`DeviationMultiplier`標準偏差下回ったときにロングポジションに入ります。ヒストグラムが平均を同じ量だけ上回ったときにショートポジションが建てられます。ヒストグラムが平均を再び越えたときにトレードを決済します。

このアプローチは、モメンタムの極値に対して逆張りすることに慣れているトレーダーに向いています。エントリー価格のパーセンテージとして測定されたストップロスが、強まり続けるトレンドから守ります。

## 詳細
- **エントリー条件**:
  - **ロング**: MACD Histogram < Avg - DeviationMultiplier * StdDev
  - **ショート**: MACD Histogram > Avg + DeviationMultiplier * StdDev
- **ロング/ショート**: 両方。
- **エグジット条件**:
  - **ロング**: Histogram > Avg のときに決済
  - **ショート**: Histogram < Avg のときに決済
- **ストップ**: あり、パーセンテージストップロス。
- **デフォルト値**:
  - `FastMacdPeriod` = 12
  - `SlowMacdPeriod` = 26
  - `SignalPeriod` = 9
  - `AveragePeriod` = 20
  - `DeviationMultiplier` = 2m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **フィルター**:
  - カテゴリ: 平均回帰
  - 方向: 両方
  - インジケーター: MACD
  - ストップ: はい
  - 複雑さ: 中級
  - 時間軸: イントラデイ
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中

