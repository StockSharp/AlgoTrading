# 2DLimits

## Overview
2DLimits is a direct port of the MetaTrader 4 expert advisor `2DLimits_EA_v2`. The strategy evaluates the last two completed daily candles and only participates when they form a stair-step pattern (higher highs/lows or lower highs/lows). When the pattern is valid, the strategy submits stop orders at the previous day's extreme and protects the position with a midpoint stop-loss and a target equal to the prior daily range.

The implementation relies on StockSharp's high-level candle subscriptions together with level-1 quotes. Daily candles supply the breakout levels while the best bid/ask snapshots ensure that long setups are only armed when price trades below the midpoint and short setups only when price trades above it.

## Strategy logic
### Daily structure filter
* The strategy keeps a two-day rolling window of completed daily candles (configurable through the candle type parameter).
* A **bullish setup** requires the most recent day to register both a higher high and a higher low compared to the previous day.
* A **bearish setup** requires the most recent day to post both a lower high and a lower low than the earlier day.
* The midpoint of the latest day is calculated as `(high + low) / 2`, and the candle range is stored for the profit target.

### Entry rules
* Only one batch of pending orders is active at a time; all orders are cancelled and recalculated when a new daily candle closes.
* Long entries are prepared when:
  * The bullish structure filter is satisfied.
  * The latest ask price is below the midpoint of the previous day (mirrors the original EA's `Ask < middleY` check).
  * A buy-stop order is placed exactly at the previous day's high.
* Short entries are prepared when:
  * The bearish structure filter is satisfied.
  * The latest bid price is above the midpoint of the previous day (mirrors `Bid > middleY`).
  * A sell-stop order is placed at the previous day's low.
* If both structure checks fail, no orders are left working for the upcoming session.

### Exit rules
* When a stop order triggers, the opposing entry order is cancelled immediately so the strategy never holds simultaneous long and short exposures.
* After a long breakout fills, two protective orders are registered:
  * A stop order at the midpoint of the reference day acts as the stop-loss.
  * A take-profit order at `previous high + previous range` matches the MetaTrader take-profit distance.
* After a short breakout fills, symmetric protection is applied:
  * A stop order at the midpoint (buy-stop) covers the stop-loss.
  * A take-profit order at `previous low - previous range` mirrors the original target.
* Protective orders are re-armed whenever the filled position size changes and are removed once the position returns to flat.

### Order lifecycle and safety checks
* Pending orders are refreshed only after the next daily candle completes, enforcing a single setup per trading day.
* The strategy skips signal generation whenever it already holds a position, preventing overlaps between setups.
* The most recent bid/ask snapshot is retained from `SubscribeLevel1()`; if unavailable, the last trade price is used as a fallback to avoid submitting blind orders.
* Rounding is performed with the instrument's price step so all orders align with the exchange tick size.

## Parameters
| Name | Description |
| --- | --- |
| `Volume` | Order volume for the stop entries. Must be greater than zero. |
| `CandleType` | Candle type that provides the reference range (defaults to daily candles). |

## Additional notes
* The strategy manages every order directly through the high-level API; there is no reliance on custom collections or indicator buffers.
* Only the C# implementation is provided in this package. No Python version is created for this conversion.
* Tests are untouched as requested.
