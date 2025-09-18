# Template M5 Envelopes Strategy

Converted from the MetaTrader 4 expert advisor "Template_M5_Envelopes.mq4". The strategy tracks a linear weighted moving average (LWMA) envelope on five-minute candles and arms breakout stop orders whenever price stretches far enough away from the channel. Pending orders are dynamically repriced to follow the market, and filled positions are protected by configurable stop-loss, take-profit and trailing-stop logic.

## Trading logic

1. A LWMA based on the median candle price is calculated with the configured `EnvelopePeriod`. Upper and lower envelope bands are derived by applying the `EnvelopeDeviation` percentage.
2. Every finished five-minute candle stores its envelope values alongside the high and low. Signals are only evaluated once a full set of "previous" values is available, matching the MetaTrader implementation that referenced `iEnvelopes(..., shift = 1)` and the prior bar.
3. A **buy** setup appears when:
   * The previous candle low sits at least `DistancePoints` below the previous lower envelope, and
   * The current bid price remains at least `DistancePoints` below the same envelope value.
4. A **sell** setup mirrors the logic with the previous high and the upper envelope.
5. When a setup is active, only one stop order is allowed (the original EA also restricted itself to a single market or pending order). The order is placed at the current ask/bid plus the `EntryOffsetPoints` distance.
6. While the pending order remains active, the strategy monitors the market. If the difference between the order price and the current bid/ask exceeds `EntryOffsetPoints + SlippagePoints`, the order is cancelled and immediately re-registered at the new reference price, keeping the attached stop-loss and take-profit aligned with the desired offsets.
7. If the current spread exceeds `MaxSpreadPoints`, all pending entries are cancelled to avoid trading during unfavourable liquidity conditions.

## Order management

* Upon entry order activation, the strategy records the execution price and registers protective stop and take-profit orders at `StopLossPoints` and `TakeProfitPoints` offsets respectively. If either value is zero, the corresponding protection is skipped.
* The trailing stop module (enabled with `UseTrailingStop`) tracks the best bid/ask. Whenever price moves in favour of the open position by more than `TrailingStopPoints`, the stop order is repriced closer to the market using `ReRegisterOrder`. Long stops only trail upwards, while short stops only trail downwards.
* When the position is fully closed, all protective orders are cancelled and internal state is reset. No new entry orders are considered until the position returns to flat.

## Parameters

| Parameter | Description |
|-----------|-------------|
| `MaxSpreadPoints` | Maximum allowed spread before pending orders are cancelled. |
| `TakeProfitPoints` | Take-profit distance applied to filled positions. |
| `StopLossPoints` | Stop-loss distance applied to pending and filled positions. |
| `EntryOffsetPoints` | Offset (in points) from bid/ask where stop entries are placed. |
| `UseTrailingStop` | Enables trailing stop management for open positions. |
| `TrailingStopPoints` | Distance (in points) maintained by the trailing stop. |
| `FixedVolume` | Trading volume submitted with each entry order. |
| `EnvelopePeriod` | Length of the LWMA used as the envelope basis. |
| `EnvelopeDeviation` | Width of the envelope in percent. |
| `DistancePoints` | Minimum gap between price and envelope required for a signal. |
| `SlippagePoints` | Extra tolerance (in points) added to the repricing threshold. |
| `CandleType` | Timeframe used to calculate the LWMA envelope (default M5). |

## Notes

* The strategy subscribes to both candles and level-1 quotes. If bid/ask data is unavailable, entry conditions will not trigger because spread and trailing-stop calculations depend on it.
* Protective stop and take-profit orders are recreated with the latest volume whenever the trailing logic adjusts the stop-loss price.
* All comments inside the code are written in English, and tabs are used for indentation to match the project conventions.
