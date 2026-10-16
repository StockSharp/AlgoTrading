# Fibonacci 与布林带策略
[English](README.md) | [Русский](README_ru.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

当价格收盘于布林带下轨之下且仍高于最近摆动高低点计算出的61.8%斐波那契扩展位时，策略建立多头仓位。当价格到达上轨或前高时平仓。

**可验证规则与默认值：**`BollingerPeriod=20`、`BollingerDeviation=2`、`FibLookback=50`、`FibonacciLevel=0.618`、`CandleType=1分钟`。布林带基于**包括当前信号K线**的最近已收盘K线计算均值和总体标准差。此前高低点来自**之前**`FibLookback`根K线，排除当前K线。斐波那契价格=`此前最低价 + FibonacciLevel × (此前最高价 − 此前最低价)`（自低点向上61.8%；本简化策略使用回撤水平价，而不是摆动区间外的延伸）。仅在空仓且`Close < 下轨 && Close > Fib价格`时**开多**；`Close >= 上轨 || Close >= 此前最高价`时全部平多。无空头、无止损，不读取未来K线。
