# High Yield Spread Strategy with SMA Filter
[Русский](README_ru.md) | [中文](README_cn.md)

The strategy trades based on the High Yield Spread or the VIX index. A position opens when the chosen spread crosses a threshold and an optional price filter confirms. The price filter requires the close to be above a simple moving average for longs or below it for shorts. Positions close after a fixed number of bars.

## Details

- **Entry Criteria**:
  - **Long**: Spread > threshold and close > SMA (if enabled).
  - **Short**: Spread < threshold and close < SMA (if enabled).
- **Long/Short**: Both, selected via parameter.
- **Exit Criteria**:
  - Close position after holding period bars.
- **Stops**: None.
- **Default Values**:
  - `Threshold` = 5
  - `HoldingPeriod` = 5
  - `SmaLength` = 50
- **Filters**:
  - Category: Macro
  - Direction: Both
  - Indicators: High Yield Spread/VIX, SMA
  - Stops: No
  - Complexity: Low
  - Timeframe: 1d (default)
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium
