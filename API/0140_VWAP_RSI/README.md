# VWAP RSI Strategy

VWAP RSI uses the volume-weighted average price to gauge fair value during the session while RSI shows momentum extremes.
Trades are taken when price stretches away from VWAP and RSI reaches overbought or oversold levels.

The expectation is that price will revert back toward VWAP once momentum cools.

A percent stop guards against trends that continue to drive price away from VWAP.

## Details

- **Entry Criteria**: indicator signal
- **Long/Short**: Both
- **Exit Criteria**: stop-loss or opposite signal
- **Stops**: Yes, percent based
- **Default Values**:
  - `CandleType` = 15 minute
  - `StopLoss` = 2%
- **Filters**:
  - Category: Trend following
  - Direction: Both
  - Indicators: VWAP, RSI
  - Stops: Yes
  - Complexity: Intermediate
  - Timeframe: Intraday
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium
