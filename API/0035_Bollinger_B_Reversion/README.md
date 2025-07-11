# Bollinger Percent B Reversion

This approach fades price extremes beyond the Bollinger Bands using the Percent B indicator. Moves above the upper band or below the lower band suggest overextension.

When percent B is less than zero or greater than one, the system bets on a return to the middle of the band. An exit threshold closes trades once momentum normalizes.

Stops are placed at a fixed percentage from entry.

## Rules

- **Entry Criteria**: Percent B outside the 0–1 range.
- **Long/Short**: Both directions.
- **Exit Criteria**: Percent B crosses `ExitValue` or stop.
- **Stops**: Yes.
- **Default Values**:
  - `BollingerPeriod` = 20
  - `BollingerDeviation` = 2.0m
  - `ExitValue` = 0.5m
  - `StopLossPercent` = 2.0m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filters**:
  - Category: Mean Reversion
  - Direction: Both
  - Indicators: Bollinger Bands
  - Stops: Yes
  - Complexity: Basic
  - Timeframe: Intraday
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium
