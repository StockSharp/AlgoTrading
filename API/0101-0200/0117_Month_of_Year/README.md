# Month of Year Effect Strategy
[Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)
 
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
  - `StopLossPercent` = 2
    Days are UTC days, since the market trades around the clock; entries happen at the close of the day's first candle. November through April are bought and May through October sold short, the classic seasonal split with strong winter months and a weak September; the position opens on the month's first candle and closes at its last.
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

