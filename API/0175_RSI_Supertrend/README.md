# Rsi Supertrend Strategy

Strategy based on RSI and Supertrend indicators. Enters long when RSI is
oversold (< 30) and price is above Supertrend Enters short when RSI is
overbought (> 70) and price is below Supertrend

The RSI oscillator defines momentum extremes while Supertrend points to the prevailing direction. Trades occur when RSI aligns with the Supertrend color.

Works for traders who appreciate a trailing-stop style exit. ATR settings further safeguard the position.

## Details

- **Entry Criteria**:
  - Long: `RSI < 30 && Close > Supertrend`
  - Short: `RSI > 70 && Close < Supertrend`
- **Long/Short**: Both
- **Exit Criteria**: Supertrend change
- **Stops**: Trailing with Supertrend
- **Default Values**:
  - `RsiPeriod` = 14
  - `SupertrendPeriod` = 10
  - `SupertrendMultiplier` = 3.0m
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **Filters**:
  - Category: Mean reversion
  - Direction: Both
  - Indicators: RSI, Supertrend
  - Stops: Yes
  - Complexity: Intermediate
  - Timeframe: Mid-term
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium
