# 简单 APF 策略回测
[English](README.md) | [Русский](README_ru.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

该策略实现了简化的自相关价格预测模型。策略通过自相关检测价格周期，并使用最近收益的线性回归来预测未来价格。当预测收益超过设定阈值且没有持仓时，策略开多。达到目标价位后平仓。

## 参数

- `Length` – 用于自相关和回归的柱数。
- `Threshold Gain` – 入场所需的最小预期涨幅。
- `Signal Threshold` – 存储预测所需的自相关水平。
- `Candle Type` – 计算使用的K线类型。
