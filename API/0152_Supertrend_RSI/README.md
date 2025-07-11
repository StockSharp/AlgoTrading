# Supertrend Rsi Strategy

Implementation of strategy #152 - Supertrend + RSI. Buy when price is
above Supertrend and RSI is below 30 (oversold). Sell when price is
below Supertrend and RSI is above 70 (overbought).

The Supertrend indicator shows the current trend, and RSI spots when price is stretched. Orders follow the Supertrend direction once RSI reaches an extreme.

A good choice for traders relying on trailing stops. The built-in stop from Supertrend works with the ATR setting to cap losses.

## Details

- **Entry Criteria**:
  - Long: `Close > Supertrend && RSI < RsiOversold`
  - Short: `Close < Supertrend && RSI > RsiOverbought`
- **Long/Short**: Both
- **Exit Criteria**:
  - Supertrend flip in opposite direction
- **Stops**: Uses Supertrend as trailing stop
- **Default Values**:
  - `SupertrendPeriod` = 10
  - `SupertrendMultiplier` = 3.0m
  - `RsiPeriod` = 14
  - `RsiOversold` = 30m
  - `RsiOverbought` = 70m
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **Filters**:
  - Category: Mean reversion
  - Direction: Both
  - Indicators: Supertrend, RSI
  - Stops: Yes
  - Complexity: Intermediate
  - Timeframe: Mid-term
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium
