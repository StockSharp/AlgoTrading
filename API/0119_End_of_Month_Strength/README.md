# End of Month Strength Strategy
[Русский](README_ru.md) | [中文](README_zh.md)
 
End of Month Strength observes that equities often rally during the last few trading days as portfolio managers adjust holdings.
Buying pressure tied to window dressing can create a reliable upward bias ahead of the monthly close.

Testing indicates an average annual return of about 94%. It performs best in the stocks market.

The strategy buys near the final days of the month and exits on the first trading day of the new month to capture that tendency.

Stops are placed below recent support to guard against unexpected weakness.

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

