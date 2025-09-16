# RobotPowerM5 Meta4 V12 Strategy

## Overview
The RobotPowerM5 Meta4 V12 strategy is a C# port of the MetaTrader 4 expert advisor `RobotPowerM5_meta4V12.mq4`. The original EA
was designed for five-minute Forex charts and evaluates the balance between Bulls Power and Bears Power to decide whether a new
long or short position should be opened. The StockSharp version keeps the one-position-at-a-time behaviour, reproduces the point-
based stop-loss / take-profit settings, and reimplements the trailing-stop logic that gradually locks in profits once the market
moves in the trade's favour.

## Trading Logic
1. **Indicator engine**
   - Five-minute candles are subscribed by default (the timeframe is configurable through the `CandleType` parameter).
   - A pair of StockSharp indicators, `BullsPower` and `BearsPower`, are updated on every finished candle using the configured
     averaging period.
   - The combined value `BullsPower + BearsPower` is stored with a one-bar delay in order to mimic the `shift=1` calls from the
     MQL code, which always operate on the last fully closed bar.
2. **Entry rules**
   - When no position is open and the delayed sum of Bulls/Bears Power is **positive**, a market buy order is issued.
   - When no position is open and the delayed sum is **negative**, a market sell order is issued.
   - Signals are ignored while a position is active; the trade is managed exclusively through protective exits.
3. **Volume handling**
   - The `Volume` parameter represents the requested lot size. It is passed directly to `BuyMarket` / `SellMarket`, allowing the
     connector to round to the instrument's lot step if necessary.

## Risk Management
- **Stop-loss** – The initial stop is placed `StopLossPoints` MetaTrader points away from the average fill price. The level is
  monitored with candle lows (for longs) or highs (for shorts); once touched the strategy exits at market.
- **Take-profit** – The profit target is `TakeProfitPoints` points from the entry and is evaluated on candle highs/lows, matching
  how MT4 closes positions when a target is hit intrabar.
- **Trailing stop** – After the price moves in the trade's favour by more than `TrailingStopPoints`, a trailing stop is activated.
  For long positions the stop is shifted to `referencePrice - trailingDistance`, where the reference is the maximum of the candle's
  close and high. For shorts the stop follows `referencePrice + trailingDistance`, using the minimum of the candle's close and low.
  This reproduces the EA's trailing behaviour that was originally implemented with `OrderModify`.

## Parameters
| Name | Description | Default | Notes |
| --- | --- | --- | --- |
| `BullBearPeriod` | Averaging period supplied to both Bulls Power and Bears Power indicators. | `5` | Increasing the value smooths the momentum filter. |
| `Volume` | Requested lot size for every entry. | `1` | The actual traded volume depends on the broker's lot step and limits. |
| `StopLossPoints` | Initial protective stop distance in MetaTrader points. | `45` | Set to `0` to disable the hard stop-loss. |
| `TakeProfitPoints` | Take-profit distance in MetaTrader points. | `150` | Set to `0` to trade without a fixed profit target. |
| `TrailingStopPoints` | Distance used by the trailing stop once the trade is profitable. | `15` | Set to `0` to disable trailing. |
| `CandleType` | Timeframe used for indicator calculations. | `5m time frame` | Any other `DataType` can be selected if needed. |

## Implementation Notes
- The strategy stores all risk levels (stop-loss, take-profit, trailing stop) internally and issues market exits when candles
  confirm that a price threshold was breached. This mirrors the MT4 approach where orders were modified tick-by-tick.
- Indicator subscriptions are wired via `Subscription.Bind`, which feeds both Bulls Power and Bears Power into a single callback.
- The point size is derived from `Security.PriceStep`, keeping the parameters compatible with instruments that quote in ticks,
  pips, or cents.
- Entry checks always use the *previous* indicator values, ensuring that partially formed candles never trigger orders.

## Differences vs. the MQL Version
- Trade management uses explicit market exits instead of modifying the stop-loss order in place; this is more robust across
  different StockSharp connectors while producing the same outcome.
- Parameter ranges are validated through `StrategyParam` helpers so that invalid values (for example, negative trailing stops) are
  rejected at configuration time.
- Detailed logging hooks, chart output, and candle subscriptions leverage StockSharp's high-level API instead of manual tick loops.
- The expert identifier string present in the MT4 script is not required in StockSharp and has therefore been omitted.
