# OsHMA Breakdown Twist
[English](README.md) | [Русский](README_ru.md)

基于 OsHMA 振荡器（快慢 Hull 移动平均差值）的策略。它可以以两种模式运行：

- **Breakdown** – 当振荡器穿过零线时交易。
- **Twist** – 当振荡器改变方向时交易。

该策略订阅选定时间框架的 K 线，并使用 Hull 移动平均指标计算振荡器。

## 详情

- **入场条件**：OsHMA 零线突破或方向变化。
- **多空方向**：双向。
- **出场条件**：反向信号或止损/止盈。
- **止损/止盈**：支持。
- **默认值**：
  - `FastHma` = 13
  - `SlowHma` = 26
  - `Mode` = Twist
  - `TakeProfit` = 2000
  - `StopLoss` = 1000
  - `CandleType` = TimeSpan.FromHours(4)
- **过滤器**：
  - 分类：趋势
  - 方向：双向
  - 指标：MA
  - 止损：有
  - 复杂度：基础
  - 时间框架：4小时
  - 季节性：无
  - 神经网络：无
  - 背离：无
  - 风险等级：中
