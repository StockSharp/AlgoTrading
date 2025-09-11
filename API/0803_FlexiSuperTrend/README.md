# FlexiSuperTrend Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

The strategy combines a SuperTrend filter with a smoothed deviation oscillator.
A position is opened when price agrees with the SuperTrend direction and the
oscillator confirms momentum.

## Details

- **Entry Criteria**:
  - Price above SuperTrend and deviation (SMA of price minus SuperTrend) > 0 → buy.
  - Price below SuperTrend and deviation < 0 → sell.
- **Long/Short**: Both directions can be enabled.
- **Exit Criteria**:
  - Trend reversal when price crosses the SuperTrend line.
- **Stops**: No stop logic by default.
- **Default Values**:
  - ATR period = 10.
  - ATR factor = 3.0.
  - SMA length = 10.
- **Filters**:
  - Category: Trend following
  - Direction: Both
  - Indicators: SuperTrend, SMA
  - Stops: None
  - Complexity: Low
  - Timeframe: Any
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium
