# Fractal AMA MBK 交叉策略

## 概述
Fractal AMA MBK 交叉策略将 **Fractal Adaptive Moving Average (FRAMA)** 与 **指数移动平均线 (EMA)** 触发线结合使用。当 FRAMA 与 EMA 相交时产生交易信号。

## 工作原理
- FRAMA 根据价格波动的分形维度自动调整平滑系数。
- EMA 作为触发线，对价格数据进行平滑。
- **做多：** 当 FRAMA 向上穿越 EMA 且当前没有多单时开多。
- **做空：** 当 FRAMA 向下穿越 EMA 且当前没有空单时开空。
- 可选的止损和止盈用于保护已打开的仓位。

## 参数
| 名称 | 说明 |
| --- | --- |
| `CandleType` | 计算所用的蜡烛类型和时间框架（默认：4 小时）。 |
| `FramaPeriod` | FRAMA 指标的周期。 |
| `SignalPeriod` | EMA 触发线的周期。 |
| `StopLoss` | 距离开仓价的止损（价格单位，0 表示关闭）。 |
| `TakeProfit` | 距离开仓价的止盈（价格单位，0 表示关闭）。 |
| `Volume` | 交易手数。 |

## 备注
- 仅处理已完成的蜡烛。
- 交易通过市价单 (`BuyMarket`/`SellMarket`) 执行。
- `FramaPeriod` 和 `SignalPeriod` 参数支持优化。
