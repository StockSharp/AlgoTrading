# Bollinger Stochastic Strategy

The bollinger stochastic looks for specific patterns or indicator conditions to enter trades.

Signals rely on bollinger bands, stochastic to confirm the opportunity before executing.

Risk is controlled with a fixed percent stop and positions close when the signal fades.

## Details

- **Entry Criteria**: indicator signal
- **Long/Short**: Both
- **Exit Criteria**: stop-loss or opposite signal
- **Stops**: Yes, percent based
- **Default Values**:
  - `CandleType` = 15 minute
  - `StopLoss` = 2%
- **Filters**:
  - Category: Mean reversion
  - Direction: Both
  - Indicators: Bollinger Bands, Stochastic
  - Stops: Yes
  - Complexity: Intermediate
  - Timeframe: Intraday
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium
