# Monday Weakness Strategy
[Русский](README_ru.md) | [中文](README_zh.md)
 
Monday Weakness notes that equities often open lower after the weekend as traders digest news and reposition.
Short-term bearish pressure can appear at the start of the week before markets stabilize.

The strategy sells short at Monday's open and covers by the close, seeking to profit from that initial softness.

Stops are kept narrow to avoid losses if the market bucks the tendency and rallies instead.

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
