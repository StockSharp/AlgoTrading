# Profit Hunter HSI with Fibonacci Strategy

## Overview
This strategy is a C# port of the MetaTrader 4 expert advisor `Profit_Hunter_HSI_with_fibonacci.mq4`. The original script combines
an intraday exponential moving average (EMA) filter with Fibonacci retracement zones derived from the daily chart. The StockSharp
implementation follows the same idea using the high-level API: it subscribes to two candle streams (intraday and daily), calculates
the Fibonacci grid dynamically, generates trade signals when price interacts with those bands, and manages the resulting position
with adaptive stop placement and a stepped trailing stop logic.

## Market Data Flow
1. **Intraday candles** – the `TimeFrame` parameter defines the working resolution (default: 1 minute). Each finished candle feeds
   the EMA trend filter, updates the most recent support/resistance reference taken `NumBars` bars ago, and triggers the trading
   logic.
2. **Daily candles** – a dedicated subscription collects higher time frame data. Two user-configurable indices pick the swing high
   and swing low used as anchors for the Fibonacci grid. Whenever a new daily candle arrives the entire retracement ladder is
   recalculated, including the extensions (161.8%, 261.8%, 423.6%).

## Signal Generation
The MQL advisor stored the last discovered swing high/low and determined which one happened first (`highFirst`). The port keeps the
same concept by comparing the day indices:
- If the selected high is more recent than the selected low (`highFirst = true`) the market is treated as descending and the
  Fibonacci levels are measured upward from the low.
- Otherwise the move is considered ascending and the grid is projected downward from the high.

For every completed intraday candle the following rules mirror the original EA:
1. **Trend filter** – an EMA with period `MaPeriod` classifies the short-term bias. If the close price (treated as both bid and ask)
   is above the EMA the trend is "Naik" (up); if it is below, the trend is "Turun" (down). When the price hovers exactly around the
   EMA no trade will be opened.
2. **Fibonacci signal** – depending on `highFirst` the price interaction with the 23.6%, 76.4%, 91% and 14.6% levels produces one of
   four string signals from the MT4 code: `Reverse-Buy`, `Reverse-Sell`, `Trading-Area` or `Continuation`. Only the first three are
   used for actual entries, the last one simply reports a trend continuation.
3. **Entry rules** – the original script contained six entry branches. They are reproduced verbatim:
   - Up trend + trading area + breakout above the reference resistance → buy with the protective stop at the referenced support.
   - Up trend + reverse sell + `highFirst == false` + price still below resistance → open a short with the stop at the 14.6% level.
   - Up trend + reverse buy + `highFirst == false` + price below resistance → buy with the stop at the 91% level.
   - Down trend + trading area + breakdown under support → sell with the stop at the resistance line.
   - Down trend + reverse sell + `highFirst == true` + price below resistance → sell with the stop at the 91% level.
   - Down trend + reverse buy + `highFirst == true` + price below resistance → buy with the stop at the 14.6% level.
   Only one position may exist at a time; active orders are not stacked.

## Position Management
- **Support/resistance exits** – as in the EA, a long position is liquidated if price falls back to the support reference while a
  short is closed when price rallies to the resistance reference, regardless of current profit.
- **Initial protective stop** – the stop level computed during the entry decision is stored internally and used as an exit trigger.
  The StockSharp version performs the same check on every candle instead of modifying broker orders directly.
- **Stepped trailing stop** – the MQL script raised the stop level every 20 points after an initial 60-point move (e.g., +60 → stop
  to +55, +80 → stop to +75, … up to +260). The port keeps the exact ladder using the instrument `PriceStep` to convert points into
  price offsets. For short trades the stop slides downward to lock in profits, guaranteeing the same distance as the original.

## Parameters
| Name | Description | Default | Notes |
| --- | --- | --- | --- |
| `NumBars` | Shift of the candle whose high/low becomes the temporary resistance/support. | `3` | Matches the `numBars` extern input; must be greater than zero. |
| `MaPeriod` | Period of the EMA used for trend classification. | `5` | Equivalent to `maPeriod` in the EA. |
| `TimeFrame` | Intraday candle timeframe. | `1 minute` | Mirrors the `timeFrame` extern; accepts any `TimeSpan`. |
| `DaysBackForHigh` | Index of the daily candle providing the swing high. | `1` | Corresponds to `daysBackForHigh`. |
| `DaysBackForLow` | Index of the daily candle providing the swing low. | `1` | Corresponds to `daysBackForLow`. |
| `Volume` | Market order size. | `1` | Represents lots/shares; validated to stay positive. |

## Implementation Notes
- The original EA created numerous graphical objects. Those calls are intentionally omitted because StockSharp handles charting
  separately and the shapes were purely cosmetic.
- Instead of querying historical buffers like `iLow` and `iHigh`, the port maintains two in-memory lists of finished candles and
  reads the required shift directly from there.
- Stop management is implemented in strategy code (`ManagePosition`) rather than via `OrderModify`, which keeps the behaviour broker
  agnostic while preserving the same decision tree.
- Order rejections clear the pending entry state so manual adjustments do not leave stale internal flags, matching the defensive
  coding present in many existing API strategies.

## Differences from the MetaTrader Version
- MetaTrader assumed access to tick-level `Ask` and `Bid`. StockSharp operates on candle closes by default; the close price is used
  as both bid and ask proxy, which is sufficient for replicating the decision logic.
- The notion of "which extremum appeared first" cannot rely on MT4's `High[]`/`Low[]` series. The port approximates it by comparing
  the selected day indices, delivering identical results for the default configuration and preserving the intended behaviour for
  other settings.
- Broker-side stop and take-profit orders are replaced with virtual exits evaluated per candle. This avoids connector-specific order
  types while ensuring the same exit conditions are met.
