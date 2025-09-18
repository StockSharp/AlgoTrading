# Andrew's Pitchfork Strategy

Port of the MetaTrader expert advisor "Andrew's Pitchfork". The original script expected a manually drawn Andrews' Pitchfork object and combined it with momentum, multi-timeframe moving averages and MACD filters. The StockSharp version keeps the indicator stack, replaces the manual drawing with automatic trend detection and recreates the protective logic (multi-entry limits, stop-loss, take-profit, break-even and trailing management).

## Strategy logic

1. **Indicators**
   - Two *Linear Weighted Moving Averages* (LWMA) calculated on the typical price of the selected candle series.
   - A *Momentum* oscillator on the same timeframe, evaluated by the absolute deviation from the equilibrium level 100.
   - A classic *MACD (12, 26, 9)* signal line pair.
2. **Entry rules**
   - **Long** trades require the fast LWMA to be above the slow LWMA, at least one of the last three momentum deviations to exceed the `MomentumBuyThreshold`, and the MACD line to be above its signal line.
   - **Short** trades invert these conditions.
   - The strategy pyramids by repeatedly adding the base `Volume` while the absolute position is below `Volume * MaxPyramids`. Opposite signals close the current exposure before opening the new direction.
3. **Risk management**
   - Initial stop-loss and take-profit levels are placed in price steps around the entry. Both are updated whenever the position size changes.
   - Break-even logic moves the stop after the price has travelled a configurable number of steps in favour of the position.
   - Trailing stop logic keeps following the most profitable price with an additional padding distance.

Compared with the MQL version, the StockSharp port automatically infers the trend using LWMA slope instead of checking the orientation of a user-drawn Pitchfork object. All other filters (momentum, MACD, multi-order limit) and money management tools were reproduced with StockSharp's high-level API.

## Parameters

| Name | Type | Default | Description |
|------|------|---------|-------------|
| `CandleType` | `DataType` | 15-minute time frame | Primary candle series used by all indicators. |
| `FastMaPeriod` | `int` | 6 | Length of the fast LWMA on typical price. |
| `SlowMaPeriod` | `int` | 85 | Length of the slow LWMA on typical price. |
| `MomentumPeriod` | `int` | 14 | Momentum indicator lookback. |
| `MomentumBuyThreshold` | `decimal` | 0.3 | Minimum \|Momentum - 100\| for long entries. |
| `MomentumSellThreshold` | `decimal` | 0.3 | Minimum \|Momentum - 100\| for short entries. |
| `MaxPyramids` | `int` | 1 | Maximum number of base lots allowed in the same direction. |
| `StopLossSteps` | `int` | 20 | Stop-loss distance expressed in price steps. |
| `TakeProfitSteps` | `int` | 50 | Take-profit distance expressed in price steps. |
| `EnableTrailing` | `bool` | `true` | Enables dynamic trailing stop. |
| `TrailingTriggerSteps` | `int` | 40 | Profit in steps required before the trailing stop activates. |
| `TrailingDistanceSteps` | `int` | 40 | Distance in steps maintained between the price extreme and trailing stop. |
| `TrailingPadSteps` | `int` | 10 | Extra padding applied to the trailing stop. |
| `EnableBreakEven` | `bool` | `true` | Enables break-even stop adjustment. |
| `BreakEvenTriggerSteps` | `int` | 30 | Profit in steps needed before moving the stop to break-even. |
| `BreakEvenOffsetSteps` | `int` | 30 | Offset in steps beyond entry when break-even is applied. |

## Notes

- The strategy requires a valid `PriceStep` from the selected security to convert step-based distances into prices. If the step is missing the trailing and break-even logic remain dormant.
- Protective orders (stop and take-profit) are recreated whenever the position size changes, ensuring that scaling-in or reversing aligns the orders with the new exposure.
- The default parameters match the original EA configuration but can be optimised via the built-in `StrategyParam` ranges.
