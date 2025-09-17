# Flat Channel 策略

**Flat Channel Strategy** 是 MetaTrader 5 智能交易系统 *Flat Channel (barabashkakvn's edition)* 的 StockSharp 高级 API 版本。策略完整复刻原始流程：通过平滑后的标准差识别波动率收缩区间，区间内的最高价和最低价定义水平通道，并在通道两侧稍作偏移后挂出买入/卖出止损单。一旦市场向任一方向突破，策略即以预设的止损和止盈水平进场，并可根据利润情况启动移动止损。

## 工作原理

1. **检测波动率收缩**：`StandardDeviation` 指标（周期为 `StdDevPeriod`）再经过 `SmoothingLength` 周期的 `SimpleMovingAverage` 平滑。当平滑序列连续 `FlatBars` 个值不再上升时，视为进入盘整区间，并重新允许挂出突破订单。
2. **构建通道**：确认盘整后，使用内置 `Highest` / `Lowest` 指标获取最近 `max(ChannelLookback, FlatBars + 1)` 根蜡烛的最高价和最低价。通道高度会通过 `ChannelMinPips` 与 `ChannelMaxPips` 过滤，所有以点数表示的距离均通过 `PipSize`（或自动检测到的最小变动价位）转换为价格单位。
3. **挂出止损单**：在未持仓且满足交易条件时，于 `high + IndentPips` 挂出买入止损单，于 `low − IndentPips` 挂出卖出止损单。下单时同时记录对应的止损和止盈价格。
4. **突破执行**：任意一侧的止损单成交后，另一侧的挂单立即取消。成交价成为移动止损的基准，之前计算的止损/止盈距离随即生效。
5. **仓位管理**：每根完成的蜡烛都会检测价格是否触及止损或止盈，若满足条件则直接以市价平仓。当 `TrailingStopPips` 大于零时，只要收盘价相对开仓价至少移动 `TrailingStopPips + TrailingStepPips`，系统就会沿着利润方向上调止损。
6. **交易时段过滤**：启用 `UseTradingHours` 后，策略仅在 `StartHour`（含）与 `EndHour`（不含）之间监控突破；若 `StartHour > EndHour` 则表示跨夜时段。

## 风险控制

- **固定或动态防护**：`StopLossPips` / `TakeProfitPips` 为正值时使用固定点差；若为零则按照通道高度和 `DynamicStopMultiplier` / `DynamicTakeMultiplier` 计算动态距离。
- **移动止损**：设置 `TrailingStopPips` 后，仓位进入盈利区间会触发移动止损，`TrailingStepPips` 控制每次调整的最小幅度。
- **仓位上限**：`MaxPositions` 将净头寸限制在 `MaxPositions × TradeVolume` 以内，超出阈值时不再挂出新单。
- **方向过滤**：`UseBuy` 与 `UseSell` 可分别启用多头突破、空头突破或双向模式。

## 参数

| 参数 | 默认值 | 说明 |
|------|--------|------|
| `TradeVolume` | `1` | 每笔挂单的成交量。 |
| `PipSize` | `0.0001` | 单个点对应的价格变化。设为 0 时根据品种的最小变动价位自动推算（含 3/5 位报价补偿）。 |
| `StdDevPeriod` | `46` | 基础标准差指标的回溯周期。 |
| `SmoothingLength` | `3` | 用于平滑标准差的移动平均周期。 |
| `FlatBars` | `3` | 连续不增加的平滑波动率个数，达到后重新允许挂单。 |
| `ChannelLookback` | `5` | 在检测到盘整时用于求最高/最低价的蜡烛数量，与 `FlatBars + 1` 取较大值。 |
| `ChannelMinPips` | `15` | 通道允许的最小高度（点）。设为 `0` 关闭该过滤。 |
| `ChannelMaxPips` | `105` | 通道允许的最大高度（点）。设为 `0` 关闭该过滤。 |
| `DynamicStopMultiplier` | `1` | 当 `StopLossPips = 0` 时，动态止损使用的通道高度倍数。 |
| `DynamicTakeMultiplier` | `1` | 当 `TakeProfitPips = 0` 时，动态止盈使用的通道高度倍数。 |
| `StopLossPips` | `0` | 固定止损距离（点），为正时覆盖动态算法。 |
| `TakeProfitPips` | `0` | 固定止盈距离（点），为正时覆盖动态算法。 |
| `IndentPips` | `0` | 在通道边界基础上额外添加的点数偏移。 |
| `TrailingStopPips` | `5` | 移动止损的基础距离（点）。设为 `0` 表示不启用。 |
| `TrailingStepPips` | `5` | 每次调整移动止损时的最小间隔（点）。 |
| `UseBuy` | `true` | 允许多头突破（买入止损单）。 |
| `UseSell` | `true` | 允许空头突破（卖出止损单）。 |
| `MaxPositions` | `5` | 允许的最大净仓位，以 `TradeVolume` 为单位倍数。 |
| `UseTradingHours` | `true` | 启用交易时段过滤。 |
| `StartHour` | `0` | 允许交易的开始小时（含）。 |
| `EndHour` | `23` | 允许交易的结束小时（不含）。 |
| `CandleType` | `H1` | 计算所用的蜡烛类型（默认 1 小时）。 |

## 其他说明

- 策略仅在蜡烛收盘后运行，使用 `SubscribeCandles().Bind(...)` 等高层封装，确保回测与实时行为一致。
- 所有价格都会通过 `Security.ShrinkPrice` 归一化，以满足交易所规定的最小报价步长。
- 任一挂单成交后，另一侧挂单会立即取消，因此同一时间只会维护一个方向的突破仓位。
