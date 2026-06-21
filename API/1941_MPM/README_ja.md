# MPMモメンタム戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

この戦略は、元のMQLエキスパート `mpm-1_8.mq4` を簡略化して変換したものです。
連続する進行ローソク足の並びを待ち、同方向にポジションを開きます。
Average True Rangeはローソク足のサイズを評価し、ストップをトレールするために使用されます。

## パラメーター

| 名前 | 説明 |
| ---- | ---- |
| `ProgressiveCandles` | 取引をトリガーするために必要な連続したローソク足の数。 |
| `ProgressiveSize` | 進行とみなすためのATRに対する最小ローソク足実体サイズ。 |
| `StopRatio` | ストップレベルのトレールに使用するATRの比率。 |
| `AtrPeriod` | Average True Rangeインジケーターの期間。 |
| `CandleType` | 戦略が使用するローソク足のタイプ。 |
| `ProfitPerLot` | ロットあたりの利益目標。 |
| `BreakEvenPerLot` | ブレークイーブンで決済するために必要な利益。 |
| `LossPerLot` | ロットあたりの最大許容損失。 |

## ロジック

1. 完成した各ローソク足で実体のサイズがATRと比較されます。
2. 実体が `ProgressiveSize` 閾値を超えると、強気または弱気カウンターが増加します。
3. 一方向に `ProgressiveCandles` 本が確認されると、成行注文が送信されます。
4. ストップレベルはATRの `StopRatio` 分トレールされます。
5. ストップが発動するか、利益/損失目標に達したときにポジションが決済されます。
