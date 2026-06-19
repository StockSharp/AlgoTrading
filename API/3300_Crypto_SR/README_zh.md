# Crypto SR 策略

Crypto SR 策略将 MetaTrader 4 的 "Crypto S&R" 专家顾问迁移到 StockSharp 的高级 API。该实现保留了原始系统的多重过滤结构：主周期使用两条线性加权移动平均线（LWMA）识别趋势，高阶时间框架上的 Momentum 提供动量确认，长周期 MACD 过滤宏观方向，同时通过比尔·威廉姆斯的分形构建支撑/阻力水平。策略使用市价单入场，并通过固定止损/止盈、移动到保本以及按点数计算的跟踪止损管理持仓。

## 交易逻辑

1. **主时间框架** —— 订阅配置的蜡烛序列，对典型价格 `(High + Low + Close) / 3` 计算快、慢两条 LWMA。做多需要快线高于慢线，做空则相反。
2. **高阶 Momentum 过滤** —— 在第二组蜡烛上计算 Momentum 指标。最近三次 Momentum 值与 100 的绝对差必须超过买入或卖出阈值。
3. **长期 MACD 过滤** —— 第三组蜡烛用于计算 MACD(12, 26, 9)。多头要求 MACD 主线高于信号线，空头要求主线低于信号线。默认使用日线级别来近似原策略中的月线，若数据源提供真实月线可自行调整。
4. **分形支撑/阻力** —— 将已完成的蜡烛保存在滑动窗口中。当出现经典的分形形态（两侧各两根蜡烛）时，对应的高点或低点成为新的阻力或支撑。为了复现原 EA 绘制的水平线，可为分形价格添加可调的点数缓冲区。
5. **入场条件**：
   - *做多*：当前无多头仓位，快 LWMA 高于慢 LWMA，Momentum 偏离值达到买入阈值，MACD 看多，当前蜡烛触及支撑缓冲并收于前一根蜡烛收盘价之上。
   - *做空*：条件与做多相反，使用阻力缓冲、卖出阈值及 MACD 看空确认。
6. **风控与持仓管理** —— 新开仓位会设置固定的止损和止盈点数。当收益达到触发值后可将止损移动到保本价，若启用跟踪止损则按蜡烛高/低价动态更新。若 MACD 过滤条件逆转，仓位将立即平仓。

## 实现说明

- 原版 EA 使用的月线 MACD 在此实现中默认改为日线，因为 StockSharp 没有现成的按日历月聚合。若需要，可改用自定义月线数据源。
- 当止损、止盈或保护条件触发时，策略通过市价单平仓，与 MQL 中的 `OrderClose` 调用保持一致，不依赖交易所托管的止损单。
- 指标更新全部通过 `SubscribeCandles().Bind(...)` 高级接口完成，无需调用 `GetValue` 访问内部缓冲。

## 参数

| 名称 | 说明 | 默认值 |
| --- | --- | --- |
| `FastMaPeriod` | 主时间框架上快速 LWMA 的周期。 | `6` |
| `SlowMaPeriod` | 主时间框架上慢速 LWMA 的周期。 | `85` |
| `MomentumPeriod` | 高阶时间框架上的 Momentum 周期。 | `14` |
| `MomentumBuyThreshold` | Momentum 偏离 100 的最小值（做多）。 | `0.3` |
| `MomentumSellThreshold` | Momentum 偏离 100 的最小值（做空）。 | `0.3` |
| `MacdFastPeriod` | 长期 MACD 的快速 EMA 周期。 | `12` |
| `MacdSlowPeriod` | 长期 MACD 的慢速 EMA 周期。 | `26` |
| `MacdSignalPeriod` | 长期 MACD 的信号 EMA 周期。 | `9` |
| `StopLossPips` | 固定止损距离（点数）。 | `20` |
| `TakeProfitPips` | 固定止盈距离（点数）。 | `50` |
| `TrailingStopPips` | 跟踪止损距离（点数，0 表示关闭）。 | `40` |
| `UseBreakEven` | 是否在达到目标后移动到保本价。 | `true` |
| `BreakEvenTriggerPips` | 启动保本逻辑所需的盈利点数。 | `30` |
| `BreakEvenOffsetPips` | 移动到保本时额外添加的偏移。 | `30` |
| `FractalWindowLength` | 确认分形所需的窗口长度。 | `7` |
| `FractalBufferPips` | 分形水平周围的缓冲点数。 | `10` |
| `TradeVolume` | 每次下单的交易量。 | `1` |
| `CandleType` | 主时间框架蜡烛类型。 | `15m` 时间框架 |
| `HigherCandleType` | Momentum 使用的高阶蜡烛类型。 | `1h` 时间框架 |
| `LongTermCandleType` | MACD 过滤使用的蜡烛类型。 | `1d` 时间框架 |

