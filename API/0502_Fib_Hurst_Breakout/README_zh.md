# Fib Hurst Breakout
[English](README.md) | [Русский](README_ru.md)

**Fib Hurst Breakout** 将日线级别的斐波那契回撤与 Hurst 指数过滤器结合。当价格在趋势方向上突破关键斐波那契水平时入场，2% 的止损和 1:2 的盈亏比控制风险。

## 细节

- **入场条件**：
  - 多头：收盘价上穿 61.8% 且日线 Hurst > 0.5
  - 空头：收盘价下穿 38.2% 且日线 Hurst < 0.5
- **多空方向**：双向
- **退出条件**：止损或止盈
- **止损**：是
- **默认参数**：
  - `CandleType` = TimeSpan.FromMinutes(15)
  - `HurstPeriod` = 50
  - `MaxTradesPerDay` = 5
  - `MaxTotalTrades` = 510
  - `RiskPercent` = 2m
  - `RiskReward` = 2m
- **筛选**：
  - 类别: Breakout
  - 方向: 双向
  - 指标: Hurst, Fibonacci
  - 止损: 是
  - 复杂度: 中等
  - 时间框架: 日内
  - 季节性: 否
  - 神经网络: 否
  - 背离: 否
  - 风险等级: 中等
