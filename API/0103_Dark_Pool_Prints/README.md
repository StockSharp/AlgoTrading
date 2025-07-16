# Dark Pool Prints Strategy
[Русский](README_ru.md) | [中文](README_zh.md)
 
Dark Pool Prints tracks large off-exchange transactions that often precede sharp moves once the activity is revealed.
Unusual volume hitting the tape can signal institutional positioning that hasn't yet impacted the regular market.

The strategy enters in the same direction as heavy dark pool buying or selling, expecting follow-through when the rest of the market reacts.

A small percent stop keeps risk contained and positions close if the anticipated momentum fails to appear.

## Details

- **Entry Criteria**: indicator signal
- **Long/Short**: Both
- **Exit Criteria**: stop-loss or opposite signal
- **Stops**: Yes, percent based
- **Default Values**:
  - `CandleType` = 15 minute
  - `StopLoss` = 2%
- **Filters**:
  - Category: Volume
  - Direction: Both
  - Indicators: Volume
  - Stops: Yes
  - Complexity: Intermediate
  - Timeframe: Intraday
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium
