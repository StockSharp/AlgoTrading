# Screener Mean Reversion Channel 策略
[Русский](README_ru.md) | [English](README.md)

该策略使用移动平均线和ATR构建均值回归通道。当价格收于上轨之上时做空，收于下轨之下时做多。当价格回到均线附近时平仓。

## 细节
- 入场：收盘高于上轨做空，低于下轨做多。
- 出场：价格回到均线。
- 指标：SMA 和 ATR。
- 止损：无。
