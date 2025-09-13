# EMA WPR 趋势策略
[English](README.md) | [Русский](README_ru.md)

该策略结合 EMA 趋势过滤器和 Williams %R 指标信号。在超卖时买入，在超买时卖出。回撤阈值避免连续入场。可选的退出规则在 Williams %R 到达相反极值或在若干亏损 K 线后平仓。

## 详情

- **入场条件**：
  - 多头：Williams %R <= -100 且 EMA 趋势向上
  - 空头：Williams %R >= 0 且 EMA 趋势向下
- **多/空**：双向
- **出场条件**：
  - 启用 `UseWprExit` 时，Williams %R 穿越相反极值
  - 启用 `UseUnprofitExit` 时，仓位连续 `MaxUnprofitBars` 根 K 线无盈利
- **止损**：无
- **默认值**：
  - `WprPeriod` = 46
  - `WprRetracement` = 30
  - `EmaPeriod` = 144
  - `BarsInTrend` = 1
  - `MaxUnprofitBars` = 5
- **过滤器**：
  - 类别：Mean Reversion
  - 方向：双向
  - 指标：EMA, Williams %R
  - 止损：无
  - 复杂度：中等
  - 时间框架：日内
  - 季节性：无
  - 神经网络：无
  - 背离：无
  - 风险级别：中等
