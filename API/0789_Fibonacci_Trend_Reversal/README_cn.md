# 斐波那契趋势反转策略

该策略基于最近的高点和低点构建斐波那契通道。当价格向突破方向穿越50%水平时开仓。风险控制采用ATR止损和风险收益比的止盈，并可选择部分平仓。

## 参数
- **Candle Type** — K线序列。
- **Sensitivity** — 通道计算的基础敏感度。
- **ATR Period** — ATR长度，用于止损。
- **ATR Multiplier** — ATR止损系数。
- **Risk Reward** — 盈利与风险倍数。
- **Use Partial TP** — 在第一个目标位平掉一半仓位。
- **Trade Direction** — 允许的交易方向。
