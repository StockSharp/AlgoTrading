# VWAP Breakout Strategy

This breakout strategy tracks how far price pulls away from the volume weighted average price. By measuring the distance in terms of the Average True Range it attempts to identify moments of accelerated momentum.

A buy is triggered when the market closes more than `K` times the ATR above VWAP, signalling strong upward pressure. Likewise, a short is taken once price drops `K` ATRs below VWAP. Trades are closed when price comes back to the VWAP line, assuming the burst of energy has faded.

The approach is designed for short-term traders who enjoy trading sudden expansions in volatility. Fixed protective stops and a clear reentry level help manage the risk of false breakouts.

## Details
- **Entry Criteria**:
  - **Long**: Price > VWAP + K*ATR (breakout above upper band)
  - **Short**: Price < VWAP - K*ATR (breakout below lower band)
- **Long/Short**: Both sides.
- **Exit Criteria**:
  - **Long**: Exit long when price falls back below VWAP
  - **Short**: Exit short when price rises back above VWAP
- **Stops**: Yes.
- **Default Values**:
  - `K` = 2.0m
  - `CandleType` = TimeSpan.FromMinutes(5)
  - `AtrPeriod` = 14
- **Filters**:
  - Category: Breakout
  - Direction: Both
  - Indicators: VWAP Breakout
  - Stops: Yes
  - Complexity: Intermediate
  - Timeframe: Intraday
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk Level: Medium
