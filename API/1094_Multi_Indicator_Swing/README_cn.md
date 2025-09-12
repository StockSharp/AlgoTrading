# 多指标摆动策略
[English](README.md) | [Русский](README_ru.md)

结合 Parabolic SAR、SuperTrend、ADX 与成交量差确认的摆动交易策略。

## 详情

- **入场条件**：所有启用的指标一致。
- **多/空**：双向。
- **出场条件**：反向信号或触及止损/止盈。
- **止损**：可选百分比水平。
- **默认参数**：
  - `CandleType` = TimeSpan.FromMinutes(2)
  - `PsarStart` = 0.02m
  - `PsarIncrement` = 0.02m
  - `PsarMaximum` = 0.2m
  - `AtrPeriod` = 10
  - `AtrMultiplier` = 3m
  - `AdxLength` = 14
  - `AdxThreshold` = 25m
  - `DeltaLength` = 14
  - `DeltaSmooth` = 3
  - `DeltaThreshold` = 0.5m
  - `StopLossPercent` = 2m
  - `TakeProfitPercent` = 4m
- **筛选**：
  - 分类：Trend
  - 方向：Both
  - 指标：PSAR、SuperTrend、ADX、Volume
  - 止损：Yes
  - 复杂度：Intermediate
  - 时间框架：Intraday (2m)
  - 季节性：No
  - 神经网络：No
  - 背离：No
  - 风险级别：Medium
