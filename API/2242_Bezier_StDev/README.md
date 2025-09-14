# Bezier Standard Deviation Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy trades turning points in volatility using a standard deviation indicator. It interprets local minima and maxima of the indicator as potential reversals in price action. When the standard deviation forms a trough, the system expects volatility to expand upward and enters a long position. When a peak appears, it sells short anticipating volatility contraction.

The approach is designed for both long and short trading on a four-hour timeframe by default. It does not apply stop-loss orders, focusing instead on signal-based exits.

## Details

- **Entry Criteria**:
  - **Long**: Standard deviation value at the previous bar is lower than its neighbors (local minimum).
  - **Short**: Standard deviation value at the previous bar is higher than its neighbors (local maximum).
- **Long/Short**: Both.
- **Exit Criteria**:
  - Opposite signal triggers a reversal.
- **Stops**: No.
- **Default Values**:
  - `StdDev Period` = 9.
  - `Candle Type` = 4-hour candles.
- **Filters**:
  - Category: Mean reversion
  - Direction: Both
  - Indicators: Standard deviation
  - Stops: No
  - Complexity: Simple
  - Timeframe: Medium-term
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium
