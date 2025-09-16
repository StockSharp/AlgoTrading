# Color Schaff DeMarker Trend Cycle Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

The **Color Schaff DeMarker Trend Cycle Strategy** uses a custom oscillator derived from fast and slow DeMarker values. The indicator applies two stochastic steps to create a cycle value that oscillates between -100 and +100. Colors are assigned based on the level and slope of the oscillator, which are then used to generate trading signals.

The strategy enters long positions when the oscillator exits the upper zone and leaves short positions. It opens short positions when the oscillator leaves the lower zone and exits long positions. The idea is to react to momentum changes at extreme levels.

## Details

- **Entry Criteria**:
  - **Long**: Previous color > 5 and current color < 6.
  - **Short**: Previous color < 2 and current color > 1.
- **Long/Short**: Both.
- **Exit Criteria**:
  - **Long**: Color < 2 when a long position is open.
  - **Short**: Color > 5 when a short position is open.
- **Stops**: No explicit stop-loss or take-profit.
- **Default Values**:
  - `FastDeMarker` = 23
  - `SlowDeMarker` = 50
  - `Cycle` = 10
  - `HighLevel` = 60
  - `LowLevel` = -60
- **Filters**:
  - Category: Trend following
  - Direction: Both
  - Indicators: DeMarker, Highest, Lowest
  - Stops: No
  - Complexity: Medium
  - Timeframe: 4H
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium
