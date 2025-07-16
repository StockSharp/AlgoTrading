# Open Drive Strategy
[Русский](README_ru.md) | [中文](README_zh.md)
 
Open Drive refers to strong directional movement right from the opening bell, often after an overnight news catalyst.
Traders look for heavy volume and sustained momentum in the first few minutes.

The strategy joins that momentum, entering long or short within the opening range and trailing a stop as price extends.

Positions close quickly if the drive stalls, keeping losses small during choppy opens.

## Details

- **Entry Criteria**: indicator signal
- **Long/Short**: Both
- **Exit Criteria**: stop-loss or opposite signal
- **Stops**: Yes, percent based
- **Default Values**:
  - `CandleType` = 15 minute
  - `StopLoss` = 2%
- **Filters**:
  - Category: Intraday
  - Direction: Both
  - Indicators: Price Action
  - Stops: Yes
  - Complexity: Intermediate
  - Timeframe: Intraday
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium
