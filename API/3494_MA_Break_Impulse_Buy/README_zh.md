# MA Break Impulse Buy 策略

## 概述
本策略按照 StockSharp 高层 API 重现 “M.A break mt4 buy” 智能交易顾问。核心思想是在一段低波动的整固之后捕捉强势的多头突破。交易逻辑依次检查多条指数移动平均线（EMA）、静默期以及触碰突破 EMA 的强势多头蜡烛。策略 **只做多**。

## 交易逻辑
1. **EMA 趋势过滤**
   - 在上一根完成的 K 线上计算两个 EMA 组合（`shift = 1`）。
   - `EMA(FirstFastPeriod)` 必须高于 `EMA(FirstSlowPeriod)`。
   - `EMA(SecondFastPeriod)` 必须高于 `EMA(SecondSlowPeriod)`。
2. **突破蜡烛筛选**
   - 突破蜡烛是上一根完成的 K 线（`shift = 1`）。
   - 其开盘价需高于 `TrendMaPeriod` 对应的 EMA。
   - 其最低价需触及或跌破 `BreakoutMaPeriod` EMA。
   - 蜡烛必须为阳线（`Close > Open`）。
   - 蜡烛振幅需位于 `CandleMinSize` 与 `CandleMaxSize` 之间（通过 `Security.PriceStep` 将点值转换为价格单位）。
   - 上影线不得超过 `UpperWickLimit`% 的蜡烛振幅，下影线至少为 `LowerWickFloor`% 的振幅。
3. **静默区与动能要求**
   - 回看突破蜡烛之前的 `QuietBarsCount` 根 K 线（`shift ≥ 2`），记录其中最大高低点差值。
   - 该静默区间需大于 `QuietBarsMinRange`（点值→价格）。
   - 突破蜡烛实体（`Close - Open`）必须 ≥ `ImpulseStrength × 静默区间`。
4. **持仓管理**
   - 当以上条件全部满足且当前无仓位时市价买入。
   - 使用 `StartProtection` 根据点值（结合 `PriceStep`）自动布置止损与止盈。

## 参数
| 参数 | 默认值 | 说明 |
|------|--------|------|
| `FirstFastPeriod` | 20 | 第一组趋势过滤的快速 EMA 周期。 |
| `FirstSlowPeriod` | 30 | 第一组趋势过滤的慢速 EMA 周期。 |
| `SecondFastPeriod` | 30 | 第二组趋势过滤的快速 EMA 周期。 |
| `SecondSlowPeriod` | 50 | 第二组趋势过滤的慢速 EMA 周期。 |
| `TrendMaPeriod` | 30 | 要求突破蜡烛开盘价高于的 EMA 周期。 |
| `BreakoutMaPeriod` | 20 | 突破蜡烛最低价需要触碰的 EMA 周期。 |
| `QuietBarsCount` | 2 | 统计静默区所需的 K 线数量。 |
| `QuietBarsMinRange` | 0.0 | 静默区间的最小点值。 |
| `ImpulseStrength` | 1.1 | 将静默区间放大后的突破实体要求。 |
| `UpperWickLimit` | 100.0 | 上影线占蜡烛振幅的最大百分比。 |
| `LowerWickFloor` | 0.0 | 下影线占蜡烛振幅的最小百分比。 |
| `CandleMinSize` | 0.0 | 突破蜡烛允许的最小振幅（点值）。 |
| `CandleMaxSize` | 100.0 | 突破蜡烛允许的最大振幅（点值）。 |
| `VolumeSize` | 0.01 | 市价买单的下单量，会按照交易所的 `VolumeStep` 归一化。 |
| `StopLossPips` | 20.0 | 止损距离（点值，通过 `PriceStep` 转换）。 |
| `TakeProfitPips` | 20.0 | 止盈距离（点值，通过 `PriceStep` 转换）。 |
| `CandleType` | 15 分钟 | 订阅的 K 线数据类型。 |

## 实现说明
- 使用高层 `Bind` 订阅保持指标与 K 线同步，不需要手工管理指标缓存。
- 仅对完成的 K 线（`CandleStates.Finished`）进行计算，避免使用未闭合数据。
- 静默区与蜡烛尺寸过滤会把点值参数通过 `Security.PriceStep` 转为价格单位；若标的未提供 `PriceStep`，则退化为 1，与原 MQL 中的 `PipValue` 逻辑一致。
- `StartProtection` 在 `OnStarted` 中只调用一次，之后的每笔新仓都会自动带上止损/止盈。
- 蜡烛历史只保留最近 `QuietBarsCount + 3` 根，以便快速评估静默期与突破蜡烛。

## 使用建议
- 请确保标的提供 `PriceStep`、`VolumeStep` 以及最小/最大下单量，这样点值与手数转换才准确。
- 可根据品种波动性调整 EMA 周期与动能阈值；降低 `ImpulseStrength` 会更敏感，高值则只接受更强的突破。
- 策略一次只允许一个仓位，若有外部持仓可能阻止新的入场信号。

