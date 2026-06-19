# Dual SuperTrend 策略（含 VIX 过滤）
[English](README.md) | [Русский](README_ru.md)

该策略结合两个 SuperTrend 指标和 VIX 波动率过滤器。当两个 SuperTrend 均为多头且 VIX 高于其均值时开多；当两个 SuperTrend 均为空头且 VIX 高于其均值并上升时开空。当任一 SuperTrend 反转时平仓。

## 详情

- **入场条件**：
  - **多头**：两个 SuperTrend 均为上升趋势且 VIX 高于其均值。
  - **空头**：两个 SuperTrend 均为下降趋势且 VIX 高于其均值并呈上升趋势。
- **出场条件**：
  - SuperTrend 方向反转。
- **止损**：无。
- **默认参数**：
  - `StLength1` = 13
  - `StMultiplier1` = 3.5
  - `StLength2` = 8
  - `StMultiplier2` = 5
  - `UseVixFilter` = true
  - `VixLookback` = 252
  - `VixTrendPeriod` = 10
  - `StdDevMultiplier` = 1
  - `EnableLong` = true
  - `EnableShort` = true
- **过滤**：
  - 分类：趋势跟随
  - 方向：双向
  - 指标：SuperTrend、SMA、StandardDeviation、EMA
  - 止损：无
  - 复杂度：中等
  - 时间框架：任意
  - 季节性：无
  - 神经网络：无
  - 背离：无
  - 风险等级：中等
