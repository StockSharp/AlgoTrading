# JS MA SAR Trades 策略

JS MA SAR Trades 将 MetaTrader 5 专家顾问 "JS MA SAR Trades" 转换为 StockSharp 的高级 API。策略通过类 ZigZag 的摆动过滤器寻找更高的低点或更低的高点，使用两条移动平均线确认动量，并在价格突破 Parabolic SAR 时入场。仓位由传统止损、止盈、可选的移动止损以及明确的交易时段进行保护。

## 策略逻辑

1. **摆动结构**：使用 Highest/Lowest 指标（带有深度参数）来近似原始 ZigZag。系统跟踪最近两个摆动低点和高点。若最新低点高于前一个低点，则形成多头结构；若最新高点低于前一个高点，则形成空头结构。可设置的摆动偏差（以点为单位）和最小 backstep（两个枢轴之间的最少柱数）用于过滤噪声。
2. **移动平均确认**：两条均线共享相同的平滑方法和价格源，与 MT5 版本一致，并支持向右的正向平移。多头信号要求平移后的快线高于慢线；空头信号要求快线低于慢线。
3. **Parabolic SAR 触发**：只有在满足摆动和均线条件之后，且蜡烛收盘价在 SAR 之外（多头为收于 SAR 上方，空头为收于下方）才会下单。即便在交易窗口之外，SAR 翻转也会立即平仓。
4. **风险控制**：止损与止盈以点数设置，并通过标的 `PriceStep` 转换为价格差。移动止损复制 MT5 行为：只有当价格相对入场价前进了 `TrailingStop + TrailingStep` 的距离，止损价才会被提升或下移。
5. **时间过滤**：开启后，仅在设定的起止小时之间（包含边界）允许开仓。所有保护性检查（止损、移动止损、止盈、SAR 翻转）都会在每根完成的蜡烛上执行。

## 进出场规则

- **做多**：摆动低点抬高、SAR 在收盘价下方、快线（含平移）高于慢线，并且处于允许的交易时间内。下单量为 `OrderVolume + |Position|`，可同时平掉空头并建立多头。
- **做空**：摆动高点降低、SAR 在收盘价上方、快线低于慢线，且时间过滤通过。
- **平多**：
  - 收盘价跌破 SAR；
  - 触发止损、移动止损或止盈。
- **平空**：
  - 收盘价上破 SAR；
  - 触发止损、移动止损或止盈。

## 参数

| 参数 | 默认值 | 说明 |
|------|--------|------|
| `OrderVolume` | `1` | 新仓的基础数量；在反向开仓时会加上当前仓位的绝对值。 |
| `StopLossPips` | `50` | 入场价到止损的点数距离，`0` 表示禁用。 |
| `TakeProfitPips` | `50` | 入场价到止盈的点数距离，`0` 表示禁用。 |
| `TrailingStopPips` | `5` | 移动止损距离（点）。与 `TrailingStepPips` 配合使用。 |
| `TrailingStepPips` | `5` | 当价格朝盈利方向移动达到该额外距离（点）时才收紧移动止损；启用移动止损时必须大于 0。 |
| `UseTimeFilter` | `true` | 是否启用交易时间窗口。 |
| `StartHour` | `19` | 允许交易的起始小时（含）。 |
| `EndHour` | `22` | 允许交易的结束小时（含）。 |
| `FastMaPeriod` | `55` | 快速移动平均周期。 |
| `FastMaShift` | `3` | 快速均线的向右平移柱数。 |
| `SlowMaPeriod` | `120` | 慢速移动平均周期。 |
| `SlowMaShift` | `0` | 慢速均线的平移柱数。 |
| `MaType` | `Smoothed` | 移动平均类型（Simple、Exponential、Smoothed、Weighted）。 |
| `AppliedPrice` | `Median` | 均线使用的价格源（Close、Open、High、Low、Median、Typical、Weighted）。 |
| `SarStep` | `0.02` | Parabolic SAR 的初始加速因子。 |
| `SarMax` | `0.2` | Parabolic SAR 的最大加速因子。 |
| `ZigZagDepth` | `12` | 用于摆动识别的回溯柱数。 |
| `ZigZagDeviation` | `5` | 接受新枢轴所需的最小摆动幅度（点）。 |
| `ZigZagBackstep` | `3` | 相同类型枢轴之间的最少柱数。 |
| `CandleType` | `H1` | 交易时使用的蜡烛类型/周期。 |

## 说明

- 保护性逻辑始终运行，因此即使在交易窗口外也会依据止损或 SAR 翻转平仓。
- 移动止损完全复刻 MT5 行为：当价格前进 `TrailingStop + TrailingStep` 后，将止损设为 `Close - TrailingStop`（做空时对称）。
- 移动平均基于所选价格源计算，平移参数重现 MT5 指标的水平偏移。
- 请确保标的证券具有有效的 `PriceStep`，否则与点相关的距离无法计算。
