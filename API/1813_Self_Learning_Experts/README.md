# Self-Learning Experts Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy learns from historical binary price patterns and estimates the probability of future upward or downward movement. When the probability exceeds a user-defined threshold, the strategy opens a market position in that direction. Collected statistics decay over time via a forgetting factor to give more weight to recent behavior. The system can optionally move stop levels when new signals appear and supports a trailing stop based on price steps.

## Details

- **Entry Criteria**:
  - **Long**: Probability of upward move ≥ `ProbabilityThreshold`.
  - **Short**: Probability of downward move ≥ `ProbabilityThreshold`.
- **Stops**: Optional trailing stop with symmetrical stop-loss and take-profit.
- **Default Values**:
  - `PatternSize` = 10
  - `ProbabilityThreshold` = 0.8
  - `ForgetRate` = 1.05
  - `Trailing` = 0 (disabled)
- **Filters**:
  - Category: Pattern recognition
  - Direction: Both
  - Indicators: None
  - Stops: Optional
  - Complexity: High
  - Timeframe: Any
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: High
