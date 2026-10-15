# Ta 策略
[English](README.md) | [Русский](README_ru.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

基于 MACD 金叉/死叉、支撑阻力枢轴、RSI 和 ADX 过滤的策略。采用两个分批止盈目标。

## 细节

- **入场**
  - **多头**：MACD 上穿信号线，价格高于阻力，RSI > 50，+DI > -DI，ADX > 20。
  - **空头**：MACD 下穿信号线，价格低于支撑，RSI < 50，-DI > +DI，ADX > 20。
- **离场**：两个止盈目标和一个止损。
