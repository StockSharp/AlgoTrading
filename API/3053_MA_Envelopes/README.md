# MA Envelopes Strategy

Converted from the MetaTrader 5 expert "MA Envelopes". The strategy looks for price retracements towards a moving average that is wrapped by an envelope channel. When a completed candle closes between the moving average and one of the envelope bands during the configured trading window, the strategy places limit entries at the moving average with protective exit orders derived from the envelope.

## Trading logic

1. A moving average is calculated with the selected method, price source and period. The same value is used to build symmetric envelope bands using the deviation parameter.
2. When a finished candle closes above the moving average but below the upper envelope band and the current ask price remains above the moving average, a staggered sequence of buy limit orders is prepared at the moving average price.
   * Each buy limit uses the lower envelope as the stop-loss level and the upper envelope plus an additional pip offset as the take-profit.
   * Up to three independent orders are managed, each with its own take-profit offset (`First`, `Second`, `Third` SL/TP parameters).
3. When a finished candle closes below the moving average but above the lower envelope band and the current bid price remains below the moving average, the logic is mirrored for sell limit orders.
4. The trading window is controlled by `StartHour` and `EndHour` (terminal time). After the end hour all still-active entry orders are cancelled.
5. Risk per trade is estimated through `MaximumRisk` and reduced after consecutive losses using `DecreaseFactor`. Order volume is aligned to the instrument’s volume step and limits.
6. Once an entry order is fully filled, protective stop-loss and take-profit orders are registered immediately. If an exit order is triggered, the counterpart order is cancelled and, if there is remaining position volume, new protective orders are issued for the rest.

## Parameters

| Parameter | Description |
|-----------|-------------|
| `MaximumRisk` | Fraction of available equity risked per position. |
| `DecreaseFactor` | Reduces position size after consecutive losing trades. |
| `First/Second/ThirdStopTakeProfitPips` | Pip distances added to the envelope bands for the three staged orders. |
| `StartHour`, `EndHour` | Trading session boundaries in terminal time (0–23). |
| `MaPeriod`, `MaShift`, `MaMethodType`, `AppliedPrice` | Moving-average configuration. |
| `EnvelopeDeviation` | Width of the envelope channel in percent. |
| `CandleType` | Timeframe of candles used for the calculations. |

## Notes

* Protective orders are recreated whenever only part of a position is closed, keeping the remaining size covered.
* Pending entry orders are cancelled at the end of the session; open positions remain managed by their protective orders.
* The strategy relies on order book updates to capture the latest bid/ask prices; candle close values are used as a fallback when order book data is unavailable.
