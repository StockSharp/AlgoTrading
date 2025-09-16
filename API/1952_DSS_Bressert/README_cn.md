# DSS Bressert 策略

该策略使用 Double Smoothed Stochastic (DSS) Bressert 指标。计算两条线：

- **DSS** – 经过两次指数移动平均平滑的随机指标。
- **MIT** – 第一次平滑后的中间值。

当两条线交叉时产生信号：

- 当 DSS 线从上向下穿过 MIT 线时买入。
- 当 MIT 线从上向下穿过 DSS 线时卖出。

## 参数

| 参数 | 描述 |
|------|------|
| `EmaPeriod` | EMA 平滑周期（默认 8） |
| `StoPeriod` | 随机指标周期（默认 13） |
| `TakeProfitPercent` | 止盈百分比（默认 2） |
| `StopLossPercent` | 止损百分比（默认 1） |
| `CandleType` | 计算所用的时间框架（默认 4 小时） |

## 说明

- 策略仅在已完成的 K 线上运行。
- 保护机制使用百分比止盈和止损。
