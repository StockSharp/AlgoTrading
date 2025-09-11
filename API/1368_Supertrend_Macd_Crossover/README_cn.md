# Supertrend + MACD 交叉策略
[English](README.md) | [Русский](README_ru.md)

该策略将 Supertrend 指标与 MACD 交叉结合，用于识别做多信号。
当价格位于 Supertrend 线上方且 MACD 线上穿信号线时开多仓。
当价格跌破 Supertrend 且 MACD 线下穿信号线时平仓。

## 详情

- **指标**: Supertrend (ATR 10, 系数 3), MACD (12, 26, 9)
- **入场**: 价格高于 Supertrend 且 MACD 看涨交叉
- **出场**: 价格低于 Supertrend 且 MACD 看跌交叉
- **方向**: 仅做多
- **周期**: 任意
