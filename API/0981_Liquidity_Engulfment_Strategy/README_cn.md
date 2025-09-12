# 流动性吞没策略
[English](README.md) | [Русский](README_ru.md)

该策略在价格触及最近流动性高点或低点后出现的看涨或看跌吞没形态时开仓。交易模式可配置，并使用固定点数的止损及可选止盈。

## 细节

- **入场条件**：
  - **做多**：触及下方流动性后的看涨吞没。
  - **做空**：触及上方流动性后的看跌吞没。
- **离场条件**：反向信号、止损或止盈。
- **方向**：可配置（默认同时做多和做空）。
- **指标**：Highest、Lowest。
- **止损**：`StopLossPips`，可选 `TakeProfitPips`。
- **默认值**：
  - `CandleType` = 1 分钟
  - `UpperLookback` = 10
  - `LowerLookback` = 10
  - `StopLossPips` = 10
  - `TakeProfitPips` = 20
  - `Mode` = Both
