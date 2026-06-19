# MA on Momentum Min Profit 策略

## 概述
该策略复刻 MetaTrader 5 智能交易系统 **MA on Momentum Min Profit.mq5**。当 Momentum 指标向上穿越自身的动量均线并且上一根 K 线仍低于 100 水平时买入；当 Momentum 向下跌破均线且上一根 K 线高于 100 时卖出。实现中保留了原始版本的资金止损和以点数表示的固定止盈。

## 交易逻辑
1. 订阅 `CandleType` 指定的 K 线并计算 Momentum 指标。
2. 使用 `MomentumMovingAverageType` 与 `MomentumMovingAveragePeriod` 对 Momentum 序列进行平滑。
3. 通过上一根 K 线的动量值检测交叉，避免重复信号。
4. 沿用 MQL 版本的附加功能：
   - 反转买卖信号；
   - 在开仓前平掉反向持仓或者跳过进场；
   - 强制只保留一张净头寸；
   - 支持在当前尚未收盘的 K 线上触发信号。
5. 风险控制：
   - 资金止损：`PnL + Position * (close - PositionPrice)` 不得低于 `StopLossMoney`；
   - 将 `TakeProfitPoints` 乘以 `Security.PriceStep` 得到价格止盈距离。

## 参数
| 参数 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| `CandleType` | `DataType` | `TimeSpan.FromMinutes(5).TimeFrame()` | 用于计算 Momentum 的 K 线类型。 |
| `MomentumPeriod` | `int` | `14` | Momentum 指标的回溯长度。 |
| `MomentumMovingAveragePeriod` | `int` | `6` | 动量均线的周期。 |
| `MomentumMovingAverageType` | `MomentumMovingAverageType` | `Smoothed` | 动量均线算法（Simple、Exponential、Smoothed、Weighted）。 |
| `ReverseSignals` | `bool` | `false` | 反向执行买卖信号。 |
| `CloseOpposite` | `bool` | `true` | 开仓前关闭反向持仓。 |
| `OnlyOnePosition` | `bool` | `true` | 仅保留一张净头寸。 |
| `UseCurrentCandle` | `bool` | `false` | 在未收盘的当前 K 线上计算信号。 |
| `StopLossMoney` | `decimal` | `15` | 账户层面的资金回撤阈值。 |
| `TakeProfitPoints` | `decimal` | `460` | 以点数表示的止盈距离（乘以 `PriceStep`）。 |
| `MomentumReference` | `decimal` | `100` | 来自原策略的动量基准线。 |

## 实现说明
- 平滑处理使用 StockSharp 自带的 SMA/EMA/SMMA/WMA 指标，对应 `LengthIndicator<decimal>` 实例。
- 原策略的下单队列与 magic number 过滤在 StockSharp 中转换为净头寸逻辑：当启用 `CloseOpposite` 时会发送一张市价单，同时平掉反向仓位并建立新的方向。
- 资金止损通过调用 `CloseAll()` 平掉全部仓位，与 MQL 版本监控 `Commission + Swap + Profit` 的方式一致。
