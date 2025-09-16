# Parabolic SAR Bug5 策略

## 概述

Parabolic SAR Bug5 策略使用抛物线 SAR 指标识别价格反转。当价格上穿 SAR 时开多头，下穿 SAR 时开空头。策略可以反转交易方向，在 SAR 翻转时平掉已有仓位，并提供止损、止盈和跟踪止损。

## 入场规则

- 当价格上穿 SAR 且没有多头仓位时买入。
- 当价格下穿 SAR 且没有空头仓位时卖出。
- 如果启用 `Reverse`，信号方向反转。

## 出场规则

- 若启用 `SarClose`，当 SAR 发出相反信号时平仓。
- 固定止损和止盈目标。
- 若启用 `Trailing`，止损将根据进场后的最高价或最低价移动。

## 参数

| 参数 | 说明 |
|------|------|
| `Step` | Parabolic SAR 的初始加速因子。 |
| `Maximum` | Parabolic SAR 的最大加速因子。 |
| `StopLossPoints` | 止损距离（点）。 |
| `TakeProfitPoints` | 止盈距离（点）。 |
| `Trailing` | 是否启用跟踪止损。 |
| `TrailPoints` | 跟踪止损距离（点）。 |
| `Reverse` | 反转交易方向。 |
| `SarClose` | SAR 翻转时是否平仓。 |
| `CandleType` | 处理的 K 线周期。 |

