# Buy Dip Multiple Positions Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

The Buy Dip Multiple Positions strategy adds long positions when a price dip occurs alongside high volume and a price surge condition. Each trade risks 2% of equity and shares common trailing stop and target levels. A new position is opened only if the previous closed trade was profitable.

## Details

- **Entry Criteria**:
  - Close below the previous low by 0.2%.
  - Volume above 120% of the average of the last two bars.
  - Close below the close price N bars ago multiplied by `PriceSurgePercent` / 100.
- **Long/Short**: Long only.
- **Exit Criteria**:
  - Initial stop as a percentage of the entry bar low.
  - Trailing stop increasing each bar after the setup.
  - Target price above the entry bar low.
- **Stops**: Yes.
- **Default Values**:
  - `MaxPositions` = 20
  - `TrailRatePercent` = 1
  - `InitialStopPercent` = 85
  - `TargetPricePercent` = 60
  - `PriceSurgePercent` = 89
  - `SurgeLookbackBars` = 14
- **Filters**:
  - Category: Mean reversion
  - Direction: Long
  - Indicators: Volume, Price action
  - Stops: Yes
  - Complexity: Medium
  - Timeframe: Any
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium
