# 血腥街市买入策略
[English](README.md) | [Русский](README_ru.md)

当当前回撤相对于最近高点低于标准差阈值时买入，并在固定数量的K线后退出。

## 细节

- **入场条件**：
  - 多头：回撤 ≤ 均值 + `StdDevThreshold` × 标准差
- **多空方向**：仅多头
- **出场条件**：在`ExitBars`根K线后平仓
- **止损**：无
- **默认值**：
  - `LookbackPeriod` = 50
  - `StdDevLength` = 50
  - `StdDevThreshold` = -1m
  - `ExitBars` = 35
  - `CandleType` = TimeSpan.FromMinutes(1).TimeFrame()
- **筛选**：
  - 分类：反转
  - 方向：多头
  - 指标：Highest、SMA、StandardDeviation
  - 止损：否
  - 复杂度：基础
  - 时间框架：中期
  - 季节性：否
  - 神经网络：否
  - 背离：否
  - 风险等级：中
