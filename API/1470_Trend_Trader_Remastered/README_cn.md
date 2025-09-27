# Trend Trader-Remastered Strategy
[English](README.md) | [Русский](README_ru.md)

该策略基于 Parabolic SAR 指标。当价格向上穿越 SAR 时买入；向下穿越时卖出。相反方向的穿越会平掉当前持仓。

## 细节

- **入场条件**：
  - **多头**：价格上穿 PSAR。
  - **空头**：价格下穿 PSAR。
- **出场**：相反方向的 PSAR 穿越平仓。
- **止损**：无额外止损。
- **默认值**：
  - `Start` = 0.02
  - `Increment` = 0.02
  - `Max` = 0.2
