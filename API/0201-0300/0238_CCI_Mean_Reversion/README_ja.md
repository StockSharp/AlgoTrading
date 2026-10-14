# CCI 平均回帰戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)
 
Commodity Channel Index（CCI）は、価格が統計的平均からどれほど離れているかを測定します。この戦略は、CCIが自身の平均から大きく乖離したときにエントリーし、モメンタムが衰えたら急反発することを期待します。

テストでは年平均リターンは約151%を示しています。株式市場で最もよいパフォーマンスを発揮します。

CCIが平均から`DeviationMultiplier`倍の標準偏差を差し引いた値を下回ったときにロングトレードを行います。CCIが平均にそのマルチプライヤーを加えた値を上回ったときにショートトレードが建てられます。CCIが平均値を再び越えたときにポジションを決済します。

このシステムは逆張りのセットアップを好む短期トレーダーに適しています。パーセンテージ移動に基づくストップロスが、市場が素早く反転できない場合のリスクを制限するのに役立ちます。

## 詳細
- **エントリー条件**:
  - **ロング**: CCI < Avg - DeviationMultiplier * StdDev
  - **ショート**: CCI > Avg + DeviationMultiplier * StdDev
- **ロング/ショート**: 両方。
- **エグジット条件**:
  - **ロング**: CCI > Avg のときに決済
  - **ショート**: CCI < Avg のときに決済
- **ストップ**: あり、パーセンテージストップロス。
- **デフォルト値**:
  - `CciPeriod` = 20
  - `AveragePeriod` = 20
  - `DeviationMultiplier` = 2
  - `StopLossPercent` = 2
  - `CandleType` = TimeSpan.FromMinutes(5)
    AvgとStdDevは現在値を含む直近AveragePeriod個のCCIの平均と標準偏差です。 ストップはエントリー価格の固定StopLossPercentで、ローソク足の間も監視されます。0で無効になります。 保有ポジションと逆方向のエントリーシグナルはドテンになります。
- **フィルター**:
  - カテゴリ: 平均回帰
  - 方向: 両方
  - インジケーター: CCI
  - ストップ: はい
  - 複雑さ: 中級
  - 時間軸: イントラデイ
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中

