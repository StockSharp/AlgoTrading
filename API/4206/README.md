# MACD Not So Sample Strategy

## Overview
The MACD Not So Sample strategy is a conversion of the MetaTrader expert advisor *MACD_Not_So_Sample*. The original robot trades
a 4-hour EURUSD chart using MACD crossovers confirmed by an EMA trend filter, combined with large take-profit levels and a
trailing stop. The StockSharp version keeps the same structure: the MACD histogram must be negative and cross above its signal
line for a long entry, while a positive histogram crossing below the signal produces a short entry. A trend EMA must confirm the
direction before any position is opened.

All money-management features are implemented in StockSharp: the strategy sets a configurable take-profit target, manages a
trailing stop once price travels far enough, and closes trades when the MACD crosses in the opposite direction with sufficient
strength. The port uses StockSharp indicators and high-level candle subscriptions so all calculations happen on finalized H4
candles, mirroring the MetaTrader behaviour.

## Trading logic
1. Subscribe to the timeframe defined by `CandleType` (defaults to 4-hour candles) and process only finished candles.
2. Feed a `MovingAverageConvergenceDivergenceSignal` indicator with the configured `FastPeriod`, `SlowPeriod`, and
   `SignalPeriod`. The indicator provides both the MACD line and the signal line.
3. Calculate an EMA trend filter with length `TrendPeriod`. Its slope determines whether long or short entries are allowed.
4. Convert the pip-based thresholds (`MacdOpenLevelPips`, `MacdCloseLevelPips`, `TakeProfitPips`, `TrailingStopPips`) to absolute
   price distances using the instrument’s pip size.
5. When no position exists:
   - Open a **long** position if the MACD is below zero, the current value is above the signal value, the previous MACD was below
     the previous signal, the EMA is rising, and the MACD magnitude exceeds `MacdOpenLevelPips`.
   - Open a **short** position if the MACD is above zero, the current value is below the signal value, the previous MACD was above
     the previous signal, the EMA is falling, and the MACD magnitude exceeds `MacdOpenLevelPips`.
6. While holding a long position:
   - Close the trade when the MACD becomes positive, crosses below the signal, and its magnitude exceeds `MacdCloseLevelPips`.
   - Exit early if price reaches the configured take-profit or if the trailing stop level is breached.
7. While holding a short position:
   - Close the trade when the MACD turns negative, crosses above the signal, and its magnitude exceeds `MacdCloseLevelPips`.
   - Exit early if price hits the take-profit target or the trailing stop.
8. The trailing stop activates only after price moves beyond the threshold by `TrailingStopPips` and then locks in profit by
   following subsequent candle extremes.

## Parameters
| Name | Type | Default | Description |
| --- | --- | --- | --- |
| `FastPeriod` | `int` | `47` | Fast EMA length used inside the MACD calculation. |
| `SlowPeriod` | `int` | `166` | Slow EMA length used inside the MACD calculation. |
| `SignalPeriod` | `int` | `11` | EMA length of the MACD signal line. |
| `TrendPeriod` | `int` | `8` | Length of the EMA trend filter. |
| `MacdOpenLevelPips` | `decimal` | `1` | Minimum MACD magnitude (in pips) required to open a position. |
| `MacdCloseLevelPips` | `decimal` | `3` | Minimum MACD magnitude (in pips) required to close a position. |
| `TakeProfitPips` | `decimal` | `550` | Take-profit distance measured in pips. |
| `TrailingStopPips` | `decimal` | `19` | Trailing-stop distance measured in pips. A value of `0` disables trailing. |
| `TradeVolume` | `decimal` | `1` | Net volume used for market entries. |
| `CandleType` | `DataType` | 4-hour time frame | Candle series processed by the strategy. |
| `RequiredSecurityCode` | `string` | `EURUSD` | Security code that must match the selected instrument, mimicking the MetaTrader check. |

## Differences from the original MetaTrader expert
- MetaTrader manages individual orders and magic numbers. StockSharp works with net positions, so the conversion closes the
  current exposure and opens a new one instead of juggling multiple tickets.
- The original code used `AccountFreeMargin` to size positions dynamically. The StockSharp port exposes a simple `TradeVolume`
  parameter and documents that users should configure position sizing externally.
- Stop-loss adjustments use StockSharp’s candle extremes rather than modifying existing orders. Exits still occur on the first
  candle that violates the trailing stop, producing behaviour very close to the MetaTrader logic.
- All indicator calculations rely on StockSharp indicator classes bound through `SubscribeCandles`, without direct calls to
  `iMACD` or `iMA` functions.

## Usage notes
- Assign the desired instrument before starting the strategy. If the instrument code does not match `RequiredSecurityCode` the
  strategy stops immediately to prevent accidental deployment on the wrong market.
- `TradeVolume` is copied into `Strategy.Volume` during `OnStarted`, so helper methods (`BuyMarket`, `SellMarket`) always use the
  configured size.
- Trailing stops only become active after price advances beyond the configured distance; until then the strategy will rely on the
  MACD crossover and take-profit target for exits.
- Adding the strategy to a chart draws candles, both indicators, and executed trades so the crossover logic can be validated
  visually.

## Indicators
- `MovingAverageConvergenceDivergenceSignal` (MACD line and signal line).
- `ExponentialMovingAverage` (trend filter).
