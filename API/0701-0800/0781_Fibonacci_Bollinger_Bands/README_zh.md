# Fibonacci 与布林带策略
[English](README.md) | [Русский](README_ru.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

仅做多的均值回归策略，结合布林带与前一波段低点下方的斐波那契扩展。

默认值：`BollingerPeriod = 20`、`BollingerDeviation = 2`、`FibLookback = 50`、`FibonacciLevel = 0.618`、`CandleType = 1分钟`。只使用已完成 K 线。布林带按含信号 K 线的最近20个收盘价和总体标准差计算。波段为之前50根已完成 K 线的最高 High 和最低 Low，不含信号 K 线。

向下扩展为 `F = previousLow − FibonacciLevel × (previousHigh − previousLow)`。支持大于1的正系数，例如1.618。空仓时严格满足 `Close < lowerBand` 且 `Close > F` 才买入。当 `Close >= upperBand` 或 `Close >= previousHigh` 时平掉全部多仓。等于 F 不入场。不做空、不设止损单、不使用未来 K 线。
