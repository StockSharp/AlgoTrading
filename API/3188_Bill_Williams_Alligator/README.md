# Bill Williams Alligator Strategy

This strategy ports the MetaTrader 5 expert advisor **"Bill Williams.mq5"** by Vladimir Karputov to the StockSharp high-level API. It subscribes to a single candle series, rebuilds Bill Williams fractal points, and evaluates breakouts relative to the shifted Alligator lines. When the current candle closes beyond the nearest up or down fractal and that fractal sits outside all three Alligator curves (Jaw, Teeth, Lips), the system opens a position. Optional money-management features reproduce the original inputs such as stop-loss, take-profit, trailing stop, signal reversal, and automatic closing of opposite positions.

## Trading Logic

1. **Fractal detection** – every finished candle updates rolling buffers of highs and lows. The algorithm scans up to `FractalsLookback` completed bars and finds the most recent confirmed up and down Bill Williams fractals (five-bar pattern).
2. **Alligator reconstruction** – the Median Price `(High + Low) / 2` feeds three `SmoothedMovingAverage` instances representing the jaw, teeth, and lips. Their values are shifted forward by the configured number of bars to match the MetaTrader plotting rules.
3. **Breakout validation** – a long setup requires the latest up fractal to stay above the shifted jaw, teeth, and lips while the most recent candle closes above the fractal price. A short setup mirrors the logic below the Alligator.
4. **Order execution** – by default the strategy opens a single market order with `OrderVolume` when the breakout is detected and no position is held. If `CloseOppositePositions` is enabled, an opposite position is flattened before opening a new one. Setting `ReverseSignals = true` swaps the breakout sides to reproduce the EA's reverse mode.
5. **Risk management** – configurable stop-loss and take-profit levels are stored internally and evaluated on every candle. The trailing stop activates once the market moves by `TrailingStopPips + TrailingStepPips` in the trade direction and keeps stepping as the price advances. Stops are expressed in "pips" derived from the security `PriceStep`, including the MetaTrader 3- or 5-digit adjustment.

## Parameters

| Name | Description | Default |
| ---- | ----------- | ------- |
| `OrderVolume` | Trade size in lots or contracts used for market entries. | `0.1` |
| `StopLossPips` | Initial stop-loss distance in pips. Set to `0` to disable. | `50` |
| `TakeProfitPips` | Take-profit distance in pips. Set to `0` to disable. | `50` |
| `TrailingStopPips` | Trailing stop distance in pips. `0` disables trailing logic. | `10` |
| `TrailingStepPips` | Extra pip gain required before the trailing stop moves again. Must be positive when trailing is enabled. | `5` |
| `JawPeriod` | Length of the smoothed moving average used for the Alligator jaw (blue). | `13` |
| `JawShift` | Forward shift for the jaw values, measured in bars. | `8` |
| `TeethPeriod` | Length of the teeth smoothed moving average (red). | `8` |
| `TeethShift` | Forward shift for the teeth values. | `5` |
| `LipsPeriod` | Length of the lips smoothed moving average (green). | `5` |
| `LipsShift` | Forward shift for the lips values. | `3` |
| `FractalsLookback` | Number of completed candles scanned when searching for the most recent confirmed fractals. | `100` |
| `ReverseSignals` | When `true`, buy signals come from down-fractal breakouts and sell signals come from up-fractal breakouts. | `false` |
| `CloseOppositePositions` | When `true`, the strategy closes an existing opposite position before entering a new trade. | `false` |
| `CandleType` | Candle series used for calculations and signals. | `TimeFrame(1h)` |

## Notes

- The strategy operates strictly on **finished candles** and ignores intrabar ticks, matching the original Expert Advisor's bar-by-bar workflow.
- To emulate the MetaTrader 5 pip calculation, the strategy multiplies the exchange `PriceStep` by 10 when the security has 3 or 5 decimal places.
- Protective orders and the trailing stop are managed internally. When a stop or target condition is met within the next candle, the position is closed at market to mimic the EA's order management.
- The Alligator indicators are drawn automatically if a chart area is available, allowing visual comparison between the StockSharp port and the MetaTrader template.
- Python and test projects are intentionally omitted according to the repository guidelines for new conversions.
