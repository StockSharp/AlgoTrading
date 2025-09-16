# II Outbreak Strategy

## Overview
The II Outbreak strategy is a high-frequency breakout system originally written for MetaTrader 4. It combines a proprietary timing oscillator with a volatility pressure gauge to enter strong directional moves, then manages trades using adaptive trailing stops and pyramiding. This conversion reproduces the original logic on top of the StockSharp high-level API and keeps the same guardrails for spread, volatility and calendar filters.

## Converted trading logic
### Timing oscillator
* Each new M1 candle contributes a "typical price" (average of high, low and close multiplied by 100) that feeds the legacy smoothing cascade.
* The cascade rebuilds the original nested moving average / difference pipeline (dtemp/atemp buffers) to produce a timing value from 0 to 100.
* Buy signal: timing value crosses upward over its previous reading (buffer[0] > buffer[1] with buffer[1] ≤ buffer[2]).
* Sell signal: timing value crosses downward (buffer[0] < buffer[1] with buffer[1] ≥ buffer[2]).

### Volatility filter
* A 10-period standard deviation on closing prices must stay below the `StdDevLimit`. When the limit is breached, no fresh positions are allowed and an optional warning is logged.
* A custom volatility score replicates the original amplitude × tick density formula: it uses the overlap between the current and previous minute candle and the average number of ticks per second. The score must exceed the configurable `VolatilityThreshold`.

### Entry rules
* The strategy works on a single symbol/timeframe pair supplied through the `CandleType` parameter (defaults to 1-minute candles).
* When no position is open and the calendar filter allows trading, the engine refreshes lot size through `CalculateOrderVolume()` and verifies current spread against `SpreadThreshold` (using level 1 bid/ask data).
* A long position is opened if the timing oscillator issues a buy signal and the volatility score is valid. A short position follows the mirrored condition. Upon entry, a static stop is placed two times the `TrailStopPoints` below/above the fill price.

### Pyramiding and trailing
* The trailing module activates once the aggregated position earns at least `TrailStopPoints + int(Commission) + SpreadThreshold` points of unrealized profit.
* The stop is tightened to `TrailStopPoints` behind the latest close (tracked separately for longs and shorts). Any improvement larger than one point updates the trailing price.
* As long as volatility, timing and spread conditions remain valid, the strategy can pyramid new orders every `max(10, SpreadThreshold + 1)` points of additional profit. New orders disable the static stop and rely purely on the trailing logic.

### Risk and capital management
* Position size is recalculated before each order: `balance × MaximumRisk ÷ (500000 / AccountLeverage)` rounded to the security volume step. If balance information is unavailable, it falls back to the `Volume` or minimum lot.
* A simplified margin check approximates the original MetaTrader guard (`volume × price / leverage × (1 + MaximumRisk × 190)`). Orders are ignored if the account value cannot cover that amount.
* After pyramiding is enabled, the strategy monitors floating loss. When the unrealized drawdown exceeds `TotalEquityRisk` percent of the account value, all positions are liquidated.

### Calendar & spread guardrails
* Trading stops on Fridays after 23:00 server time and during the last trading days of the year (day of year 358, 359, 365 or 366) after 16:00.
* Every entry and add-on checks the current bid/ask spread and skips execution if it breaches the configured threshold.

## Parameters
| Parameter | Default | Description |
|-----------|---------|-------------|
| `Commission` | 4 | Round-lot commission in points used when calculating the trailing activation offset. |
| `SpreadThreshold` | 6 | Maximum spread (in points) allowed for new entries or pyramiding. |
| `TrailStopPoints` | 20 | Trailing stop distance in points; the initial stop is twice this value. |
| `TotalEquityRisk` | 0.5 | Percentage of account equity loss that triggers a forced exit after pyramiding. |
| `MaximumRisk` | 0.1 | Fraction of account balance committed to each order when sizing volume. |
| `StdDevLimit` | 0.002 | Maximum 10-period standard deviation to accept new trades. |
| `VolatilityThreshold` | 800 | Minimum volatility score (amplitude × tick density) required for trading. |
| `AccountLeverage` | 100 | Account leverage used in margin approximation and position sizing. |
| `WarningAlerts` | true | Enables logging when the standard deviation filter blocks entries. |
| `CandleType` | 1 minute | Candle type used for all calculations. |

## Indicators
* `StandardDeviation(Length = 10)` on close prices for the volatility filter.
* Custom timing oscillator reproduced from the original EA (implemented inline without StockSharp indicator objects).

## Implementation notes
* Spread filtering requires live level 1 data (`Security.BestBid`/`BestAsk`). When the feed is absent the strategy assumes zero spread.
* Margin and equity checks are approximations because the original EA relied on MetaTrader-specific account properties and contract sizes. Adjust `AccountLeverage`, `MaximumRisk` or `Volume` to fit the broker model.
* The conversion uses the StockSharp high-level API (candle subscriptions with `Bind`) and keeps all comments in English as requested. No Python port is generated for this strategy.
