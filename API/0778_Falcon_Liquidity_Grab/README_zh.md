# Falcon Liquidity Grab Strategy
[English](README.md) | [Русский](README_ru.md)

该策略在主要市场时段内交易流动性抓取，使用简单移动平均线确定趋势。当价格突破近期摆动高低点后沿趋势反转时入场。每笔交易都使用以 tick 为单位的固定止损和止盈。

## 细节

- **入场条件**：
  - **做多**：`Low < 周期最低` && `Close > SMA` && `会话过滤`
  - **做空**：`High > 周期最高` && `Close < SMA` && `会话过滤`
- **离场条件**：固定止损和止盈。
- **类型**：反转
- **指标**：SMA、Highest、Lowest
- **时间框架**：15 分钟（默认）
- **止损**：`StopLossPoints` tick，`TakeProfitMultiplier`× 止损距离
