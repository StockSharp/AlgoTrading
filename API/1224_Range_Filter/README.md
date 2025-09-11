# Range Filter
[Русский](README_ru.md) | [中文](README_cn.md)

Range filter strategy with realistic range calculation and fixed risk/reward levels.

It uses a smoothed range to create dynamic bands around price. Trades are taken when price breaks above or below these bands. Risk management uses fixed stop loss and take profit distances.

## Details

- **Entry Criteria**: Price breaks range filter bands.
- **Long/Short**: Both directions.
- **Exit Criteria**: Stop loss or take profit.
- **Stops**: Yes.
- **Default Values**:
  - `SamplingPeriod` = 100
  - `RangeMultiplier` = 3
  - `RiskPoints` = 50
  - `RewardPoints` = 100
  - `MaxTradesPerDay` = 5
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filters**:
  - Category: Trend
  - Direction: Both
  - Indicators: Range filter
  - Stops: Yes
  - Complexity: Intermediate
  - Timeframe: Intraday (5m)
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium
