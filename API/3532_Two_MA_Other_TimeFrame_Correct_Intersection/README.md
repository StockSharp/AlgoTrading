# Two MA Other TimeFrame Correct Intersection Strategy

## Overview
This strategy is a StockSharp port of the MetaTrader 5 expert advisor "Two MA Other TimeFrame Correct Intersection". The original EA relies on two moving averages that are each calculated on their own timeframe (for example H1 vs D1) while trade decisions are synchronized to the chart timeframe. The conversion keeps the multi-timeframe behaviour and opens long positions when the fast moving average crosses above the slow moving average. Conversely, short positions are opened when the fast average crosses below the slow one. All orders are executed at market price and the strategy always closes any opposite exposure before opening a new trade, matching the engine-driven execution model of the MQL5 script.

## Trading logic
- Subscribe to three candle streams: the primary trading timeframe, the fast-MA timeframe and the slow-MA timeframe.
- Compute the fast and slow moving averages on their dedicated timeframes. Each moving average supports the same smoothing methods and price sources that were exposed by the original `iCustom` indicator.
- Optionally apply a configurable horizontal shift to the moving-average outputs before they are compared, reproducing the `ma_shift` inputs of the EA.
- Every time a candle on the primary trading timeframe finishes, check for a crossover between the most recent and the previous moving-average values:
  - If the fast MA was below the slow MA on the previous step and is now above it, close any short position and open (or reverse into) a long position.
  - If the fast MA was above the slow MA on the previous step and is now below it, close any long position and open (or reverse into) a short position.
- All entries use the configured trade volume. When reversing an existing position the strategy increases the order size by the magnitude of the opposite exposure to ensure the position flips in a single market order.

## Parameters
| Parameter | Description |
|-----------|-------------|
| `TradeVolume` | Base volume for market entries. Used for both long and short trades. |
| `CandleType` | Primary trading timeframe. Signals are evaluated whenever a candle of this type closes. |
| `FastTimeFrame` | Timeframe used to build the fast moving average. |
| `SlowTimeFrame` | Timeframe used to build the slow moving average. |
| `FastLength` | Number of bars included in the fast moving average. |
| `SlowLength` | Number of bars included in the slow moving average. |
| `FastShift` | Horizontal shift applied to the fast moving average output before comparison. |
| `SlowShift` | Horizontal shift applied to the slow moving average output before comparison. |
| `FastMethod` | Smoothing algorithm for the fast moving average (simple, exponential, smoothed or linear weighted). |
| `SlowMethod` | Smoothing algorithm for the slow moving average. |
| `FastAppliedPrice` | Candle price used by the fast moving average (open, high, low, close, median, typical or weighted). |
| `SlowAppliedPrice` | Candle price used by the slow moving average. |

## Implementation notes
- The moving averages are processed through StockSharp high-level subscriptions (`SubscribeCandles().Bind(...)`) and keep running even when the trading timeframe differs from the calculation timeframe.
- Shift parameters are implemented with small queues that delay the indicator output by the requested number of bars, replicating the behaviour of the `ma_shift` inputs.
- The strategy uses `StartProtection()` to align with StockSharp account protection utilities, just like the original trading engine that guarded open positions.
- Chart rendering adds the primary candles together with the fast and slow moving averages so the crossover signals remain visible during backtests.
- There is no stop-loss, take-profit or trailing-stop module in the original EA. Traders can combine this module with separate money-management strategies if additional risk control is required.
