# Month of Year Effect Strategy
[Русский](README_ru.md) | [中文](README_zh.md)
 
The Month of Year Effect captures performance differences observed in various months.
For example, equities often rally in November and December but can be weak during September.

Testing indicates an average annual return of about 88%. It performs best in the stocks market.

The system goes long or short at the beginning of each month based on those historical averages, exiting by month-end.

Stops are used to protect capital if the usual seasonal behavior fails to appear.

## Details

- **Entry Criteria**: calendar effect triggers
- **Long/Short**: Both
- **Exit Criteria**: stop-loss or opposite signal
- **Stops**: Yes, percent based
- **Default Values**:
  - `CandleType` = 15 minute
  - `StopLoss` = 2%
- **Filters**:
  - Category: Seasonality
  - Direction: Both
  - Indicators: Seasonality
  - Stops: Yes
  - Complexity: Intermediate
  - Timeframe: Intraday
  - Seasonality: Yes
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium

