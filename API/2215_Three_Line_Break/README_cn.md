# 三线突破策略
[English](README.md) | [Русский](README_ru.md)

该策略利用 Three Line Break 指标来捕捉趋势反转。
指标将当前的最高价和最低价与前 N 根已完成蜡烛的最高点和最低点进行比较。
在下跌趋势中向上突破最近高点表示新一轮上升趋势并开多单；在上升趋势中跌破最近低点则开空单。
每当出现相反信号时，仓位会反向。

## 细节

- **入场条件**：
  - 多头：`Downtrend` 变为 `Uptrend`
  - 空头：`Uptrend` 变为 `Downtrend`
- **多/空**：双向
- **出场条件**：相反信号（反手）
- **止损**：无
- **默认值**：
  - `LinesBreak` = 3
  - `CandleType` = TimeSpan.FromHours(12).TimeFrame()
- **筛选**：
  - 分类：趋势跟随
  - 方向：双向
  - 指标：Highest, Lowest（Three Line Break 逻辑）
  - 止损：无
  - 复杂度：基础
  - 时间框架：波段
  - 季节性：无
  - 神经网络：无
  - 背离：无
  - 风险等级：中等
