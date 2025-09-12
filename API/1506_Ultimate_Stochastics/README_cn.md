# Ultimate Stochastics 策略
[English](README.md) | [Русский](README_ru.md)

该策略基于随机指标的交叉信号，可同时做多和做空。可选在出现反向信号时平仓，并按百分比设置止盈和止损。

## 细节

- **做多**：%K 在超卖区域向上穿越 %D。
- **做空**：%K 在超买区域向下穿越 %D。
- **指标**：Stochastic。
- **止损**：可选百分比 TP/SL。
