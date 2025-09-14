# Color Stochastic NR Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy trades using a Stochastic oscillator with several selectable modes. Each mode defines how the %K and %D lines are interpreted to generate buy and sell signals.

Modes:

- **Breakdown** – long when %K crosses above the 50 level, short when it falls below.
- **OscTwist** – reacts to direction changes of %K.
- **SignalTwist** – reacts to direction changes of %D.
- **OscDisposition** – long when %K crosses above %D, short when it crosses below.
- **SignalBreakdown** – trades when %D crosses the 50 level.

Opposite signals close existing positions and open new ones in the opposite direction. Risk control is handled by fixed percentage stop-loss and take-profit levels.

## Details

- **Entry Criteria**:
  - **Long**: Depends on selected mode, see above.
  - **Short**: Depends on selected mode, see above.
- **Long/Short**: Both.
- **Exit Criteria**: Opposite signal or stop protection.
- **Stops**: Yes, `StopLossPercent` and `TakeProfitPercent`.
- **Default Values**:
  - `KPeriod` = 5
  - `DPeriod` = 3
  - `Mode` = `OscDisposition`
  - `StopLossPercent` = 2
  - `TakeProfitPercent` = 2
  - `CandleType` = 4 hour
- **Filters**:
  - Category: Oscillator
  - Direction: Both
  - Indicators: Stochastic
  - Stops: Yes
  - Complexity: Intermediate
  - Timeframe: 4H
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium
