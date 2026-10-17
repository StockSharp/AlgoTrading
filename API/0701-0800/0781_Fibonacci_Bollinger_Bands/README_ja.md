# FibonacciとBollinger Bands戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

ボリンジャーバンドと直前レンジの安値より下のフィボナッチ拡張を使う、ロング専用の平均回帰戦略です。

既定値：`BollingerPeriod = 20`、`BollingerDeviation = 2`、`FibLookback = 50`、`FibonacciLevel = 0.618`、`CandleType = 1分`。確定足のみ使用します。バンドはシグナル足を含む直近20終値の平均と母標準偏差から計算します。レンジはシグナル足を除く直前50確定足の最高 High と最低 Low です。

下方拡張は `F = previousLow − FibonacciLevel × (previousHigh − previousLow)`。1.618など1を超える正の係数にも対応します。建玉なしで `Close < lowerBand` かつ `Close > F` の場合のみ買います。`Close >= upperBand` または `Close >= previousHigh` で全ロングを決済します。F と等しい場合は買いません。ショート、ストップ注文、将来足の参照はありません。
