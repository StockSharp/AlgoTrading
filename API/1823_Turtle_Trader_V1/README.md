[Русский](README_ru.md) | [中文](README_cn.md)

Turtle Trader V1 combines multiple momentum oscillators with a moving average filter. A long position is opened when the fast EMA is above the slow EMA and RSI, Stochastic, CCI, Momentum and Chaikin oscillator all point upward. Shorts require the opposite conditions.

## Details

- **Entry Criteria**:
  - Fast EMA above slow EMA (below for shorts)
  - RSI rising and below 70 for longs, RSI falling and above 30 for shorts
  - Stochastic %K below 88 for longs, above 12 for shorts
  - CCI and Momentum increasing for longs, decreasing for shorts
  - Chaikin oscillator moving in trade direction
- **Long/Short**: Both
- **Exit Criteria**: opposite signal
- **Stops**: none by default
- **Default Values**:
  - `FastMaPeriod` = 10
  - `SlowMaPeriod` = 50
  - `RsiPeriod` = 14
  - `StochPeriod` = 14
  - `CciPeriod` = 20
  - `MomentumPeriod` = 10
  - `ChoFastPeriod` = 3
  - `ChoSlowPeriod` = 10
- **Filters**:
  - Category: Trend Following / Momentum
  - Direction: Both
  - Indicators: EMA, RSI, Stochastic, CCI, Momentum, Chaikin Oscillator
  - Stops: None
  - Complexity: Advanced
  - Timeframe: 1 hour
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium
