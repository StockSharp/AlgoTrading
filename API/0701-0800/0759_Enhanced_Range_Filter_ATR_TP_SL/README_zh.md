# 增强型范围过滤器策略与ATR止盈止损
[English](README.md) | [Русский](README_ru.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

该策略结合自定义范围过滤器与基于ATR的止盈和止损。
当价格突破过滤器并满足以下条件时入场：

- 成交量高于平均值
- RSI 位于设定范围内
- EMA 金叉或死叉确认趋势
- ATR 比率表明市场非震荡

持仓在触及ATR止损或止盈水平时平仓。
