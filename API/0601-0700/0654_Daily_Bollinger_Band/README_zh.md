# Daily Bollinger Band 策略
[English](README.md) | [Русский](README_ru.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

该策略使用趋势过滤器和基于 ATR 的仓位管理，交易每日布林带突破。

## 细节

- **入场**：价格上穿上轨且斜率为正时做多，价格下穿下轨且斜率为负时做空。
- **离场**：价格再次穿越中轨时平仓。
