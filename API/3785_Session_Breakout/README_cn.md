# Session Breakout 策略

## 概述
Session Breakout 策略移植自 MetaTrader 专家顾问 "Session breakout"。策略在欧洲早盘统计价格区间，当区间足够
狭窄时，便在美国时段寻找突破机会。实现基于 StockSharp 的高级 API，每个交易日最多只开一次多单和一次
空单，并自动为持仓附加止损与止盈。

## 交易逻辑
- 每个交易日开始时重置内部状态；周末不交易。是否在周一交易由参数控制。
- 在欧洲时段（默认 06:00–12:00）跟踪收盘的 K 线，记录最高价和最低价。
- 美国时段开始时，如果区间宽度小于 `SmallSessionThresholdPips`（以点数计算），则认为出现了窄幅震荡。
- 若满足窄幅条件，则在美国时段（默认 12:00–16:00）继续观察，等待至少一根美国时段 K 线收盘（`EuropeSess
ionStartHour + 5` 到 `EuropeSessionStartHour + 10` 之间）。
- 当整根 K 线位于欧洲高点之上并超过缓冲（`BreakoutBufferPips`）时触发做多；当整根 K 线低于欧洲低点减去缓
冲时触发做空。
- 入场后立即根据点数参数设置止损和止盈，并禁止当天再次在同一方向开仓。

## 参数
| 参数 | 说明 |
|------|------|
| `Volume` | 多空突破使用的下单手数。 |
| `EuropeSessionStartHour` | 开始统计欧洲区间的小时。 |
| `EuropeSessionEndHour` | 结束统计欧洲区间的小时。 |
| `UsSessionStartHour` | 美国交易窗口的开始小时。 |
| `UsSessionEndHour` | 美国交易窗口的结束小时。 |
| `SmallSessionThresholdPips` | 判断欧洲区间是否足够窄的最大点数。 |
| `BreakoutBufferPips` | 触发突破前加在区间上的额外缓冲点数。 |
| `TradeOnMonday` | 是否允许在周一交易（周末始终禁用）。 |
| `TakeProfitPips` | 入场价到止盈价的距离（点）。 |
| `StopLossPips` | 入场价到止损价的距离（点）。 |
| `CandleType` | 用于计算的 K 线类型，默认 15 分钟。 |

## 说明
- 点值根据合约的 `PriceStep` 推导，请根据标的物规格调整相关参数。
- 策略在符合条件的 K 线收盘时下单，因此回测中成交价等于该 K 线的收盘价。实时环境可能因流动性产生滑点。
- 每个交易日最多允许一笔多单和一笔空单，逻辑忠实还原原始专家顾问，同时利用 StockSharp 的风控辅助函数。
