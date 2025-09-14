# Color HMA StDev
[Русский](README_ru.md) | [中文](README_cn.md)

Strategy based on Hull Moving Average with a dynamic standard deviation filter.

The system watches how far price deviates from the HMA. When the close breaks above the
average by a chosen multiple of the standard deviation the strategy enters long, and vice versa for short positions.
A wider multiplier defines an exit zone so that positions are closed only after a significant return inside the band.

This approach attempts to capture fast momentum bursts while avoiding noise. The Hull Moving Average reacts quickly
to trend changes, and the standard deviation adapts to volatility allowing the thresholds to expand during turbulent
markets. The strategy trades in both directions and does not use fixed stops, relying instead on the
mean reversion of price back toward the HMA.

## Details

- **Entry Criteria**: Close crossing HMA ± K1 * StdDev.
- **Long/Short**: Both directions.
- **Exit Criteria**: Close crossing HMA ± K2 * StdDev in opposite direction.
- **Stops**: No fixed stop-loss or take-profit.
- **Default Values**:
  - `HmaPeriod` = 13
  - `StdPeriod` = 9
  - `K1` = 1.5m
  - `K2` = 2.5m
  - `CandleType` = TimeSpan.FromHours(4)
- **Filters**:
  - Category: Trend, Volatility
  - Direction: Both
  - Indicators: HMA, Standard Deviation
  - Stops: No
  - Complexity: Intermediate
  - Timeframe: 4h
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium
