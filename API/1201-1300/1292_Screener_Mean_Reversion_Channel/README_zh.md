# Screener Mean Reversion Channel 策略
[English](README.md) | [Русский](README_ru.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

该策略使用移动平均线和ATR构建均值回归通道。当价格收于上轨之上时做空，收于下轨之下时做多。当价格回到均线附近时平仓。

## 细节
- 入场：收盘高于上轨做空，低于下轨做多。
- 出场：价格回到均线。
- 指标：SMA 和 ATR。
- 止损：无。
