# Starter Edge 策略
[English](README.md) | [Русский](README_ru.md)

该策略使用基于 ZLEMA 的 MACD，并结合 EMA 100 趋势过滤和 RSI 确认。

## 细节

- **入场**：价格高于 EMA100 且 MACD 线上穿信号线并且 RSI 高于 50 时做多；反向条件做空。
- **出场**：反向 MACD 交叉、MACD 柱状图转跌或 RSI 离开超买/超卖区域。
- **指标**：ZLEMA、MACD、EMA 100、RSI。
- **类型**：趋势跟随。

