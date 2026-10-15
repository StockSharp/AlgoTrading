# RSIダイバージェンス戦略 - AliferCrypto
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

RSIダイバージェンスを利用した戦略で、オプションのゾーンおよびトレンドフィルターを備えます。ストップロスとテイクプロフィットは、スイングまたはATRから計算でき、動的または静的に更新されます。

## ロジック
- **エントリー**
  - 強気ダイバージェンス: 価格が安値を切り下げる一方、RSIが安値を切り上げる。
  - 弱気ダイバージェンス: 価格が高値を切り上げる一方、RSIが高値を切り下げる。
  - オプションのRSIゾーンフィルターは、事前の売られすぎ/買われすぎ状態を必要とする。
  - オプションのトレンドフィルターは移動平均の方向を使用する。
- **エグジット**
  - 直近のスイングまたはATRからSL/TP。
  - レベルはエントリー時に固定するか、各バーで再計算できる。

## インジケーター
- Relative Strength Index
- Moving Average
- Average True Range
- Highest/Lowest
