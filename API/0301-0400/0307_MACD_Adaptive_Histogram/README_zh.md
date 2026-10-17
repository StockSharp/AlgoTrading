# MACD Adaptive Histogram 策略
[English](README.md) | [Русский](README_ru.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

交易统计上显著的 MACD 柱状值。使用已完成15分钟 K 线和 `H = MACD(12,26) − Signal(9)`，计算 H 的 `A = SMA(20)`、`D = StandardDeviation(20)`。等待全部指标形成且 `D > 0`。

空仓时，`H >= A + 2 × D && H > 0` 买入，`H <= A − 2 × D && H < 0` 卖出。`H <= A` 平全部多仓，`H >= A` 平全部空仓。每笔信号订单后暂停 `CooldownBars = 16` 根已完成 K 线的后续信号判断。

已成交持仓具有本地原生百分比保护：`StopLossPercent = 2`，以实际入场成交价为基准。监控标的的 bid/ask，并独立于信号冷却提交市价平仓；K 线之间需要报价流。无止盈。

`FastPeriod = 12; SlowPeriod = 26; SignalPeriod = 9; HistogramAvgPeriod = 20; StdDevMultiplier = 2; StopLossPercent = 2; CooldownBars = 16`.
