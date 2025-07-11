# Supertrend Stochastic Strategy

Supertrend + Stochastic strategy. Strategy enters trades when Supertrend indicates trend direction and Stochastic confirms with oversold/overbought conditions.

Supertrend marks the trend, and Stochastic points out temporary counter moves. Entries happen once Stochastic exits oversold or overbought against the trend.

Best for momentum traders needing clear trend cues. ATR values define the stop distance.

## Details

- **Entry Criteria**:
  - Long: `Close > Supertrend && StochK < 20`
  - Short: `Close < Supertrend && StochK > 80`
- **Long/Short**: Both
- **Exit Criteria**: Supertrend reversal
- **Stops**: Uses Supertrend as trailing stop
- **Default Values**:
  - `SupertrendPeriod` = 10
  - `SupertrendMultiplier` = 3.0m
  - `StochPeriod` = 14
  - `StochK` = 3
  - `StochD` = 3
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **Filters**:
  - Category: Mean reversion
  - Direction: Both
  - Indicators: Supertrend, Stochastic Oscillator
  - Stops: Yes
  - Complexity: Intermediate
  - Timeframe: Mid-term
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium
