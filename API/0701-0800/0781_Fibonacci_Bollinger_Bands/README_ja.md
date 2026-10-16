# FibonacciとBollinger Bands戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

直近のスイング高値と安値から算出した61.8%のFibonacci延長水準を上回りつつ、終値がBollinger Bandsの下バンドを下回るときに買いポジションを建てます。価格が上バンドまたは直前のスイング高値に到達したときにポジションをクローズします。

**検証可能なルールと初期値:** `BollingerPeriod=20`、`BollingerDeviation=2`、`FibLookback=50`、`FibonacciLevel=0.618`、`CandleType=1分`。バンドはシグナル足を**含む**確定終値の移動平均±母標準偏差×倍率です。直前の高安はシグナル足を除く**過去**`FibLookback`本の最高値と最安値です。Fib価格＝`過去の安値 + FibonacciLevel × (過去の高値−過去の安値)`（安値から61.8%上、今回の簡略実装では外側への伸長でなく戻りの水準）。ポジションなしで`Close < 下側バンド && Close > Fib`なら**ロングのみ**。`Close >= 上側バンド || Close >= 過去の高値`で全決済。ショート、ストップ、未来データはありません。
