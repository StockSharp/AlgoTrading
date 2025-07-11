# Adx Stochastic Strategy

Strategy that combines ADX (Average Directional Index) for trend strength and Stochastic Oscillator for entry timing with oversold/overbought conditions.

ADX highlights trend strength while Stochastic pinpoints pullbacks. Long or short signals appear when momentum turns while ADX stays high.

It suits traders who combine trend following with oscillator timing. Protective ATR stops help control drawdowns.

## Details

- **Entry Criteria**:
  - Long: `ADX > AdxThreshold && StochK < StochOversold && Bullish`
  - Short: `ADX > AdxThreshold && StochK > StochOverbought && Bearish`
- **Long/Short**: Both
- **Exit Criteria**:
  - Exit when `ADX < AdxThreshold`
- **Stops**: Percent-based at `StopLossPercent`
- **Default Values**:
  - `AdxPeriod` = 14
  - `AdxThreshold` = 25m
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
  - Indicators: ADX, Stochastic Oscillator
  - Stops: Yes
  - Complexity: Intermediate
  - Timeframe: Mid-term
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium
