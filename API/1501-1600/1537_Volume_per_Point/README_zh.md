# 体积每点策略
[English](README.md) | [Русский](README_ru.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

该策略计算每根K线的每点成交量。当K线波动范围缩小时成交量增加并且启用的RSI过滤器确认信号时开多仓。 当波动范围扩大而成交量减少时开空仓。

## 参数
- **RSI长度** – RSI计算周期。
- **RSI高/低阈值** – 可选RSI过滤器的阈值。
- **使用RSI过滤器** – 启用或禁用RSI过滤。
- **K线类型** – 输入K线的时间框架。
