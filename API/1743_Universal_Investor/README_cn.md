# Universal Investor Strategy

## 概述

**Universal Investor Strategy** 使用指数移动平均线（EMA）和线性加权移动平均线（LWMA）的交叉来判断市场方向，并确认两条均线的趋势一致性。

## 逻辑

- **买入**：LWMA 在 EMA 之上且两条均线同时上升。
- **卖出**：LWMA 在 EMA 之下且两条均线同时下降。
- **平多仓**：LWMA 从上向下穿越 EMA。
- **平空仓**：LWMA 从下向上穿越 EMA。

在启用减少系数时，连续亏损交易后仓位会减少。

## 参数

| 名称 | 说明 |
| ---- | ---- |
| `MovingPeriod` | EMA 和 LWMA 的计算周期。 |
| `DecreaseFactor` | 亏损后减少仓位的系数（0 表示不启用）。 |
| `CandleType` | 计算所用的蜡烛图类型。 |
| `Volume` | 策略设置中的基础交易量。 |

## 备注

- 仅处理完成的蜡烛。
- 使用带指示器绑定的 StockSharp 高级 API。
- 未提供 Python 版本。

