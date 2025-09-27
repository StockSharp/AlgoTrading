# D-BoT Alpha Short SMA 和 RSI 策略
[English](README.md) | [Русский](README_ru.md)

当 RSI 向上穿过设定阈值且价格低于简单移动平均线时做空。追踪止损跟随新的低点，当 RSI 达到止损或止盈水平时平仓。

## 细节

- **入场**：RSI 上穿入场水平且价格低于 SMA。
- **离场**：价格上穿追踪止损或 RSI 达到止损或止盈水平。
