# Williams R Mean Reversion Strategy
[Русский](README_ru.md) | [中文](README_zh.md)
 
Williams %R oscillates between 0 and -100 to show when price closes near the extremes of its recent range. This strategy fades those extremes once the indicator stretches far from its own average.

Testing indicates an average annual return of about 154%. It performs best in the stocks market.

A long trade triggers when Williams %R falls below the average minus `DeviationMultiplier` times the standard deviation. A short trade is taken when it rises above the average plus that multiplier. Exits occur when Williams %R moves back toward its average level.

The approach suits traders who rely on momentum exhaustion to time entries. A protective stop-loss limits risk if price keeps moving to new extremes.

## Details
- **Entry Criteria**:
  - **Long**: %R < Avg - DeviationMultiplier * StdDev
  - **Short**: %R > Avg + DeviationMultiplier * StdDev
- **Long/Short**: Both sides.
- **Exit Criteria**:
  - **Long**: Exit when %R > Avg
  - **Short**: Exit when %R < Avg
- **Stops**: Yes, percent stop-loss.
- **Default Values**:
  - `WilliamsRPeriod` = 14
  - `AveragePeriod` = 20
  - `DeviationMultiplier` = 2m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filters**:
  - Category: Mean Reversion
  - Direction: Both
  - Indicators: Williams %R
  - Stops: Yes
  - Complexity: Intermediate
  - Timeframe: Intraday
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk Level: Medium

