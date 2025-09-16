# MA Reverse Strategy

## Overview
The MA Reverse Strategy is a StockSharp conversion of the simple MetaTrader 4 expert advisor "MA_Reverse". The original robot
monitors how long the bid price stays above or below a 14-period simple moving average (SMA). After a long enough streak in one
direction, it opens a position betting on a short-term reversion. The StockSharp port keeps the same idea by counting the number
of consecutive finished candles closing above or below the SMA and executing a market order as soon as the configured threshold is
reached.

## Trading logic
- Subscribe to candles of the selected timeframe and calculate a simple moving average with the period defined by `SmaPeriod`.
- Maintain an integer counter (`StreakThreshold` controls the target length) that increments while the candle close remains above
the moving average and decrements while the close stays below it. Touching the moving average resets the counter.
- Once the counter hits `StreakThreshold` and the close is at least `MinimumDeviation` above the SMA, the strategy sells with a
market order. The assumption is that a prolonged bullish excursion from the moving average is likely to mean-revert.
- When the counter reaches `-StreakThreshold` and the close is at least `MinimumDeviation` below the SMA, the logic mirrors the
behaviour and opens a long position.
- After every trade the counter keeps its running value, just like the source EA, so that it can immediately start measuring the
next streak.

## Order management
- Market entries use the `TradeVolume` parameter. If there is an opposite position on the book, the strategy first closes it and
then opens the new trade in a single market order so that reversals match the MetaTrader behaviour.
- A global take-profit is configured through StockSharp's `StartProtection` helper. The distance equals `TakeProfitPoints`
multiplied by the security price step, reproducing the "30 * Point" profit target from the MQL code. When the target is hit the
position is closed with a market order.
- No stop-loss is implemented in the original expert and therefore none is added in the port. Risk control is entirely handled by
the take-profit and by the user's money management settings.

## Parameters
| Parameter | Description |
|-----------|-------------|
| `TradeVolume` | Lot size used for every market entry. The value is also used to size reversals when switching direction. |
| `SmaPeriod` | Number of candles used by the simple moving average. The default matches the 14-period moving average of the EA. |
| `StreakThreshold` | Number of consecutive closes that must stay on one side of the SMA before a reversal order is allowed. |
| `MinimumDeviation` | Minimum absolute distance between the close and the SMA that confirms the breakout condition. |
| `TakeProfitPoints` | Take-profit distance expressed in price steps. It is multiplied by the instrument's `PriceStep` to obtain the absolute price offset. |
| `CandleType` | Candle type (timeframe) used to calculate the SMA and evaluate the streak counters. |

## Notes
- The counter logic works with finished candles provided by `SubscribeCandles`, which makes the implementation robust and
compatible with historical testing. The behaviour matches the tick-based MetaTrader version as long as the candles are fine
grained enough to capture short-term excursions.
- Because StockSharp aggregates positions by default, multiple consecutive entries are managed as a single position with a single
floating take-profit distance. This is equivalent to having MetaTrader place the same take-profit on every order because the
distance from the current average entry price stays constant.
- The strategy does not add its own indicator to `Strategy.Indicators` because the binding infrastructure manages indicator
lifetime automatically.
- Always validate the price step and volume settings for your specific broker symbols so that the `TakeProfitPoints` parameter
produces the desired absolute target size.
