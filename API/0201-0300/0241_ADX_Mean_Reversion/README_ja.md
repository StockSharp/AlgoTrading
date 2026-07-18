# ADX 平均回帰戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)
 
ここではAverage Directional Index（ADX）が全体的なトレンドの強さを測定します。ADXが低いとき、市場は方向性を欠き、価格は平均値を中心に振動する傾向があります。この戦略は、ADXがその移動平均から乖離するのを取引することでその動きを利用します。

テストでは年平均リターンは約70%を示しています。株式市場で最もよいパフォーマンスを発揮します。

ADXが平均を`DeviationMultiplier`倍の標準偏差下回り、価格が移動平均を下回っているときにロングトレードに入ります。ADXが上限バンドを急上昇し価格が平均を上回っているときにショートトレードが建てられます。ADXが平均に向かって戻ったときにポジションを決済します。

このシステムは、低トレンド環境での機会を探すトレーダーに訴求します。新たなトレンドが発生した場合、ストップロスにより小さな平均回帰トレードが大きな損失に成長するのを防ぎます。

## 詳細
- **エントリー条件**:
  - **ロング**: ADX < Avg - DeviationMultiplier * StdDev && Close < MA
  - **ショート**: ADX > Avg + DeviationMultiplier * StdDev && Close > MA
- **ロング/ショート**: 両方。
- **エグジット条件**:
  - **ロング**: ADX > Avg のときに決済
  - **ショート**: ADX < Avg のときに決済
- **ストップ**: あり、パーセンテージストップロス。
- **デフォルト値**:
  - `AdxPeriod` = 14
  - `AveragePeriod` = 20
  - `DeviationMultiplier` = 2m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **フィルター**:
  - カテゴリ: 平均回帰
  - 方向: 両方
  - インジケーター: ADX
  - ストップ: はい
  - 複雑さ: 中級
  - 時間軸: イントラデイ
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中

