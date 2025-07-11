# Rsi Stochastic Strategy

Strategy that combines RSI and Stochastic Oscillator for double
confirmation of oversold and overbought conditions.

RSI provides a broader momentum view, while Stochastic gives faster signals near extremes. Trades flip as the oscillator crosses levels within the RSI context.

Ideal for nimble traders who favor oscillator setups. The strategy relies on an ATR stop to contain risk.

## Details

- **Entry Criteria**:
  - Long: `RSI < RsiOversold && StochK < StochOversold`
  - Short: `RSI > RsiOverbought && StochK > StochOverbought`
- **Long/Short**: Both
- **Exit Criteria**:
  - Long: `RSI > 50`
  - Short: `RSI < 50`
- **Stops**: Percent-based at `StopLossPercent`
- **Default Values**:
  - `RsiPeriod` = 14
  - `RsiOversold` = 30m
  - `RsiOverbought` = 70m
  - `StochPeriod` = 14
  - `StochK` = 3
  - `StochD` = 3
  - `StochOversold` = 20m
  - `StochOverbought` = 80m
  - `StopLossPercent` = 2.0m
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **Filters**:
  - Category: Mean reversion
  - Direction: Both
  - Indicators: RSI, Stochastic Oscillator
  - Stops: Yes
  - Complexity: Intermediate
  - Timeframe: Mid-term
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium
