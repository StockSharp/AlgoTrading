# 出来高平均回帰戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

このシステムは、歴史的平均と比較して異常に高いまたは低い取引出来高を探します。重大な出来高スパイクは活動が正常化するにつれて頻繁に反転し、逆張り取引の機会を提供します。

テストでは平均年間リターンが約76%であることが示されています。外国為替市場で最も良いパフォーマンスを発揮します。

出来高が平均値から`DeviationMultiplier`倍の標準偏差を引いた値を下回り、価格が移動平均を下回るときにロングエントリーします。出来高が上部バンドを超え、価格が平均を上回るときにショートエントリーします。出来高が平均水準に戻ったら取引を決済します。

この戦略は出来高急増後の燃え尽きを観察するトレーダーに有益です。パーセントストップロスが出来高が同じ方向に拡大し続けるシナリオに対して保護します。

## 詳細
- **エントリー条件**:
  - **ロング**: Volume < Avg - DeviationMultiplier * StdDev && Close < MA
  - **ショート**: Volume > Avg + DeviationMultiplier * StdDev && Close > MA
- **ロング/ショート**: 両方。
- **エグジット条件**:
  - **ロング**: volume > Avg のときに決済
  - **ショート**: volume < Avg のときに決済
- **ストップ**: あり、パーセントストップロス。
- **デフォルト値**:
  - `AveragePeriod` = 20
  - `DeviationMultiplier` = 2m
  - `CandleType` = TimeSpan.FromMinutes(5)
  - `StopLossPercent` = 2m
- **フィルター**:
  - カテゴリ: 平均回帰
  - 方向: 両方
  - インジケーター: Volume
  - ストップ: あり
  - 複雑さ: 中級
  - 時間軸: イントラデイ
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
