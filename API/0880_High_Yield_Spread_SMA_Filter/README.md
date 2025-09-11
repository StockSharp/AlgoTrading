# High Yield Spread Strategy with SMA Filter
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy trades based on the level of the high yield credit spread or the VIX index. When the selected spread moves beyond a threshold and price is on the proper side of a simple moving average, the strategy opens a position in the chosen direction.

Positions are held for a fixed number of candles before being closed. The strategy operates on daily candles only.

## Details

- **Entry Criteria**:
  - **Long**: spread > threshold and (price > SMA if filter enabled)
  - **Short**: spread < threshold and (price < SMA if filter enabled)
- **Long/Short**: one side at a time (parameter)
- **Exit Criteria**: position closed after holding period
- **Stops**: No
- **Default Values**:
  - `Basis` = HighYieldSpread
  - `Threshold` = 5
  - `IsLong` = true
  - `HoldingPeriod` = 5
  - `UseSmaFilter` = true
  - `SmaLength` = 50
  - `CandleType` = 1 day
- **Filters**:
  - Category: Spread
  - Direction: Configurable
  - Indicators: SMA
  - Stops: No
  - Complexity: Basic
  - Timeframe: Daily
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium
