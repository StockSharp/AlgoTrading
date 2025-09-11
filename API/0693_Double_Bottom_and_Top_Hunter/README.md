# Double Bottom and Top Hunter Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy searches for double bottom and double top patterns by comparing recent lows and highs. A double bottom occurs when the lowest low is reached twice within a wider lookback window, while a double top uses the highest high. Long and short positions are opened accordingly and closed when price breaks the opposite extreme after a new extreme is formed.

## Details

- **Entry Criteria**:
  - **Long**: Double bottom detected.
  - **Short**: Double top detected.
- **Long/Short**: Both directions.
- **Exit Criteria**:
  - Long: New high above previous high with price dropping below previous low.
  - Short: New low below previous low with price rising above previous high.
- **Stops**: None.
- **Default Values**:
  - `Length` = 100
  - `Lookback` = 100
- **Filters**:
  - Category: Pattern
  - Direction: Both
  - Indicators: Highest, Lowest
  - Stops: No
  - Complexity: Basic
  - Timeframe: Intraday
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Low
