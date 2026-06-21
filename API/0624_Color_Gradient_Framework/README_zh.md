# 颜色渐变框架策略
[English](README.md) | [Русский](README_ru.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

演示如何基于RSI计算从红到绿的颜色渐变，并在RSI穿越中线时交易。

## 逻辑
- 计算可配置周期的相对强弱指数。
- 生成用于可视化的颜色渐变。
- 当RSI上穿50时买入。
- 当RSI下穿50时卖出。
