# TTP智能累积器
[English](README.md) | [Русский](README_ru.md)

当RSI低于其平均值一个标准差时，该策略会累积多头仓位；当RSI高于相同阈值时逐步退出。

## 详情

- **入场条件**: RSI < SMA(RSI, `MaPeriod`) - StdDev(RSI, `StdPeriod`)
- **多空方向**: 仅多头
- **退出条件**: RSI > SMA(RSI, `MaPeriod`) + StdDev(RSI, `StdPeriod`) 且利润高于 `MinProfit`
- **止损**: 无
- **默认值**:
  - `RsiPeriod` = 7
  - `MaPeriod` = 14
  - `StdPeriod` = 14
  - `AddWhileInLossOnly` = true
  - `MinProfit` = 0m
  - `ExitPercent` = 100m
  - `UseDateFilter` = false
  - `StartDate` = 2022-06-01
  - `EndDate` = 2030-07-01
  - `CandleType` = TimeSpan.FromHours(1)
- **过滤器**:
  - 类型: Mean Reversion
  - 方向: 多头
  - 指标: RSI, MA, StdDev
  - 止损: 无
  - 复杂度: 基础
  - 时间框架: 日内 (1h)
  - 季节性: 无
  - 神经网络: 无
  - 背离: 无
  - 风险等级: 中
