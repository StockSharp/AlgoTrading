# SuperTrend AI Oscillator Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

SuperTrend AI Oscillator combines a SuperTrend trailing stop with a custom oscillator filter.
The strategy trades on SuperTrend reversals confirmed by the oscillator.
Positions are managed by a trailing stop and optional risk-reward target.

## Details

- **Entry Criteria**: SuperTrend flip with oscillator > 50 for long or < 50 for short
- **Long/Short**: Both
- **Exit Criteria**: Trailing stop or risk-reward take profit
- **Stops**: Trailing stop
- **Default Values**:
  - `AtrLength` = 10
  - `Factor` = 1
  - `RiskReward` = 2
- **Filters**:
  - Category: Trend
  - Direction: Both
  - Indicators: ATR, Stochastic
  - Stops: Trailing
  - Complexity: Intermediate
  - Timeframe: Intraday
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium
